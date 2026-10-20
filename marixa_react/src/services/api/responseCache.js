// Private JSON only, in memory for this tab. Never persist employee data or tokens.
export function createResponseCache({ now = Date.now, freshMs = 300_000, staleMs = 1_800_000, maxEntries = 160, maxBytes = 8_000_000 } = {}) {
    const entries = new Map();
    const pending = new Map();
    let scope = null;
    let generation = 0;
    let bytes = 0;
    const clear = () => { entries.clear(); pending.clear(); bytes = 0; generation++; };
    const select = (next) => { if (scope !== next) { clear(); scope = next; } };
    const remove = (key) => { bytes -= entries.get(key)?.size || 0; entries.delete(key); };
    const copy = (response) => ({ ...response, data: structuredClone(response.data) });
    const peek = (key, session, allowStale = false) => {
        select(session);
        const entry = entries.get(key);
        if (!entry) return null;
        if (now() - entry.at > staleMs) { remove(key); return null; }
        if (!allowStale && now() - entry.at > freshMs) return null;
        return copy(entry.response);
    };
    const read = async (key, session, load, { force = false, staleOnError = () => false, signal } = {}) => {
        select(session);
        if (signal?.aborted) throw signal.reason || new Error("Request cancelled");
        if (!force) {
            const hit = peek(key, session);
            if (hit) return hit;
            // Caller-owned cancellation must never cancel another consumer's request.
            if (!signal && pending.has(key)) return copy(await pending.get(key));
        } else {
            generation++; pending.delete(key); remove(key);
        }
        const revision = generation;
        const task = (async () => {
            try {
                const response = await load();
                if (revision === generation && scope === session && !signal?.aborted) {
                    const size = new TextEncoder().encode(JSON.stringify(response.data)).length;
                    if (size <= maxBytes) {
                        remove(key);
                        entries.set(key, { response: copy(response), at: now(), size }); bytes += size;
                        while (entries.size > maxEntries || bytes > maxBytes) remove(entries.keys().next().value);
                    }
                }
                return response;
            } catch (error) {
                if (!force && revision === generation && scope === session && !signal?.aborted && staleOnError(error)) {
                    const stale = peek(key, session, true);
                    if (stale) return { ...stale, fromStaleCache: true };
                }
                throw error;
            }
        })();
        if (!signal) pending.set(key, task);
        try { return copy(await task); }
        finally { if (pending.get(key) === task) pending.delete(key); }
    };
    const invalidateWhere = (matches) => {
        generation++; pending.clear();
        for (const key of entries.keys()) if (matches(key)) remove(key);
    };
    return { read, peek, clear, invalidateWhere, get size() { return entries.size; }, get bytes() { return bytes; } };
}

export const responseCache = createResponseCache();
export function invalidateData(broadcast = false, prefix = null) {
    if (prefix) responseCache.invalidateWhere((key) => key.includes(prefix));
    else responseCache.clear();
    if (broadcast && typeof window !== "undefined") {
        try { localStorage.setItem("marixa_data_revision", `${Date.now()}-${Math.random()}`); } catch { /* optional cross-tab signal */ }
    }
}
export const refreshData = (load) => () => { invalidateData(); return load(); };
export const sessionScope = (auth) => auth?.token
    ? JSON.stringify([auth.userId, auth.employeeId, [...(auth.roles || [])].sort(), auth.token]) : null;

export function cacheKey(uri) {
    const [path, query = ""] = uri.split("?");
    const params = new URLSearchParams(query); params.sort();
    return params.size ? `${path}?${params}` : path;
}

export function cacheableRead(config, auth) {
    return Boolean(auth?.token) && config.cache !== false && (!config.responseType || config.responseType === "json")
        && !/\/(?:Auth|ActivationCode|UserRole|UserLogin)(?:\/|$)|download|export\.|\/snapshot(?:\?|$)/i.test(config.url || "");
}
