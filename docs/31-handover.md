# 31 — Bàn giao phát triển và vận hành

> Cập nhật 2026-10-17. REVIEW; người nhận bàn giao/on-call, hạn chót và ngân sách CHƯA XÁC ĐỊNH.

Điểm bắt đầu [00](00-README.md), [PROJECT-INTAKE](PROJECT-INTAKE.md), [AI prompt](AI-START-PROMPT.md), [backend architecture](../marixa_aspnet/ARCHITECTURE.md) và [review](../marixa_aspnet/BACKEND_REVIEW.md). Code/source là hiện trạng;01 là quyết định;workloglà bằng chứng. Không dùng README/SQL Next.js cũ làm tài liệu đường chạy.

| Bàn giao | Nơi / việc người tiếp quản phải xác minh |
| --- | --- |
| Source và CI | Git commit/nhánh/PR, .github/workflows/backend.yml; theo 19 |
| Local setup | 10 + backend README; restore/build/test, không trỏ DB thật |
| DB/schema | 04/23, migration history và scripts đúng provider, duplicate preflight |
| API/quyền | 15/inventory/16, owner/staff/Manager scope và test |
| Config/secret | 17; dashboard/user-secrets, bàn giao riêng qua kênh được phép, không ghi giá trị vào docs |
| Files/backup | App_Data/ảnh/volume,22; kiểm restore file và quyền endpoint |
| Ngày hiệu lực | 27, pending-due/applied/audit; last-admin guard và catch-up |
| UI/Figma | 09/13, sai khác ảnh/GPS và UI chức năng backend mới |

Noto Sans bundled kèm OFL license trong Assets/Fonts; PDFsharp là package đã pin, các dependency còn lại theo csproj. Khi nâng runtime/package phải chạy lại provider/export/CI, không bỏ font/license khỏi publish. Không có ký số hoặc malware scanning được chứng nhận.

Tiếp quản hoàn tất khi người nhận tự chạy được kiểm tra, hiểu schema/storage/secret và xử lý ít nhất một tình huống runbook trên môi trường cô lập. Hạng mục còn mở và tiêu chí đóng tại [34](34-risks-changes.md); không tạo checklist đã ký thay người nhận.
