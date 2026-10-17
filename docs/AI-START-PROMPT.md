# Giao việc cho Codex và Agnes trong Marixa

> Cập nhật 2026-10-17. Hướng dẫn tiếp quản, không tự cấp quyền deploy hoặc đổi chính sách.

Đọc [00](00-README.md), [hồ sơ](PROJECT-INTAKE.md), [01](01-plan.md), [worklog](worklog.md), tài liệu đúng module và AGENTS trong thư mục sẽ sửa. Đối chiếu git status/source trước khi nhận định. Backend có hướng dẫn riêng tại [AGENTS](../marixa_aspnet/AGENTS.md).

Nhiệm vụ phải nêu hành vi cần đạt, REQ/API liên quan, giới hạn dữ liệu/môi trường và bằng chứng đóng việc. Với lỗi, tái hiện bằng ca hồi quy có ý nghĩa, sửa đúng lớp rồi cập nhật tài liệu. Với thay đổi chính sách chưa được chốt, phỏng vấn chủ dự án bằng lựa chọn ngắn; tiếp tục phần độc lập. Không biến tên bảng, SQL legacy hoặc checkbox thành bằng chứng đã hoàn thành.

Giữ Identity JWT, hai provider EF, một ca chung active, ảnh chấm công public theo quyết định. OT/công/nghỉ theo [01](01-plan.md). Snapshot kỳ là lịch sử bất biến; mọi mutation phải qua guard/transaction. Không dùng ExecuteUpdate/raw SQL để vượt guard. Không đưa HTTP types vào contract nghiệp vụ mới.

Hoàn tất bằng kết quả build/test thực tế, phạm vi chưa kiểm chứng, thay đổi schema và rủi ro còn lại. Nếu được yêu cầu push, kiểm tra diff/secret/remote rồi push nhánh phù hợp; không suy quyền deploy từ quyền push. Quy trình kiểm tra ở [19](19-cicd.md), release ở [18](18-release-checklist.md).
