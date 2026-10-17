# 22 — Sao lưu và khôi phục

> Cập nhật 2026-10-17. Quy trình REVIEW; chưa có bằng chứng restore DB + files thật.

## 1. Tài sản và mục tiêu

| Tài sản | Nơi hiện hành | Điều cần bảo toàn |
| --- | --- | --- |
| Identity, nhân sự, công/đơn/ledger/audit/snapshot | SQL Server hoặc PostgreSQL | ID/FK, password hash/stamp, version/hash và history |
| Ảnh | Supabase public bucket hoặc local uploads | Object/path và liên kết DB; backup DB không chứa binary |
| Report/handover | App_Data private, legacy uploads/handovers | Files/version/metadata và quyền download |
| Source/config | Git + dashboard/user-secrets | Commit/schema/scripts; secret backup riêng có giới hạn truy cập |

RPO/RTO CHƯA XÁC ĐỊNH. Đề xuất ban đầu RPO 24 giờ/RTO 4 giờ, backup trước mọi migration và thử restore định kỳ; chưa phải cam kết đã được chấp nhận. Retention/số bản/đích mã hóa/owner lịch chạy cũng chưa chốt. Không dựa vào backup miễn phí hoặc container filesystem mà chưa kiểm plan/thực tế.

## 2. Thực hiện và kiểm chứng

Trước backup ghi provider/version/commit, thời điểm cắt dữ liệu, schema migration và số row/files; dừng hoặc kiểm soát ghi khi cần snapshot DB-file nhất quán. Dùng công cụ native SQL Server backup hoặc pg_dump phù hợp phiên bản; xuất binary Storage và App_Data riêng. Mã hóa, giới hạn quyền, checksum manifest và bản ngoài host; không commit dump hay file thật.

Restore vào DB/storage/volume cô lập được chỉ định, cấu hình init/seed/job tắt hoặc không chạy API trước khi xác minh đích. Không dùng credential/bucket production cho bài thử. Kiểm counts/FK/index/unique, login tài khoản thử, owner 403, file stream, snapshot hash/version, Deduct/Refund và job due. Tắt gửi thông báo/tích hợp có side effect khi restore. Chỉ đổi traffic sau xác minh và quyết định vận hành.

Ghi backup ID, kích thước/checksum, thời gian restore đo được, ca kiểm và sai khác; xóa bản thử theo quyền dữ liệu. Snapshot/hash và TRX không thay bản backup, cũng không chứng minh restore được. Liên quan [21](21-operations-runbook.md), [23](23-data-migration.md).
