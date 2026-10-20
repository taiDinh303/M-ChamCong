export const isConnectionFailure = (error) => Boolean(error) && error.code !== "ERR_CANCELED" && error.name !== "AbortError"
    && (!error.response || [502, 503, 504].includes(error.response.status));

export function createConnectionManager({ now = Date.now, online = () => typeof navigator === "undefined" || navigator.onLine !== false,
    pause = (ms) => new Promise((resolve) => setTimeout(resolve, ms)), maxWaitMs = 90_000 } = {}) {
    const listeners = new Set();
    let state = { phase: "online", attempt: 0 };
    let recovery = null;
    let probe;
    let lastReady = -Infinity;
    const publish = (next) => { state = { ...state, ...next }; listeners.forEach((fn) => fn()); };
    return {
    subscribe: (fn) => { listeners.add(fn); return () => listeners.delete(fn); },
    getSnapshot: () => state,
    configure: (fn) => { probe = fn; },
    offline: () => { lastReady = -Infinity; publish({ phase: "offline" }); },
    unavailable: () => { lastReady = -Infinity; publish({ phase: "unavailable" }); },
    slow: () => { if (state.phase === "online") publish({ phase: "connecting" }); },
    success: () => { if (online() && !recovery) publish({ phase: "online", attempt: 0 }); },
    async ready({ force = false } = {}) {
        if (recovery) return recovery;
        if (!force && online() && now() - lastReady < 10_000) return;
        if (!online()) {
            publish({ phase: "offline" });
            throw new Error("Thiết bị đang mất kết nối mạng. Hãy kiểm tra Wi-Fi hoặc dữ liệu di động.");
        }
        recovery = (async () => {
            const started = now();
            let attempt = 0;
            publish({ phase: "connecting", attempt: 0 });
            while (now() - started < maxWaitMs && online()) {
                publish({ phase: "connecting", attempt: ++attempt });
                try {
                    await probe({ timeout: Math.max(1, Math.min(10_000, maxWaitMs - (now() - started))) });
                    lastReady = now();
                    publish({ phase: "online", attempt: 0 });
                    return;
                } catch (error) {
                    if (!isConnectionFailure(error)) { publish({ phase: "unavailable" }); throw error; }
                }
                const remaining = maxWaitMs - (now() - started);
                if (remaining > 0) await pause(Math.min(1500 * attempt, 6000, remaining));
            }
            publish({ phase: online() ? "unavailable" : "offline" });
            throw new Error("Chưa kết nối được máy chủ. Máy chủ có thể đang khởi động lại. Vui lòng thử lại.");
        })();
        try { await recovery; } finally { recovery = null; }
    },
    };
}
export const connection = createConnectionManager();

// GET recovery is safe; mutations are intentionally sent once by axiosClient.
export async function recoverableRead(load, { signal, recover = true, manager = connection } = {}) {
    const slow = setTimeout(() => manager.slow(), 4000);
    try { const result = await load(); manager.success(); return result; }
    catch (error) {
        if (signal?.aborted || !isConnectionFailure(error)) { manager.success(); throw error; }
        manager.slow();
        if (typeof navigator !== "undefined" && navigator.onLine === false) manager.offline();
        if (!recover) { manager.unavailable(); throw error; }
        await manager.ready({ force: true });
        if (signal?.aborted) throw error;
        try { const result = await load(); manager.success(); return result; }
        catch (retryError) { if (isConnectionFailure(retryError)) manager.unavailable(); throw retryError; }
    } finally { clearTimeout(slow); }
}
