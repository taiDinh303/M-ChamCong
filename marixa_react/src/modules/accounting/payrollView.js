export const payrollStatusLabels = { 1: "Nháp", 2: "Đã tính", 3: "Đã duyệt", 4: "Đã trả", 5: "Hủy" };
export const payrollStatusTones = { 1: "muted", 2: "info", 3: "ok", 4: "ok", 5: "bad" };
export const money = (value) => value == null || value === "" || !Number.isFinite(Number(value))
    ? "—" : `${Number(value).toLocaleString("vi-VN")} ₫`;
export const payrollMonth = (value) => String(value || "").slice(0, 7);
export const filterPayrolls = (rows, { month = "", status = "", search = "" }) => {
    const query = search.trim().toLocaleLowerCase("vi-VN");
    return rows.filter((row) => (!month || payrollMonth(row.payrollMonth) === month)
        && (!status || String(row.status) === status)
        && (!query || String(row.employeeName || "").toLocaleLowerCase("vi-VN").includes(query)));
};
