# 14 — Quyết định kiến trúc Marixa

> Cập nhật 2026-10-17, REVIEW. Nguồn quyết định chủ dự án tại [01](01-plan.md); nhiệm vụ tại [hồ sơ](PROJECT-INTAKE.md).

| ADR | Trạng thái/nguồn | Quyết định và hệ quả |
| --- | --- | --- |
| ADR-001 | ACCEPTED, quyết định 12/10 trong 01 | Giữ ASP.NET 8/Identity JWT/React và hai provider EF; không khôi phục Next.js/Supabase Auth. |
| ADR-002 | ACCEPTED, quyết định 12/10 | Một AttendanceRule active, không version/ngày hiệu lực rule. Snapshot kỳ công lưu bản sao rule đã dùng, không biến rule thành hệ version riêng. |
| ADR-003 | ACCEPTED, nhiệm vụ cải thiện backend 16–17/10 | Controller qua service, actor/clock/UoW/adapter qua interface; domain methods bảo vệ transition mới. Contract không tham chiếu implementation persistence. |
| ADR-004 | ACCEPTED trong phạm vi nhiệm vụ 17/10 + yêu cầu P0 của 01 | Open → Reviewed → Locked; HR/Admin review, Admin lock/reopen với lý do. Snapshot version append-only, chứa dữ liệu/rule/hash. Mutation của tháng Reviewed làm mất review; tháng Locked trả 409. |
| ADR-005 | ACCEPTED, yêu cầu 01/05 | Export bảng công từ snapshot, không tính lại bằng rule mới. Leave PDF dùng dữ liệu phiên bản hiện tại và quyền đang có. Chưa là chữ ký điện tử hoặc payroll engine. |
| ADR-007 | ACCEPTED, phỏng vấn 17/10 và quyền giao Codex quyết định | Công thường/OT theo 01; ngày hiệu lực qua worker/AppliedAt; Approved leave cancellation giữ approval và refund ledger đúng một lần. |
| ADR-006 | ACCEPTED, quyết định public của 01 | Ảnh chấm công public; tài liệu report/handover private App_Data qua API có quyền. Cần storage bền/backup trước production. |

ADR-004 không tự thực hiện queue offline/điều chỉnh kỳ sau; late write vào tháng khóa cần Admin mở lại có lý do theo contract hiện tại. OT/break/tự duyệt và hủy Approved đã chốt theo phỏng vấn 17/10 tại 01; cấp phép/retention còn mở. Xem lại ADR khi đổi chính sách/hạ tầng/quy mô; không gọi lựa chọn kỹ thuật là xác nhận nghiệp vụ mới.
