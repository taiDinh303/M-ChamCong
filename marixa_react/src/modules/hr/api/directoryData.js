import { itemsOf } from "../../../services/api/contracts.js";

export const HR_DIRECTORY = [
    ["employees", "/Employee/get-all"],
    ["archivedEmployees", "/Employee/get-archived"],
    ["contracts", "/EmployeeContract/get-all"],
    ["departments", "/Department/get-all"],
    ["positions", "/Position/get-all"],
    ["salaries", "/EmployeeSalary/get-all"],
    ["insurance", "/EmployeeInsurance/get-all"],
    ["bankAccounts", "/EmployeeBankAccount/get-all"],
    ["users", "/User/get-all"],
];
export const emptyDirectory = () => Object.fromEntries(HR_DIRECTORY.map(([key]) => [key, []]));
export const isDirectoryRequest = (uri) => HR_DIRECTORY.some(([, url]) => uri.split("?")[0].endsWith(url));
const rows = (key, response) => key === "archivedEmployees"
    ? itemsOf(response).map((employee) => ({ ...employee, status: 4, isArchived: true })) : itemsOf(response);

export function peekDirectory(peek) {
    const data = {};
    for (const [key, url] of HR_DIRECTORY) {
        const response = peek(url);
        if (!response) return null;
        data[key] = rows(key, response);
    }
    return data;
}
export async function loadDirectory(get) {
    return Object.fromEntries(await Promise.all(HR_DIRECTORY.map(async ([key, url]) => [key, rows(key, await get(url))])));
}

// The directory filter needs status metadata, never the activation secret.
export const verificationRows = (response) => itemsOf(response).map(({ employeeId, isUsed, expiresAt }) => ({ employeeId, isUsed, expiresAt }));
