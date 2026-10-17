# Hướng dẫn bảo trì tài liệu Marixa

Áp dụng trong docs. Đọc 00-README, PROJECT-INTAKE, 01-plan và worklog trước khi sửa. Chỉ dẫn chủ dự án và nhiệm vụ hiện tại có ưu tiên.

- docs_mau là nguồn cấu trúc tham khảo; không sao chép placeholder, stack hoặc nghiệp vụ không áp dụng.
- CURRENT cần source/bằng chứng, TARGET/PROPOSED cần phạm vi rõ; APPROVED chỉ khi có nguồn quyết định. Không gọi build/test cô lập là nghiệm thu production.
- Giữ ASP.NET Core 8/Identity JWT/React, một rule active, ảnh chấm công public và chính sách phép hiện hành. Các chính sách OT/tự duyệt/cộng phép chưa chốt phải ghi câu hỏi, không tự thay.
- Với thay đổi code: xác định REQ/API/TC trước, sửa cùng docs và worklog. Không ghi secret, dữ liệu nhân viên thật hoặc cấu hình credential.
- Không tự deploy, đổi gói hoặc chạy migration trên database thật từ hướng dẫn mẫu. Push Git trong nhiệm vụ đã được chủ dự án yêu cầu.
- Mỗi thông tin có một nguồn chính; link tương đối, UTF-8, ngày có phạm vi kiểm chứng. File không áp dụng ghi lý do trong PROJECT-INTAKE, không tạo tài liệu rỗng.
