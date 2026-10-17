# 25 — Hiệu năng và dung lượng

> Cập nhật 2026-10-17. Quy mô dưới 15 người theo hồ sơ; chưa có load test/SLO đã đo hoặc ngân sách được chốt.

Giữ monolith/layered .NET phù hợp phạm vi hiện tại. Không bổ sung Redis, broker, CQRS hoặc microservices để tăng số lớp. Pagination cap 200; đọc danh sách công gom rule và OT theo batch. Index công-ngày, active rule, period/version và leave ledger bảo vệ truy vấn/race. EF IQueryable vẫn cần xem SQL khi thêm report lớn.

| Vùng | Giới hạn hiện tại / rủi ro | Kiểm tra khi tăng tải |
| --- | --- | --- |
| Upload | Ảnh 5 MiB; private adapter 20 MiB; handover 10 MiB/file và 20 MiB tổng | Parallel uploads, RAM, disk/volume, timeout |
| Period lock/export | Đọc cả tháng trong memory, JSON snapshot; XLSX/PDF tạo byte array | Tháng đông NV, long reason, file size, elapsed/GC |
| Writes | Serializable cho checkin/approval/period/job | Deadlock/serialization 409, retry có giới hạn, p95 latency |
| Job | Mỗi phút, tối đa 100 handover+100 promotion/tick, từng item transaction | Due backlog, poison row retry, nhiều instance |
| Storage | Ảnh public + documents; chưa retention tự động | Dung lượng tăng, object count/restore/cost |

Bài đo đề xuất: dữ liệu giả 30 ngày,15 NV, checkin đồng thời giờ vào, approve/cancel/lock cùng lúc, tải export và upload 5 MiB; ghi máy/provider/version/seed/command, p50/p95/errors/peakRAM. Chưa tự chọn ngưỡng thành cam kết. Không load test production hoặc gọi Storage live để tạo chi phí khi chưa thuộc nhiệm vụ.

Ưu tiên tối ưu theo bằng chứng: plan/query/index trước cache; streaming/background export chỉ khi đo thấy cần. Lịch sử theo rule trước khóa không có version; chỉ snapshot giữ bản rule đã chốt. Xem [20](20-observability.md), [34](34-risks-changes.md).
