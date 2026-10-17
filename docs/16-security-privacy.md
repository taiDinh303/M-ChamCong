# 16 — Bảo mật và dữ liệu cá nhân

> Cập nhật 2026-10-17. CURRENT từ source/test cô lập; quyền Data API, cấu hình proxy, bucket và phân quyền production: NOT VERIFIED.

## 1. Danh tính và quyền

Identity quản lý password/hash/role/securityStamp; JWT HMAC key riêng server. Password mới tối thiểu 10, có hoa/thường/số/ký tự đặc biệt. Sai mật khẩu 5 lần kích hoạt lockout; Auth có rate limit theo peer IP. JWT bị từ chối sau lockout, xóa user, đổi stamp/role hoặc tháo liên kết Employee. RememberMe lấy thời hạn cấu hình riêng, tối đa 7 ngày; không có refresh token.

| Dữ liệu/thao tác | Employee | Manager | HR | Admin |
| --- | --- | --- | --- | --- |
| Công, lương, hợp đồng, ngân hàng, BH và đơn cá nhân | Chủ dữ liệu | Staff theo service hiện hành | Staff | Staff |
| Danh sách nghỉ/công | Không | Có | Có | Có |
| Report/handover | Chủ | Nhân viên mình quản lý / manager đã gán | Có | Có |
| Review kỳ / lock-reopen | Không | Không / không | Có / không | Có / có |
| Snapshot/XLSX | Chỉ hàng của mình | Toàn kỳ | Toàn kỳ | Toàn kỳ |
| PDF đơn nghỉ | Chủ | Chủ theo contract PDF hiện tại | Có | Có |
| Tự duyệt công/OT/sửa công | Không | Không | Có | Có |
| Tự duyệt nghỉ / tự hủy nghỉ Approved | Không | Không | Không | Không |
| Account có role Admin / core role | Không | Không | Không | Có; không được làm mất admin hoạt động cuối |

Manager chưa có phạm vi phòng ban đồng nhất ở mọi module; nhiều service cũ cho staff toàn bộ dữ liệu. Đây là hiện trạng cần chủ dự án chốt trước khi mở rộng phân quyền theo đơn vị. Route attributes chi tiết ở [API inventory](api-inventory.md); ownership kiểm trong service, không dựa sidebar hoặc employeeId do client gửi. Accountant/Sale/Marketing chưa có quyền quản lý riêng.

## 2. Bề mặt tấn công và biện pháp

Ảnh chấm công public theo quyết định chủ dự án; URL là quyền đọc thực tế của người có link. Không đưa hồ sơ HR vào bucket này. Report/handover nằm App_Data ngoài web root, endpoint kiểm quyền; legacy /uploads/handovers bị chặn qua cả static providers. Adapter kiểm path/length và dọn file ghi dở; chưa có antivirus/CDR. File mới tối đa 20 MiB ở adapter, handover thêm giới hạn 10 MiB/file và 20 MiB/request; ảnh 5 MiB có signature check.

Rủi ro truy cập chéo, forged reviewer, sửa log gốc, đổi snapshot và xóa chứng từ được chặn/test. Period/event/snapshot và Deduct/Refund append-only ở SaveChanges; DBA/raw SQL có thể vượt lớp này. Hash SHA-256 phát hiện lệch payload, không phải chữ ký chống DBA sửa cả payload lẫn hash. Export dùng ô text cho dữ liệu người nhập, không phát sinh công thức XLSX.

Script PostgreSQL kỳ công bật RLS và revoke PUBLIC/anon/authenticated cho 3 bảng mới. .NET dùng kết nối DB có quyền server; không đổi sang Supabase Auth. Grants/RLS của các bảng cũ phải audit live riêng, không suy an toàn vì frontend chỉ gọi .NET.

## 3. Vận hành secret và sự cố

Không commit DB password, JWT key, Storage key, token, file nhân viên hoặc ảnh thật. Dev dùng user-secrets/env; production dùng dashboard secret. Đổi JWT key làm token cũ mất hiệu lực; reset password/role dùng stamp để thu hồi phiên. Không trust mọi X-Forwarded-For; cấu hình proxy đáng tin cần xác minh ingress trước khi lấy IP cho rate limit. Private files cần volume bền + backup trước rollout. Retention/xóa ảnh tự động chưa có chính sách; xem [22](22-backup-recovery.md), [34](34-risks-changes.md).

Package audit 17/10 phát hiện Pkcs 8.0.1 qua PDFsharp; API ghim 8.0.2 để xử lý hai advisory Microsoft [CVE-2026-84008](https://github.com/dotnet/announcements/issues/468), [CVE-2026-70162](https://github.com/dotnet/announcements/issues/454). NuGetAuditMode=all/low; CI restore warnings-as-errors. Kết quả audit cuối theo worklog, không thay đánh giá code hoặc cấu hình live.
