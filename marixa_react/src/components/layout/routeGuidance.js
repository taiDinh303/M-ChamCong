const people = ["Tìm theo tên hoặc mã nhân viên, rồi dùng bộ lọc để thu hẹp danh sách.", "Mở hồ sơ để kiểm tra thông tin trước khi sửa hoặc lưu trữ. Lưu trữ giữ lại lịch sử nhân sự."];
const attendance = ["Chọn ngày hoặc nhân viên để kiểm tra giờ vào, giờ ra và trạng thái duyệt.", "Đối chiếu ảnh và chi tiết trước khi duyệt; công thường và tăng ca được theo dõi riêng."];
const leave = ["Lọc trạng thái để tìm đơn đang chờ xử lý; mở chi tiết để kiểm tra thời gian nghỉ.", "Kiểm tra số dư và sổ phép khi điều chỉnh. Đơn của chính bạn cần người khác duyệt."];
const contracts = ["Dùng bộ lọc và tìm kiếm để kiểm tra hợp đồng theo nhân viên.", "Đối chiếu ngày hiệu lực, ngày hết hạn và loại hợp đồng trước khi cập nhật hồ sơ."];
const accounts = ["Chọn đúng nhân viên và kiểm tra thông tin liên hệ trước khi cấp tài khoản.", "Chỉ chia sẻ mã kích hoạt qua kênh riêng của người nhận; không đưa mã vào báo cáo chung."];

const guides = {
    "/accounting": ["Tra cứu lương", ["Chọn Bảng lương để tìm theo nhân viên, kỳ và trạng thái.", "Mở chi tiết để đối chiếu các khoản đã lưu. Chức năng hiện tại chỉ phục vụ xem; chưa tự tính, duyệt hoặc chi lương."]],
    "/hr": ["Làm việc với hồ sơ nhân sự", people],
    "/hr/attendance-history": ["Đối soát và duyệt công", attendance],
    "/hr/statistics": ["Đối soát kỳ công", ["Chọn đúng tháng và năm; kiểm tra công thường, tăng ca và các bản ghi chưa duyệt.", "Kiểm tra dữ liệu trước khi khóa kỳ. Dùng lịch sử và bản xuất để đối chiếu phiên bản kỳ công."]],
    "/hr/work-schedule": ["Sắp xếp lịch làm", ["Chọn một ngày trên lịch hoặc ô ngày để xem nhân viên và ca được phân công.", "Kiểm tra giờ làm và giờ nghỉ của ca trước khi xếp lịch; mở chi tiết để xem chấm công trong ngày."]],
    "/hr/contracts": ["Theo dõi hợp đồng", contracts],
    "/hr/leaves": ["Xử lý nghỉ phép", leave],
    "/hr/accounts": ["Cấp tài khoản an toàn", accounts],
    "/hr/promotions": ["Xử lý đề xuất thăng chức", ["Đối chiếu nhân viên, chức vụ, phòng ban và ngày hiệu lực trong từng đề xuất.", "Hồ sơ đã duyệt được áp dụng theo ngày hiệu lực; kiểm tra lại hồ sơ nhân viên sau thời điểm đó."]],
    "/hr/handover": ["Kiểm tra bàn giao", ["Mở hồ sơ để xem nội dung, tệp và người tiếp nhận bàn giao trước khi quyết định.", "Kiểm tra ngày nghỉ việc và ngày hiệu lực; theo dõi kết quả xử lý ngay trên hồ sơ."]],
    "/hr/reports": ["Theo dõi báo cáo công việc", ["Chọn nhóm báo cáo và trạng thái phù hợp; mở nội dung và tệp trước khi duyệt.", "Khi yêu cầu bổ sung hoặc từ chối, ghi lý do cụ thể để người gửi biết cần hoàn thiện gì."]],
    "/admin": ["Bắt đầu quản trị", ["Chọn chức năng bên dưới hoặc mở menu để chuyển trang; vào Nhân sự để xử lý hồ sơ và chấm công.", "Biểu đồ phản ánh dữ liệu đọc từ hệ thống. Nếu tải lỗi, dùng Thử lại trước khi đối soát số liệu."]],
    "/admin/contracts": ["Theo dõi hợp đồng", contracts],
    "/admin/leaves": ["Xử lý nghỉ phép", leave],
    "/admin/accounts": ["Cấp tài khoản an toàn", accounts],
    "/admin/payroll": ["Kiểm tra bảng lương", ["Kiểm tra kỳ lương và tên nhân viên, rồi đối chiếu các khoản trên từng bản ghi.", "Xuất danh sách sau khi kiểm tra bộ lọc; chỉ chia sẻ bảng lương với người được phép xem."]],
    "/admin/resigned": ["Tra cứu nhân sự nghỉ việc", ["Tìm theo mã hoặc tên để xem hồ sơ có trạng thái nghỉ việc, bao gồm hồ sơ đã lưu trữ.", "Đối chiếu bàn giao và ngày hiệu lực trước khi xử lý các thông tin liên quan."]],
    "/admin/reports": ["Theo dõi báo cáo công việc", ["Lọc báo cáo theo người gửi và trạng thái, rồi mở tệp để kiểm tra nội dung.", "Ghi nhận lý do rõ ràng khi yêu cầu bổ sung hoặc từ chối báo cáo."]],
    "/admin/work": ["Tra cứu quá trình công tác", ["Tìm nhân viên để kiểm tra phòng ban, chức vụ và thời gian công tác.", "Đối chiếu các mốc thời gian với hồ sơ nhân sự khi xem lịch sử thay đổi."]],
    "/admin/roles": ["Quản lý vai trò", ["Tìm đúng tài khoản trước khi thay đổi vai trò. Trên điện thoại, vuốt trong ma trận để đối chiếu các cột.", "Cấp quyền theo công việc thực tế và kiểm tra lại sau khi lưu; giữ ít nhất một Admin hoạt động."]],
    "/admin/block-accounts": ["Kiểm soát truy cập", ["Kiểm tra tài khoản và trạng thái hiện tại trước khi khóa hoặc mở khóa.", "Không khóa tài khoản Admin hoạt động cuối cùng. Việc khóa tài khoản không thay cho xử lý hồ sơ nghỉ việc."]],
};

export function guidanceFor(pathname) {
    const path = pathname.replace(/\/$/, "");
    const match = Object.keys(guides).sort((a, b) => b.length - a.length).find((key) => path === key || path.startsWith(`${key}/`));
    return match ? { title: guides[match][0], steps: guides[match][1] } : null;
}
