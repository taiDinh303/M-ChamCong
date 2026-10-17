# Đánh giá nghiêm khắc backend Marixa

> Cập nhật 2026-10-17. Phạm vi repository/backend và bằng chứng cô lập; không tự chứng nhận production hoặc toàn bộ UI. Nguồn ban đầu `f9585fb`.

## Rubric

| Tiêu chí | Tối đa | Ban đầu | Sau sửa | Cơ sở / giới hạn |
| --- | --- | --- | --- | --- |
| Build và khả năng phát hành | 15 | 7 | 15 | Ban đầu Release -warnaserror lỗi xUnit 2031; kết quả cuối tại worklog |
| Kiến trúc và OOP | 20 | 14 | 16 | HR qua service, actor/clock/interfaces, domain period; entities/EF queries/DTO object cũ còn coupling |
| Bảo mật | 20 | 9 | 16 | Ownership/account/JWT/lockout/private files/immutable history; Data API/proxy/malware live còn mở |
| Toàn vẹn/nghiệp vụ | 15 | 10 | 12 | Policy phỏng vấn, OT separate, cancel/refund, period/version/job; dữ liệu lịch sử và biên còn cần đối soát |
| Kiểm thử | 15 | 6 | 13 | Unit/HTTP/real provider/export; thiếu browser E 2 E và concurrency toàn bộ workflow |
| Vận hành/khôi phục | 10 | 6 | 7 | CI/health/log/runbook/scripts; private volume/restore/SLO live chưa xác minh |
| Tài liệu/tiếp quản | 5 | 1 | 5 | Bộ mẫu chọn lọc, guides/traceability/status/links; kết quả validator cuối ở worklog |
| Tổng | 100 | 53 | 84 | 5.3→8.4/10; đã chốt theo kiểm chứng local và giới hạn ghi dưới đây |

## Những lỗi đã xử lý

Build analyzer; HR controller chứa nghiệp vụ/storage; thiếu accountAdmin guards/lockout và liên kết JWT; private path/partial files; timezone/seed/constraint consistency từ bản refactor; thiếu period lock/versioned snapshot/export; OT tự cộng dù chưa duyệt/giờ nguyên làm sai số; thay hồ sơ nghỉ/thăng chức quá sớm; không hủy/hoàn phép Approved an toàn; thiếu trace/rate/readiness và docs vận hành đúng stack.

Thay đổi phải có regression và schema đúng provider. Các tính năng mới không tự chứng minh production đã nâng schema hoặc UI đã nhận contract mới. Bằng chứng lệnh/số ca/provider/CI/Markdown ở [worklog](../docs/worklog.md), cấu trúc/OOP ở [ARCHITECTURE](ARCHITECTURE.md).

## Vì sao chưa 9–10

Chưa restore thật/volume bền/E 2 E/mobile, chưa audit grants/RLS cũ và Manager scope toàn bộ. Chính sách accrual/retention/cross-year/ca biên và đối soát dữ liệu cũ còn cần chốt; chưa có offline queue, nhiều contract CRUD vẫn object/IQueryable/cancellation chưa đồng nhất. [34](../docs/34-risks-changes.md) là danh mục đóng từng rủi ro. Không thêm broker/microservice/lớp rỗng hoặc test mirror để tăng điểm.

Kiểm chứng local cuối: Release: 0 warning / 0 error, 85 PASS / 1 SKIP PostgreSQL; SQL Server fixture và export QA đạt, NuGet audit 8 project: 0 advisory, Markdown 43 file hợp lệ. CI [run 39001331032](https://github.com/taiDinh303/M-ChamCong/actions/runs/39001331032) source f3a344b SUCCESS: PostgreSQL runtime + Docker build/publish và 85 PASS / 1 SKIP SQL Server trên Linux. Production/restore/browser chưa nghiệm thu.
