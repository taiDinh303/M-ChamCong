# 07— Triển khai Render + Vercel + Supabase cho Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Bộ stack triển khai **đang chạy**: API .NET (Render, Docker), web SPA React (Vercel), PostgreSQL + Storage (Supabase). Tài liệu này mô tả cách cấu hình, biến môi trường và các điểm cần theo dõi.

## 0. Cách đọc (dành cho AI)

- Chi tiết từng bước Render/Vercel/Supabase: xem `08-deploy-detail.md` và `11-setup-cloud.md`.
- File blueprint deploy: `render.yaml` (repo root), `marixa_react/vercel.json`, `marixa_aspnet/Dockerfile`.

## 0a. Hosting đang chạy

> **Nguồn duy nhất cho bảng instance hosting (Render / Vercel / Supabase) là [`00-README.md`](00-README.md)** (mục "Quy tắc đọc / ghi tài liệu"). File này không lặp bảng hosting — chỉ link về.

- Quyết định 12/10/2026: chỉ còn **1 project Supabase** `zphqfcwrwitrqdeueonu` (Singapore, Free); bỏ hẳn các ref cũ test/prod khỏi tài liệu.
- Biến `SUPABASE__URL` trong `render.yaml` trỏ tới project này.
- API public: `https://marixa001-asp.onrender.com/api` · Web public: `https://marixa001.vercel.app`.
## Nội dung chi tiết đã gộp về `08`

> Từ 14/10/2026, các phần "biến môi trường", "bước triển khai chi tiết" và "khắc phục sự cố" của `06` đã được **gộp vào `08-deploy-detail.md`** (nguồn duy nhất cho deploy kỹ thuật). File `06` giờ chỉ còn phần overview + hosting link (trên) + checklist nghiệm thu (dưới).

### Checklist nghiệm thu triển khai (giữ ở đây)

- [ ] Render API deploy, `/swagger` hoạt động, kết nối đúng PostgreSQL (session pooler, sslmode).
- [ ] Vercel web build, `VITE_API_BASE_URL` trỏ đúng Render; CORS không lỗi.
- [ ] Login/chấm/upload ảnh/duyệt/khóa kỳ chạy được end-to-end bằng URL thật.
- [ ] Không có secret (DB/JWT/Supabase) trong Git; chỉ `.env.example` mô tả.
- [ ] Có bản sao lưu DB + ảnh, đã thử restore trên môi trường test.

> Chi tiết config/step/troubleshooting: xem [08-deploy-detail.md](08-deploy-detail.md). Test: [06-testing.md](06-testing.md).
