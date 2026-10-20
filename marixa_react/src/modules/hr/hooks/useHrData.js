import { useState, useEffect, useCallback, useRef } from "react";
import { getAllPages, getCachedPages } from "../../../services/api/lists";
import { apiErrorMessage } from "../../../services/api/contracts";
import { getAuth } from "../../../services/auth/auth";
import { responseCache, sessionScope } from "../../../services/api/responseCache.js";
import { emptyDirectory, peekDirectory, loadDirectory, isDirectoryRequest } from "../api/directoryData.js";

export const useHrData = () => {
    const scope = sessionScope(getAuth());
    const [state, setState] = useState(() => {
        const cached = peekDirectory(getCachedPages);
        return { scope, data: cached || emptyDirectory(), ready: Boolean(cached), pending: !cached, error: "", version: 0 };
    });
    const request = useRef(0);
    const load = useCallback(async ({ force = false } = {}) => {
        const current = ++request.current;
        if (force) responseCache.invalidateWhere(isDirectoryRequest);
        const cached = peekDirectory(getCachedPages);
        if (cached) {
            setState({ scope, data: cached, ready: true, pending: false, error: "", version: current });
            return;
        }
        setState((previous) => ({ ...previous, scope, error: "", pending: true,
            ready: previous.scope === scope && previous.ready,
            data: previous.scope === scope ? previous.data : emptyDirectory() }));
        try {
            const data = await loadDirectory(getAllPages);
            if (current === request.current && scope === sessionScope(getAuth()))
                setState({ scope, data, ready: true, pending: false, error: "", version: current });
        } catch (error) {
            if (current !== request.current || scope !== sessionScope(getAuth())) return;
            const denied = [401, 403].includes(error.response?.status);
            setState((previous) => ({ ...previous, pending: false, error: apiErrorMessage(error),
                ...(denied ? { ready: false, data: emptyDirectory() } : {}) }));
        }
    }, [scope]);
    const reload = useCallback(() => load({ force: true }), [load]);
    useEffect(() => {
        load();
        return () => { request.current += 1; };
    }, [load]);
    const sameSession = state.scope === scope;
    const ready = sameSession && state.ready;
    return { ...(sameSession ? state.data : emptyDirectory()), ready, reload,
        loading: !ready && (!sameSession || state.pending), refreshing: ready && state.pending,
        error: sameSession ? state.error : "", version: sameSession ? state.version : 0 };
};
