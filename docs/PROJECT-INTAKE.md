# Hồ sơ tiếp nhận và phạm vi Marixa

> Chủ dự án: Đinh Văn Tài. Cập nhật 2026-10-18. Trạng thái REVIEW; ghi nhận nhiệm vụ trong cuộc trò chuyện, không tự chứng nhận sản phẩm.

## 1. Dự án và nhiệm vụ

Web quản lý nhân sự/chấm công tiếng Việt, quy mô đã ghi nhận dưới 15 người. Repo taiDinh 303/M-ChamCong, checkout D:/My Project/marixa_aspnet_react; backend marixa_aspnet và frontend marixa_react đã merge main `bf11d5c`. Đợt nâng schema tiếp theo trên codex/supabase-schema-audit. Stack bắt buộc ASP.NET 8/Identity JWT/EF, React/Vite, SQL Server local/PostgreSQL cloud, ảnh Supabase Storage public. Múi giờ nghiệp vụ VN UTC+7, instant lưu UTC.

Nhiệm vụ 2026-10-17: tham khảo docs_mau, bổ sung tài liệu phù hợp, hoàn thiện backend theo yêu cầu có đủ đặc tả, build/test, chấm nghiêm khắc, sửa lỗi và push. Không bao gồm sửa Figma, xây native app, mua dịch vụ, chuyển Auth hoặc ghi database production. Phần tiếp theo do chủ dự án giao: hoàn thiện React khớp API, giữ giao diện và phỏng vấn trước khi thêm luồng. Chủ dự án chọn tự chạy hai SQL live; agent chỉ đọc metadata để chẩn đoán.

Bổ sung 18/10/2026: sau khi chẩn đoán thiếu schema, chủ dự án xác nhận **“Đồng ý, bạn sao lưu và cập nhật”** cho marixa001: ba bảng kỳ công, năm cột, kiểu ActualHours và ba unique index. Quyền này thay lựa chọn tự chạy SQL của bước trước, chỉ cho phép nâng schema cần thiết, bảo toàn dữ liệu và kiểm tra lại API. Đã sao lưu/restore thử, áp migration và đối chiếu dữ liệu; bằng chứng hiện hành ở worklog. Không tự suy quyền ghi/xóa dữ liệu nghiệp vụ để kiểm thử live.

## 2. Áp dụng bộ mẫu

Nhiệm vụ tiếp 18/10: chủ dự án yêu cầu dữ liệu giả thực tế để test, xác nhận **“Thêm vào Supabase hiện tại để test trên web”**. Phạm vi: thêm bộ DEMO có nhãn, giữ hồ sơ cũ; không tạo giả token/claim/đăng nhập ngoài/lịch sử migration chỉ để lấp bảng. Cho phép bổ sung tài khoản nhân viên DEMO; không mở thêm quyền HR/Admin. Đồng thời bỏ ngày Home, đưa tiêu đề lên header desktop/xuống dưới hàng logo mobile và thay sidebar ngang bằng nút mở menu. Không suy quyền đổi gói/volume hoặc tự merge/deploy từ quyền thêm dữ liệu.

| File/nhóm mẫu | Áp dụng | Phạm vi/lý do |
| --- | --- | --- |
| 00–13, worklog | YES | Giữ nội dung hiện hành, sửa sai khác source, không chép đè thư viện. |
| AGENTS, PROJECT-INTAKE, AI-START-PROMPT | YES | Hướng dẫn tiếp quản và ràng buộc agent. |
| 14–25 | YES | ADR/API/security/config/release/CI/observability/ops/backup/migration/integrations/performance. |
| 26-mobile-desktop | NO | Hiện chỉ web responsive/camera; không có native mobile/desktop hay thiết bị chấm công tích hợp. |
| 27-offline-realtime-jobs | YES | Đặc tả ranh giới chưa làm; queue UI và job cấp phép cần chính sách, không suy từ bảng DB. |
| 28-payments-commerce | NO | Không bán hàng/thu tiền; payroll là hồ sơ nhân sự, không payment gateway. |
| 29-multitenancy | NO | Một công ty/project hiện hành; chưa có tenant architecture. |
| 30–31 | YES | Hướng dẫn người dùng và bàn giao bảo trì. |
| 32-ai-features | NO | Chưa có tính năng AI trong sản phẩm; Codex/Agnes là công cụ phát triển. |
| 33-data-analytics | NO | Có thống kê trực tiếp; chưa có ETL/warehouse/ML pipeline. |
| 34-risks-changes | YES | Quản lý rủi ro, quyết định còn thiếu và thay đổi. |

YES là áp dụng tài liệu, không xác nhận chức năng đã hoàn thành. Không tạo các file NO chỉ để lấp số.

## 3. Yêu cầu lần này

| ID | Tiêu chí chấp nhận | Phạm vi |
| --- | --- | --- |
| REQ-SEC-01 | Owner/staff đúng; staff không quản trị account Admin; file HR private; test HTTP. | Backend hiện hành. |
| REQ-PER-01 | HR/Admin đánh dấu đã kiểm tra; Admin khóa/mở lại có lý do; snapshot có version và hash, không sửa/xóa. | Bảng công tháng. |
| REQ-PER-02 | Mọi đường ghi công/log/đơn ảnh hưởng tháng khóa bị chặn; sửa tháng Reviewed phải kiểm tra lại. | Persistence guard trong transaction. |
| REQ-EXP-01 | XLSX đọc snapshot đã khóa, đúng quyền và version; PDF đơn nghỉ Unicode VN, không coi là chữ ký điện tử. | Export backend; UI gọi API là việc riêng. |
| REQ-OPS-01 | Validation/config/auth rate limit/trace/health có test; schema có migration và preflight. | Local/test, không áp production. |
| REQ-POL-01 | Công thường/OT/break/phút/tự duyệt đúng phỏng vấn 17/10. | Backend. |
| REQ-LEAVE-01 | HR/Admin khác chủ hủy Approved có lý do, Refund một lần, giữ audit. | Backend; chưa auto accrual. |
| REQ-HR-01 | Áp hồ sơ/quyền đúng ngày VN, idempotent, bảo vệ admin cuối. | Worker/processor. |
| REQ-UI-01 | Ảnh/GPS theo rule; công thường/OT/số lẻ/ngày VN đúng API; lỗi API không giả dữ liệu 0. | React cá nhân/HR. |
| REQ-UI-02 | Tab OT và sửa công trong Lịch sử, đúng role/self-review. | Chủ dự án đồng ý 17/10. |
| REQ-UI-03 | Kỳ công/version/XLSX trong Thống kê; PDF và hủy/hoàn phép trong Nghỉ phép. | Chủ dự án đồng ý 17/10. |
| REQ-UI-04 | Admin chỉnh rule; HR/Admin cấp/điều chỉnh phép có reason/history, chưa auto accrual. | Hộp thoại trong trang cũ, chủ dự án đồng ý. |
| REQ-UI-05 | Session/route/own API/phân trang đúng; test/lint/build và kiểm browser cô lập. | API vẫn enforce quyền. |
| REQ-UI-06 | Bỏ ngày Home; tiêu đề trong header desktop/dưới hàng logo mobile; nút menu cạnh logo mở toàn màn hình, bỏ sidebar ngang. | Chủ dự án yêu cầu và bổ sung 18/10; shell dùng chung. |
| REQ-DEMO-01 | Dữ liệu nghiệp vụ giả có nhãn trên Supabase hiện tại; giữ hồ sơ cũ, tài khoản chỉ Employee, không fake dữ liệu bảo mật. | Chủ dự án xác nhận 18/10; seed/kiểm chứng ở worklog. |
| REQ-DOC-01 | Không placeholder mẫu còn sót; links/source/trạng thái/bằng chứng và rubric điểm rõ. | docs và backend guide. |

## 4. Điều còn thiếu thông tin

| ID | Câu hỏi/giới hạn | Người quyết định | Cách xử lý hiện tại |
| --- | --- | --- | --- |
| Q-POL-01 | Đã xác nhận 17/10: OT giao actual×Approved, tách công thường, nghỉ theo ca, phút→2 số; HR/Admin tự duyệt công/OT/sửa, nghỉ không tự duyệt. | Chủ dự án | Quyết định 01; test AttendanceTime/WorkforcePolicy. |
| Q-POL-02 | Mức/lịch/đối tượng cấp phép, carryover và retention ảnh? | Chủ dự án | Chưa bật job accrual/retention; hủy Approved có Refund do Codex chọn trong quyền được giao. |
| Q-OPS-01 | RPO/RTO, staging riêng, hạn chót/ngân sách/người trực? | Chủ dự án | CHƯA XÁC ĐỊNH; quy trình và đề xuất tại 22/25/31. |
| Q-UX-01 | Đã đồng ý UI period/XLSX, OT, sửa công, chỉnh rule và sổ phép thủ công. Offline vẫn chưa làm. | Chủ dự án 17/10 | Hiện trạng/bằng chứng 09/06/worklog; không suy nghiệm thu live từ local. |

Nguồn: quyết định trong 01, source bắt đầu `f9585fb`, nhiệm vụ trực tiếp 2026-10-17. Hồ sơ không chứa credential hoặc dữ liệu nhân viên thật.
