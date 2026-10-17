# 00 — Chỉ dẫn tài liệu và quy tắc làm việc Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **17/10/2026**.
> Phạm vi kiểm chứng: mã nguồn trong checkout hiện tại. Backend có build/test cô lập được ghi ở worklog; hạ tầng và Figma live chưa được xác minh.

## 1. Bắt đầu ở đây

Đọc file này → [kế hoạch và quyết định](01-plan.md) → [trạng thái công việc](worklog.md) → tài liệu đúng với nhiệm vụ. Trước khi sửa code, đọc yêu cầu nghiệp vụ liên quan và kiểm tra lại file nguồn; ngày cập nhật tài liệu không bảo đảm mọi tính năng đã chạy thành công.

| Công việc / Task | Tài liệu chính |
| --- | --- |
| Quyết định, ưu tiên và backlog / Decisions and priorities | [01-plan.md](01-plan.md) |
| Phạm vi, vai trò, chính sách / Business requirements | [02-business-analysis.md](02-business-analysis.md) |
| Kiến trúc, route và API / Architecture | [03-architecture.md](03-architecture.md) |
| Entity, quan hệ và ràng buộc / Data model | [04-database-design.md](04-database-design.md) |
| Luồng nghiệp vụ và trạng thái / Workflows | [05-workflow.md](05-workflow.md) |
| Kịch bản và bằng chứng kiểm thử / Verification | [06-testing.md](06-testing.md) |
| Tổng quan triển khai và nghiệm thu / Deployment overview | [07-deploy-overview.md](07-deploy-overview.md) |
| Cấu hình, startup và xử lý lỗi / Deployment reference | [08-deploy-detail.md](08-deploy-detail.md) |
| Nguyên tắc UI và trạng thái màn hình / UI design | [09-ui-design.md](09-ui-design.md) |
| Chạy trên máy dev / Local setup | [10-setup-local.md](10-setup-local.md) |
| Thao tác hạ tầng / Cloud setup | [11-setup-cloud.md](11-setup-cloud.md) |
| Thuật ngữ / Glossary | [12-glossary.md](12-glossary.md) |
| Đồng bộ thiết kế và code / Figma sync | [13-figma-sync.md](13-figma-sync.md) |
| Ma trận chức năng, sự kiện, đề xuất / Status and evidence | [worklog.md](worklog.md) |

Backend có [ARCHITECTURE.md](../marixa_aspnet/ARCHITECTURE.md), [BACKEND_REVIEW.md](../marixa_aspnet/BACKEND_REVIEW.md) và [AGENTS.md](../marixa_aspnet/AGENTS.md): cấu trúc/OOP, chấm điểm nghiêm khắc và quy tắc bảo trì.

Tài liệu mở rộng chọn lọc từ docs_mau ngày 17/10:

| Công việc | Tài liệu |
| --- | --- |
| Tiếp nhận/agent | [PROJECT-INTAKE](PROJECT-INTAKE.md), [AGENTS](AGENTS.md), [AI-START-PROMPT](AI-START-PROMPT.md) |
| Quyết định/API/quyền | [14 ADR](14-architecture-decisions.md), [15 API](15-api-contract.md), [inventory](api-inventory.md), [16 security](16-security-privacy.md) |
| Config/release/CI | [17](17-environment-config.md), [18](18-release-checklist.md), [19](19-cicd.md) |
| Quan sát/vận hành/restore | [20](20-observability.md), [21](21-operations-runbook.md), [22](22-backup-recovery.md) |
| Migration/tích hợp/dung lượng | [23](23-data-migration.md), [24](24-integrations.md), [25](25-performance-capacity.md) |
| Job/hướng dẫn/bàn giao/rủi ro | [27](27-offline-realtime-jobs.md), [30](30-user-guide.md), [31](31-handover.md), [34](34-risks-changes.md) |

Các mẫu 26/28/29/32/33 không áp dụng, lý do trong PROJECT-INTAKE. Tài liệu CURRENT/TARGET không tự chứng nhận sản phẩm đã hoàn thành.

## 2. Nguồn chuẩn và cách giải quyết mâu thuẫn

| Nội dung | Nguồn chuẩn / Source of truth |
| --- | --- |
| Quyết định của chủ dự án | [01-plan.md §1](01-plan.md#1-quyết-định-đang-có-hiệu-lực) và sự kiện có ngày trong worklog §4; quyết định mới được xác nhận thay quyết định cũ |
| Hành vi đang được cài đặt | Mã nguồn, cấu hình và kết quả chạy có bằng chứng; không suy ra từ tên entity hoặc checkbox |
| Trạng thái và bằng chứng từng chức năng | worklog §2–§3; kế hoạch chưa hoàn thành ở 01-plan §2 |
| Hosting do chủ dự án cung cấp | Bảng ở §5 của file này; các file khác chỉ dẫn về đây |
| CSS token | [theme.css](../marixa_react/src/theme.css); snapshot và quy trình đồng bộ ở [13-figma-sync.md](13-figma-sync.md) |
| Route và quyền giao diện | [App.jsx](../marixa_react/src/App.jsx), [modules.js](../marixa_react/src/constants/modules.js), [permission.js](../marixa_react/src/services/auth/permission.js); quyền dữ liệu phải kiểm tra ở backend |

Khi code khác yêu cầu nghiệp vụ, ghi cả hai và mở lỗi/việc cần làm. **Code xác định hiện trạng; quyết định nghiệp vụ xác định kết quả cần đạt.** Không sửa yêu cầu để biến lỗi thành hành vi đúng. Giữ sự kiện lịch sử, nhưng dùng quyết định hiện hành ở 01-plan.

## 3. Quy ước trạng thái và bằng chứng

| Nhãn | Ý nghĩa |
| --- | --- |
| **Hiện trạng / CURRENT** | Đã tìm thấy cài đặt trong mã nguồn; vẫn cần bằng chứng runtime khi kết luận chạy được |
| **Mục tiêu / TARGET** | Yêu cầu hoặc thiết kế cần đạt, có thể khác hiện trạng |
| **Chưa triển khai / PENDING** | Chưa có luồng đầy đủ trong stack hiện hành |
| **Chưa xác minh / NOT VERIFIED** | Chưa chạy kiểm tra, chưa đọc cấu hình live hoặc chưa có bằng chứng đủ |

Trong worklog, ✅ chỉ có nghĩa **có code**, 🟡 là **một phần/có sai khác**, ❌ là **chưa có**. Ghi riêng lệnh kiểm tra, ngày, kết quả và phạm vi. Build pass không chứng minh DB live hoạt động; test phải có số ca và phạm vi provider rõ; plugin scaffold không chứng minh Figma đã đồng bộ.

## 4. Quy tắc cập nhật cho Codex, Agnes và developer

1. **Tài liệu được bảo trì cùng công việc đã được người dùng giao.** Quyền chuẩn hóa bộ 00–13 đã được chủ dự án cấp ngày 16/10/2026. Quy tắc cũ “chỉ được sửa worklog / đặc tả đông băng” được thay bằng quy trình tại đây.
2. Trong phạm vi nhiệm vụ được giao, sửa lỗi tài liệu, link, tên file, mô tả hiện trạng và hướng dẫn setup dựa trên bằng chứng; cập nhật file chuyên trách cùng worklog. Việc sửa tài liệu không tự cấp quyền triển khai, đổi hạ tầng hoặc sửa toàn bộ ứng dụng.
3. Khi cần thay chính sách nghiệp vụ, quyền truy cập, tính công/phép, lưu trữ ảnh, kiến trúc hoặc phạm vi tính năng: dùng quyết định đã được chủ dự án xác nhận. Nếu chưa có quyết định và việc đó nằm ngoài nhiệm vụ được giao, ghi đề xuất + ảnh hưởng vào worklog §5 để chủ dự án chốt. Không hỏi lại việc đã được cho phép rõ ràng.
4. Giữ quyết định còn hiệu lực; không tự chuyển ảnh sang private, đổi sang Supabase Auth, phục hồi Next.js hay thêm version/ngày hiệu lực cho ca làm chung.
5. Không ghi secret vào tài liệu, chat, log hoặc commit. Ví dụ cấu hình dùng placeholder. Trước khi chỉnh file, kiểm tra git status; bảo toàn thay đổi đang có của người dùng.
6. Chỉ cập nhật trạng thái hoàn thành sau kiểm tra phù hợp. Với lỗi chưa sửa code, mô tả rõ là đã sửa tài liệu, lỗi ứng dụng vẫn mở.
7. Giữ tên 00–13 và worklog; bổ sung tài liệu 14+ phù hợp theo PROJECT-INTAKE, không tạo file không áp dụng chỉ để lấp số; mỗi file có một H 1 đúng số, ngày cập nhật, phạm vi và link liên quan. Dùng UTF-8, không ký tự điều khiển; Markdown link tương đối trong repo. Lệnh shell phải ghi rõ thư mục thực thi; đường dẫn checkout minh họa phải được thay theo máy đang chạy.
8. Một thông tin có một nơi quản lý: không lặp bảng hosting/backlog/quy tắc này. Trích dẫn hoặc tóm tắt khi cần, kèm link về nguồn.

**EN — Agent contract:** Work only within the user's authorized task. Read the current decision and actual implementation before editing. Keep CURRENT, TARGET, PENDING and NOT VERIFIED distinct. Maintain relevant docs with authorized changes; record evidence in worklog. Factual documentation corrections do not require a new approval. Unresolved policy changes require an owner decision, unless already authorized. Never treat code bugs as approved requirements or static review as runtime proof. Preserve user changes and never expose secrets.

## 5. Thông tin nhanh và hosting

- Stack trong repo: **ASP.NET Core 8 Web API** (marixa_aspnet/M.API), EF Core + Identity/JWT; **React 19 + Vite 6 + React Router 7** (marixa_react).
- DB: SQL Server local theo mặc định; PostgreSQL cloud. Supabase dùng PostgreSQL và Storage; không dùng Supabase Auth làm nguồn danh tính ứng dụng.
- Role seed: Admin, Manager, HR, Employee, Accountant, Sale, Marketing. Ba role cuối chưa có phân quyền module chuyên biệt; vẫn có thể dùng chức năng dành cho user theo logic hiện tại.
- Ảnh: bucket public marixa-photos khi có cấu hình Storage hợp lệ; thiếu cấu hình thì lưu đĩa wwwroot/uploads. Tên env chuẩn là SUPABASE__SERVICEKEY; adapter còn đọc alias SUPABASE__SERVICE_KEY để tương thích cấu hình cũ. Xem [08-deploy-detail.md](08-deploy-detail.md).
- marixa_aspnet/web (Next.js) đã xóa khỏi checkout. SQL cũ trong marixa_aspnet/supabase/ không chứng minh chức năng tương ứng đã có trong API .NET.
- Repo: [taiDinh303/M-ChamCong](https://github.com/taiDinh303/M-ChamCong).

**Hosting snapshot — chủ dự án cung cấp ngày 12/10/2026, chưa xác minh live ngày 16/10/2026.** URL/plan/region có thể thay đổi; đối chiếu dashboard khi thực hiện nhiệm vụ triển khai. render.yaml hiện khai báo plan standard, khác snapshot Free; không áp lại blueprint mà chưa kiểm tra cấu hình/cost thực tế.

| Nền tảng | URL / instance | Plan đã ghi nhận | Region đã ghi nhận |
| --- | --- | --- | --- |
| Render — API | [marixa001-asp.onrender.com](https://marixa001-asp.onrender.com) · service srv-db4hmnks728c73aq6h90 · Swagger /swagger | Free | Chưa ghi nhận |
| Vercel — React | [marixa001.vercel.app](https://marixa001.vercel.app) · team/project taiteam/marixa_001 | Hobby | Chưa ghi nhận |
| Supabase — PostgreSQL/Storage | ref zphqfcwrwitrqdeueonu | Free | Singapore |

Quyết định hiện hành chỉ có **một project Supabase**. Chưa có môi trường test cloud riêng được xác nhận; không chạy kiểm thử có ghi/xóa trên DB live. Dùng DB local hoặc môi trường test được chỉ định khi triển khai kiểm thử.

## 6. Mẫu giao việc cho Codex hoặc Agnes

```text
Đọc docs/00-README.md, docs/01-plan.md và phần hiện trạng của docs/worklog.md.
Nhiệm vụ: <mô tả thay đổi và kết quả cần đạt>.
Đối chiếu tài liệu chuyên trách với mã nguồn trước khi sửa.
Giữ các quyết định đang hiệu lực; ghi rõ nếu yêu cầu mới thay quyết định cũ.
Thực hiện trong phạm vi nhiệm vụ, cập nhật tài liệu liên quan và worklog.
Kiểm tra phù hợp, báo lệnh/kết quả, những phần chưa xác minh và việc còn mở.
```

Thay phần trong dấu `<...>` bằng yêu cầu cụ thể. Chỉ dẫn này không khiến các agent tự triển khai toàn bộ backlog hoặc thay đổi cloud khi nhiệm vụ không bao gồm việc đó.
