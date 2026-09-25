import { useEffect, useState } from "react";
import employeeApi from "../api/employeeApi";
import EmployeeTable from "../components/EmployeeTable";

const EmployeePage = () => {
    const [employees, setEmployees] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [pagination, setPagination] = useState({
        currentPage: 1,
        pageSize: 5,
        totalItems: 0,
        totalPages: 0,
    });

    const loadEmployees = async () => {
        try {
            setLoading(true);
            setError("");

            console.log("Calling Employee API...");

            const response = await employeeApi.getAll(1, 5);

            console.log("Employee API response:", response);
            console.log("Response data:", response.data);
            console.log("Employee data:", response.data.data);

            const data = response.data.data;

            setEmployees(data.items || []);

            setPagination({
                currentPage: data.currentPage,
                pageSize: data.pageSize,
                totalItems: data.totalItems,
                totalPages: data.totalPages,
            });
        } catch (error) {
            console.error("Employee API error:", error);

            if (error.response) {
                console.error("Status:", error.response.status);
                console.error("Data:", error.response.data);
            }

            if (error.request) {
                console.error("Request:", error.request);
            }

            setError(
                error.response?.data?.message ||
                error.message ||
                "Không thể tải danh sách nhân viên."
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadEmployees();
    }, []);

    return (
        <div className="employee-page">
            <div className="employee-page-header">
                <div>
                    <h1>Nhân viên</h1>
                    <p>Quản lý danh sách nhân viên</p>
                </div>
            </div>

            {error && (
                <div className="error-message">
                    {error}
                </div>
            )}

            <div className="employee-card">
                <EmployeeTable
                    employees={employees}
                    loading={loading}
                />
            </div>

            {!loading && employees.length > 0 && (
                <div className="pagination">
                    <button
                        disabled={pagination.currentPage === 1}
                    >
                        Trước
                    </button>

                    <span>
                        Trang {pagination.currentPage} /{" "}
                        {pagination.totalPages}
                    </span>

                    <button
                        disabled={
                            !pagination.hasNextPage
                        }
                    >
                        Sau
                    </button>
                </div>
            )}
        </div>
    );
};

export default EmployeePage;