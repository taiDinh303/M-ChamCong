import { useCallback, useEffect, useRef, useState } from "react";
import employeeApi from "../api/employeeApi";
import { verificationRows } from "../api/directoryData.js";
import { apiErrorMessage } from "../../../services/api/contracts";
import { getAuth } from "../../../services/auth/auth";
import { sessionScope } from "../../../services/api/responseCache.js";

// Security codes are not preloaded/cached. Fetch status metadata only when the
// user opens the verification filter; this request never blocks the directory.
export default function useHrVerification() {
    const scope = sessionScope(getAuth());
    const [state, setState] = useState({ scope: null, rows: [], ready: false, loading: false, error: "" });
    const request = useRef(0);
    const load = useCallback(async () => {
        const current = ++request.current;
        setState({ scope, rows: [], ready: false, loading: true, error: "" });
        try {
            const rows = verificationRows(await employeeApi.activationCodes());
            if (current === request.current && scope === sessionScope(getAuth()))
                setState({ scope, rows, ready: true, loading: false, error: "" });
        } catch (error) {
            if (current === request.current && scope === sessionScope(getAuth()))
                setState({ scope, rows: [], ready: false, loading: false, error: apiErrorMessage(error) });
        }
    }, [scope]);
    useEffect(() => () => { request.current++; }, [scope]);
    return { ...(state.scope === scope ? state : { rows: [], ready: false, loading: false, error: "" }), load };
}
