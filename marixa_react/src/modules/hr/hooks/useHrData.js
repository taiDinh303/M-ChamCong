import { useState, useEffect, useCallback, useRef } from "react";
import employeeApi from "../api/employeeApi";
import { itemsOf, apiErrorMessage } from "../../../services/api/contracts";

const items = (r) =>
    r?.status === "fulfilled" ? itemsOf(r.value) : [];

// Kéo dữ liệu HR (không còn logs chấm công - đã bỏ KPI chấm công).
export const useHrData = () => {
    const [data, setData] = useState({
        employees: [],
        archivedEmployees: [],
        contracts: [],
        departments: [],
        positions: [],
        salaries: [],
        insurance: [],
        bankAccounts: [],
        activationCodes: [],
        users: [],
    });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const request = useRef(0);

    const load = useCallback(async () => {
        const current = ++request.current;
        setLoading(true);
        setError("");
        try {
            const r = await Promise.allSettled([
                employeeApi.employees(),
                employeeApi.archivedEmployees(),
                employeeApi.contracts(),
                employeeApi.departments(),
                employeeApi.positions(),
                employeeApi.salaries(),
                employeeApi.insurance(),
                employeeApi.bankAccounts(),
                employeeApi.activationCodes(),
                employeeApi.users(),
            ]);
            if (current !== request.current) return;
            const failure = r.find((result) => result.status === "rejected");
            if (failure) throw failure.reason;
            setData({
                employees: items(r[0]),
                archivedEmployees: items(r[1]).map((employee) => ({ ...employee, status: 4, isArchived: true })),
                contracts: items(r[2]),
                departments: items(r[3]),
                positions: items(r[4]),
                salaries: items(r[5]),
                insurance: items(r[6]),
                bankAccounts: items(r[7]),
                activationCodes: items(r[8]),
                users: items(r[9]),
            });
        } catch (e) {
            if (current === request.current) setError(apiErrorMessage(e));
        } finally {
            if (current === request.current) setLoading(false);
        }
    }, []);

    useEffect(() => {
        load();
        return () => { request.current += 1; };
    }, [load]);

    return { ...data, loading, error, reload: load };
};
