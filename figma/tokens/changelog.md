# SYNC CHANGELOG — Figma ↔ Marixa (mới trên cùng)

> **Tác giả & chủ tài liệu: Đinh Văn Tài.** Tạo: 16/10/2026 (scaffold).
>
> **EN — Codex / Agnes:** Every sync change gets one entry, newest on top. Each entry records date, type, direction, Figma change/reference, repo impact, worklog reference and evidence/status. Separate repo verification from live Figma verification.
> Type ∈ `token-change` | `layout-change` | `new-page` | `codegen` | `fix`.
> Status: `proposed` → `in-progress` → `repo-verified` / `Figma-verified`; `NOT VERIFIED` for checks not performed. Documentation-only fixes require static evidence; builds and live checks apply to the changed artifact. Old ✅ entries below are historical author labels, not renewed runtime verification.

## Entry format (EN)

```
### YYYY-MM-DD — Author — Title
- Type: token-change
- Direction: Figma→code | code→Figma | metadata-only
- Figma change: <variable / page / component + giá trị>
- Figma reference: <URL hoặc node-id, hoặc "TBD">
- Repo impact: <files đổi, vd theme.css --th-x: A → B>
- Worklog reference: docs/worklog.md §5 #<n> (proposal) / §4 (done)
- Evidence: <commands/checks actually run; separate repo and Figma evidence>
- Status: proposed | in-progress | repo-verified | Figma-verified; <unverified scope>
```

## Entries
### 2026-10-16 — Codex — Đính chính scaffold, route coverage và trạng thái xác minh
- Type: fix
- Direction: metadata-only
- Figma change: Không đọc/sửa Figma live. Giữ file đích, Dev Mode copy, UI Kit trước, một Page/màn hình và hai frame 375/1440 theo quyết định hiện có. 11 mục Page trong source plugin gồm 2 nhóm route, chưa đạt mapping 1:1.
- Figma reference: [Marixa001](https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001); chưa xác minh node ids/quyền/plan hiện tại.
- Repo impact: chuẩn hóa [13-figma-sync.md](../../docs/13-figma-sync.md), [09-ui-design.md](../../docs/09-ui-design.md), [README](../README.md), [files-map](../files-map.md), changelog và [generated README](../generated/README.md). Không sửa plugin/manifest, tokens.json hoặc theme.css.
- Correction: mảng plugin có **50 COLOR + 9 FLOAT + 1 STRING = 60 định nghĩa Variables**, không phải 56 (46 COLOR). CSS có 55 biến (50 màu + 4 shadow + 1 font). UI Kit có 10 placeholders, chưa có variable bindings/variant sets đầy đủ. Manifest thiếu `main`; build/package/runtime chưa xác minh.
- Coverage: App có 42 route hiển thị: 9 route khớp mục scaffold, 24 chỉ nằm trong 2 nhóm Employees/Admin, 9 chưa map; redirect ghi riêng. Các con số cũ “11 route của sản phẩm” hoặc “map 1:1 hiện tại” không còn được dùng làm hiện trạng.
- Worklog reference: [docs/worklog.md](../../docs/worklog.md), sự kiện chuẩn hóa tài liệu 16/10/2026.
- Evidence: đọc source App/modules/theme/tokens/plugin; đếm tĩnh mảng và đối chiếu inventory. Không chạy build React/plugin, không mở Figma hoặc kiểm tra visual.
- Status: repo-verified (metadata/source comparison); Figma **NOT VERIFIED**. Các nhãn/count cũ dưới đây được giữ như lịch sử, áp dụng đính chính này khi tiếp tục công việc.

### 2026-10-16 — Agnes — Chốt workflow Q4=C (Dev Mode copy) + Q6=B (UI Kit first) + tạo Figma setup plugin
- Type: fix (meta — chốt workflow + tạo plugin setup, **không đổi giá trị token**)
- Direction: không áp dụng (meta)
- Figma change: (1) Workflow chính Figma→code = **Dev Mode copy** (Q4=C): designer đổi Variable → Dev Mode → Copy CSS → cập nhật `theme.css`. Codegen (`figma/generated/`) giữ làm future option. (2) Tạo plugin setup 1-lần `figma/plugins/setup-design-tokens.ts` → Figma: 56 Variables (46 COLOR + 9 FLOAT + 1 STRING) + Page `UI Kit` (10 components) + 11 route pages × 2 frames (mobile 375×812 + desktop 1440×900).
- Figma reference: https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001
- Repo impact: tạo `figma/plugins/setup-design-tokens.ts` + `.manifest.json` · cập nhật `figma/README.md` (bảng folder + §3 workflow Q4=C) · cập nhật `docs/13-figma-sync.md` (§1 decisions + §4.1 Dev Mode + §4.5 future-only + §2 folder structure). **Không** đổi `theme.css` / `tokens.json`.
- Worklog reference: `docs/worklog.md §4` (sự kiện 16/10/2026 (3))
- Status: ✅ (plugin đã viết, sẵn sàng paste + Run trong Figma; bước "designer chạy plugin" = việc tiếp theo)

### 2026-10-16 — Agnes — Xác nhận Figma file thật + quy ước 2-frame (mobile 375 / desktop 1440)
- Type: fix (điền TBD trong `files-map.md §0`, không đổi giá trị token)
- Direction: không áp dụng (meta — chỉ ghi nhận Figma file thật)
- Figma change: file `Marixa001` (key `2TYFTZweE1j2enGLUNXaq3`) đã có; Variables + UI Kit **chưa tạo** (Q2=A) → thêm hướng dẫn tạo Variables + UI Kit vào `files-map.md §3`. Quy ước **Q5=B**: mỗi route = 1 Page chứa 2 frame (`-mobile` 375px + `-desktop` 1440px).
- Figma reference: https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001 (team `Marixa_001`, Free, owner `taiDz`)
- Repo impact: `figma/files-map.md` (điền §0, thêm §3 hướng dẫn, ghi quy ước 2-frame trước §2). Không đổi `theme.css` / `tokens.json`.
- Worklog reference: `docs/worklog.md §4` (sự kiện 16/10/2026 — xác nhận Figma file thật)
- Status: ✅ (thông tin Figma file thật; bước "tạo Variables + UI Kit trong Figma" = việc tiếp theo cho designer/dev, hướng dẫn ở `files-map.md §3`)

### 2026-10-16 — Agnes (scaffold) — Initial tokens.json mirror from theme.css
- Type: token-change (mirror only, **không đổi giá trị**)
- Direction: code→Figma (baseline)
- Figma change: chưa có Figma file (TBD trong `files-map.md §0`)
- Figma reference: TBD
- Repo impact: tạo `figma/tokens/tokens.json` mirror 50+ biến `--th-*` từ `marixa_react/src/theme.css` + UI metrics từ `docs/09-ui-design.md §2`
- Worklog reference: `docs/worklog.md §4` (sự kiện 16/10/2026 — scaffold figma/)
- Status: ✅ (scaffold — giá trị copy nguyên văn, không có quyết định thiết kế mới)
