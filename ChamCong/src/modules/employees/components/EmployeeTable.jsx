const EmployeeTable = ({ employees, loading }) => {
    if (loading) {
        return <p>Đang tải dữ liệu...</p>;
    }

    if (!employees.length) {
        return <p>Không có nhân viên.</p>;
    }

    return (
        <table className="employee-table">
            <thead>
                <tr>
                    <th>Mã NV</th>
                    <th>Họ và tên</th>
                    <th>Email</th>
                    <th>Số điện thoại</th>
                    <th>Phòng ban</th>
                    <th>Chức vụ</th>
                    <th>Trạng thái</th>
                </tr>
            </thead>

            <tbody>
                {employees.map((employee) => (
                    <tr key={employee.id}>
                        <td>{employee.employeeCode}</td>
                        <td>{employee.fullName}</td>
                        <td>{employee.email || "-"}</td>
                        <td>{employee.phoneNumber || "-"}</td>
                        <td>{employee.departmentName || "-"}</td>
                        <td>{employee.positionName || "-"}</td>
                        <td>{employee.status}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
};

export default EmployeeTable;