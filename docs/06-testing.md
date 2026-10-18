# 06 — Kiểm thử và bằng chứng

> Cập nhật 2026-10-18. Test thực tế có số ca/provider tại worklog; tiêu chí dưới đây gồm cả phần đã có và phần cần nghiệm thu tiếp.

## 1. Ranh giới môi trường

M.Tests là xUnit, WebApplicationFactory/TestServer + SQLite per-host, fake HTTP/MemoryDocuments/TimeProvider riêng. Test fake-role không thay kiểm JWT thật: SessionTests chạy JWT để kiểm lockout/stamp/role/link. SQL Server LocalDB fixture nâng migration trên DB Marixa_Verify_GUID, kiểm default 12:00 khi upgrade, constraints và period transaction. PostgreSQL fixture chỉ nhận loopback, tạo/drop DB riêng, upgrade cả ba SQL hai lần rồi chạy workflow; CI cung cấp server. PostgresAttendanceIntegrityTests còn kiểm preflight từ chối nhiều rule active/ngày công trùng mà giữ dữ liệu, chấp nhận lịch sử xóa mềm và enforce cả hai unique index sau upgrade. Linux skip LocalDB, local thiếu PostgreSQL skip có lý do; bằng chứng 18/10 bật cả hai provider là 95 PASS/0 FAIL/0 SKIP. Không chạy bộ test trên DB/bucket thật.

## 2. Traceability tối thiểu

Bổ sung 18/10: DemoScenarioTests và PostgresDemoScenarioTests kiểm bộ DEMO chạy lặp, giữ hồ sơ cũ, tài khoản chỉ Employee, trừ/hoàn phép và OT đúng trạng thái; reviewer bootstrap không có EmployeeId không được chọn làm người duyệt fixture. HumanResourcesTests kiểm mã report mới <=30 ký tự để tránh lỗi 500 trên provider có giới hạn cột thật. Kết quả cuối: **97 PASS / 0 FAIL / 0 SKIP** với cả SQL Server LocalDB và PostgreSQL loopback; không áp test fixture lên live. Live có bước seed được cấp quyền và kiểm API đọc/tải PDF riêng, xem worklog.

Lượt tiếp theo 18/10: **101 PASS / 0 FAIL / 0 SKIP** trên fixture cô lập (SQLite, SQL Server LocalDB, PostgreSQL loopback). DemoScenarioTests đọc snapshot/XLSX từ dữ liệu vừa seed, kiểm hash mới dạng uppercase. PeriodTests thêm bốn trường hợp hash uppercase/lowercase hợp lệ, payload đã đổi và chuỗi không phải hex; kiểm endpoint snapshot/XLSX, lọc đúng chủ, giữ nguyên payload/hash trong DB. Chấp nhận khác biệt hoa/thường của cùng digest không cho phép đổi nội dung snapshot. Browser local 375×667: panel/dialog rộng 375 cao 667, cuộn nền bị khóa; chọn mục cuối đóng menu, Escape trả focus và phục hồi cuộn nền.

| REQ / TC | Bằng chứng tự động | Tiêu chí |
| --- | --- | --- |
| REQ-DEMO-01 / TC-DEMO-01 | DemoScenarioTests, PostgresDemoScenarioTests; tool verify-only và browser live ghi riêng tại worklog | Chạy lặp không thêm trùng/sửa hồ sơ cũ; role Employee; OT Pending không công nhận, ledger và guard kỳ khóa đúng; PDF thật tải được. |
| REQ-UI-06 / TC-UI-09 | Browser local desktop, khung iframe 375/390px và viewport 375×667; kiểm lint/build | Tiêu đề đúng vị trí, Home không ngày, mobile không sidebar ngang; menu toàn màn hình, mở/chọn link/Escape/focus/khóa cuộn nền. Không thay thiết bị thật bằng browser. |
| REQ-SEC-01 / TC-SEC-01 | Authorization/AccountManagement/Session/HumanResources/PrivateDocumentStorage |401/403 cross-user, staff không quản trị Admin, stamp/lock/link revoke, private path/rollback files |
| REQ-PER-01 / TC-PER-01 | PeriodTests, ProviderDatabaseTests | HR review/Admin lock, invalid/pending gate, version/hash/audit và scope export |
| REQ-PER-02 / TC-PER-02 | PeriodTests/AuditRetentionTests | Chặn 5 đường mutation tháng khóa, reopen/review lại, source đổi invalidates review, log/snapshot không xóa |
| REQ-EXP-01 / TC-EXP-01 | ExportTests/AuditRetentionTests | XLSX numeric/text không formula, PDF Unicode/font/multipage và scope download |
| REQ-POL-01 / TC-POL-01 | AttendanceTimeTests/WorkforcePolicyTests | OT intersection/union, break cấu hình/phút, tách giờ/self-review rule |
| REQ-LEAVE-01 / TC-LEAVE-01 | WorkforcePolicyTests, LeaveLedgerValidationTests | Create/approve/cancel, cấm tự duyệt/hủy Approved, reason, giữ metadata/Refund một lần; HTTP Adjust âm, Grant âm/zero bị chặn |
| REQ-HR-01 / TC-HR-01 | WorkforcePolicyTests/HumanResourcesTests | Không áp sớm, VN midnight, catch-up/repeat, lock account/last admin |
| REQ-OPS-01 / TC-OPS-01 | Operational/ProviderModel/ProviderDatabase/ContractInventory |429 Retry-After/correlation/no-store, schema constraints và route anonymous whitelist |
| REQ-DOC-01 / TC-DOC-01 | scripts/verify_docs.py | UTF-8/1 H 1/links/anchors/fences/JSON và control chars |

Lệnh chạy/tạo TRX/artifact ở [19](19-cicd.md). Sample export chỉ dữ liệu giả; PDF phải extract Unicode và render/xem tất cả trang, workbook mở bằng parser độc lập. Không lấy test chỉ kiểm %PDF hoặc ZIP header làm bằng chứng layout/tiếng Việt.

## 3. Chưa được chứng minh

Browser smoke đã chạy trên API/LocalDB riêng với dữ liệu seed giả; kết quả từng kịch bản ở worklog, không phải suite E2E tự động. Camera/GPS/thiết bị thật, Storage/bucket thật, deploy/container runtime live, backup-restore, toàn bộ Manager scope và concurrency mọi luồng chưa có bằng chứng đầy đủ. Real-provider fixture không thay thử upgrade trên bản sao schema/data production. Job processor test không chứng minh uptime/scheduler ở host live.

Khi thêm nghiệp vụ: tái hiện lỗi/tiêu chí mới bằng ca meaningful, kiểm cả HTTP role/owner và persisted data sau rollback; thêm provider case khi có SQL/transaction/constraint. Không viết test mirror để tăng số lượng hoặc bỏ ca fail bằng skip không có lý do. Từng lần chạy ghi commit/environment/command/pass-fail-skip và giới hạn vào worklog.

## 4. Frontend và kiểm tra luồng qua giao diện

Từ marixa_react: `npm test`, `npm run lint`, `npm run build`. Node tests kiểm phân trang qua nhiều trang/cap server/lỗi trang giữa, tách công thường/OT, break cấu hình/ngày VN/qua tháng-năm, số dư ledger, CSV không thực thi công thức, session/remember-me/expiry và quyền prefix. Không cần API hoặc credential.

| REQ / TC | Kịch bản browser trên dữ liệu giả | Điều kiện đạt |
| --- | --- | --- |
| REQ-UI-01 / TC-UI-01 | Login, Vào/Ra ca khi ảnh/GPS tùy chọn; lịch sử | Gửi đúng rule, phản ánh bản ghi; công thường và OT theo API |
| REQ-UI-02 / TC-UI-02 | Gửi và duyệt OT; yêu cầu sửa công từ lịch sử | Pending→Approved, notes giữ lại; role/self-review theo server |
| REQ-UI-03 / TC-UI-03 | Review→lock→XLSX→reopen, chọn version | State và audit đúng; bản chốt cũ còn xuất được |
| REQ-UI-04 / TC-UI-04 | Admin sửa phút nghỉ ở Quy định | Reload vẫn giữ giá trị và khoảng nghỉ tương ứng |
| REQ-UI-04 / TC-UI-05 | HR/Admin cấp 12 ngày, Adjust -0.5; nhân viên xem sổ | Số dư 11.5, có reason/audit, Employee không có nút cấp |
| REQ-UI-03 / TC-UI-06 | Employee tạo nghỉ nửa ngày; Admin khác chủ duyệt/PDF/hủy | Deduct/Refund 0.5 một lần, số dư hoàn lại; mine không gọi endpoint staff |
| REQ-UI-05 / TC-UI-07 | Employee vào /admin/roles; phiên chưa login vào nội bộ | Thiếu role→unauthorized, thiếu phiên→login; menu không mời vào route bị chặn |
| REQ-UI-05 / TC-UI-08 | Layout 375 và desktop; dialog/loading/error/rỗng | Không tràn toàn trang, bảng cuộn riêng; native dialog giới hạn trong viewport |

Mỗi kịch bản có phạm vi riêng; chỉ đánh PASS sau quan sát kết quả thực tế. Chưa chạy thì ghi NOT VERIFIED. Browser trỏ `/api` đến loopback API có DB Marixa_Verify_Frontend_GUID; không dùng VITE config cloud cũ. File tải và ảnh bằng chứng chỉ chứa dữ liệu giả, không commit token/file nhân viên thật.
