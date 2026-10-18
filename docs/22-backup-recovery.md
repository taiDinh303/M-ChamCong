# 22 — Sao lưu và khôi phục

> Cập nhật 2026-10-18. Đã thử khôi phục snapshot schema/dữ liệu public trước migration trên PostgreSQL 17.11 cô lập. Khôi phục binary Storage/App_Data và quy trình phục hồi toàn bộ hạ tầng chưa được nghiệm thu.

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

## 3. Bản sao lưu phục vụ nâng schema ngày 18/10/2026

CURRENT: chủ dự án đã cấp quyền sao lưu/cập nhật live. Snapshot `marixa-public-snapshot-v1` lấy schema metadata và dữ liệu của toàn bộ 43 bảng public trong một câu SELECT nhất quán; bao gồm 688 cột, 54 bản ghi, constraints/indexes/sequences/RLS/policies và định nghĩa function public. Chỉ ciphertext được xuất từ MCP: [pgcrypto](https://www.postgresql.org/docs/17/pgcrypto.html) mã hóa OpenPGP AES-256 bằng public key RSA-3072. Private key có passphrase; passphrase được bảo vệ bằng Windows DPAPI CurrentUser. Thư mục backup nằm ngoài Git, ACL giới hạn tài khoản Windows hiện tại và SYSTEM; vị trí cụ thể giao riêng cho chủ dự án.

Đã xác minh giải mã với SHA-256 của plaintext gốc và khôi phục trên PostgreSQL 17.11 loopback, không chạy API/seed/job trên bản sao. Phiên restore đặt TimeZone UTC để so timestamptz; kiểm 43/43 bảng, 54 FK và 54 bản ghi. Ba script nâng cấp chạy hai lần trên bản sao và giữ toàn bộ dữ liệu cũ; kiểm materialization các model công/ca/phép/bàn giao/kỳ công bằng EF. Evidence và tên migration live tại worklog.

Phạm vi bản sao này là schema/dữ liệu ứng dụng trong public; binary Storage, App_Data, dashboard secrets và các schema nội bộ Supabase không nằm trong snapshot. Chưa có bằng chứng bản backup ngoài máy hoặc RPO/RTO đo được. DPAPI phụ thuộc tài khoản Windows hiện tại; cần cơ chế quản lý khóa và bản sao ngoài máy được chủ dự án chọn trước khi coi đây là quy trình disaster recovery hoàn chỉnh. Công cụ native pg_dump vẫn là quy trình ưu tiên khi có kết nối trực tiếp phù hợp.
