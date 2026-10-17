import { useState, useEffect, useCallback, useRef } from "react";
import { itemsOf, apiErrorMessage } from "../../../services/api/contracts";

// Kéo danh sách từ một API get-all (Promise) -> { items, loading, error, reload }.
export const useAdminList = (fetcher) => {
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const request = useRef(0);

    const reload = useCallback(async () => {
        const current = ++request.current;
        setLoading(true);
        setError("");
        try {
            const r = await fetcher();
            if (current === request.current) setRows(itemsOf(r));
        } catch (e) {
            if (current === request.current) setError(apiErrorMessage(e));
        } finally {
            if (current === request.current) setLoading(false);
        }
    }, [fetcher]);

    useEffect(() => {
        reload();
        return () => { request.current += 1; };
    }, [reload]);

    return { rows, loading, error, reload };
};
