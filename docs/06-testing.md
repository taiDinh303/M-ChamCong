# 06 — Kiểm thử và bằng chứng

> Cập nhật 2026-10-17. Test thực tế có số ca/provider tại worklog; tiêu chí dưới đây gồm cả phần đã có và phần cần nghiệm thu tiếp.

## 1. Ranh giới môi trường

M.Tests là xUnit, WebApplicationFactory/TestServer + SQLite per-host, fake HTTP/MemoryDocuments/TimeProvider riêng. Test fake-role không thay kiểm JWT thật: SessionTests chạy JWT để kiểm lockout/stamp/role/link. SQL Server LocalDB fixture nâng migration trên DB Marixa_Verify_GUID, kiểm default 12:00 khi upgrade, constraints và period transaction. PostgreSQL fixture chỉ nhận loopback, tạo/drop DB riêng, upgrade SQL hai lần rồi chạy workflow; CI cung cấp server. Linux skip LocalDB, local thiếu PostgreSQL skip có lý do. Không dùng DB/bucket thật.

## 2. Traceability tối thiểu

| REQ / TC | Bằng chứng tự động | Tiêu chí |
| --- | --- | --- |
| REQ-SEC-01 / TC-SEC-01 | Authorization/AccountManagement/Session/HumanResources/PrivateDocumentStorage |401/403 cross-user, staff không quản trị Admin, stamp/lock/link revoke, private path/rollback files |
| REQ-PER-01 / TC-PER-01 | PeriodTests, ProviderDatabaseTests | HR review/Admin lock, invalid/pending gate, version/hash/audit và scope export |
| REQ-PER-02 / TC-PER-02 | PeriodTests/AuditRetentionTests | Chặn 5 đường mutation tháng khóa, reopen/review lại, source đổi invalidates review, log/snapshot không xóa |
| REQ-EXP-01 / TC-EXP-01 | ExportTests/AuditRetentionTests | XLSX numeric/text không formula, PDF Unicode/font/multipage và scope download |
| REQ-POL-01 / TC-POL-01 | AttendanceTimeTests/WorkforcePolicyTests | OT intersection/union, break cấu hình/phút, tách giờ/self-review rule |
| REQ-LEAVE-01 / TC-LEAVE-01 | WorkforcePolicyTests | Create/approve/cancel, cấm tự duyệt/hủy Approved, reason, giữ metadata/Refund một lần |
| REQ-HR-01 / TC-HR-01 | WorkforcePolicyTests/HumanResourcesTests | Không áp sớm, VN midnight, catch-up/repeat, lock account/last admin |
| REQ-OPS-01 / TC-OPS-01 | Operational/ProviderModel/ProviderDatabase/ContractInventory |429 Retry-After/correlation/no-store, schema constraints và route anonymous whitelist |
| REQ-DOC-01 / TC-DOC-01 | scripts/verify_docs.py | UTF-8/1 H 1/links/anchors/fences/JSON và control chars |

Lệnh chạy/tạo TRX/artifact ở [19](19-cicd.md). Sample export chỉ dữ liệu giả; PDF phải extract Unicode và render/xem tất cả trang, workbook mở bằng parser độc lập. Không lấy test chỉ kiểm %PDF hoặc ZIP header làm bằng chứng layout/tiếng Việt.

## 3. Chưa được chứng minh

Browser E 2 E/mobile/camera/GPS/new UI, Storage credential/bucket thật, deploy/container runtime live, backup-restore, toàn bộ Manager scope và concurrency mọi luồng chưa có bằng chứng đầy đủ. Real-provider fixture không thay thử upgrade trên bản sao schema/data production. Job processor test không chứng minh uptime/scheduler ở host live.

Khi thêm nghiệp vụ: tái hiện lỗi/tiêu chí mới bằng ca meaningful, kiểm cả HTTP role/owner và persisted data sau rollback; thêm provider case khi có SQL/transaction/constraint. Không viết test mirror để tăng số lượng hoặc bỏ ca fail bằng skip không có lý do. Từng lần chạy ghi commit/environment/command/pass-fail-skip và giới hạn vào worklog.
