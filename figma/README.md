# FIGMA — Điểm vào đồng bộ design và code Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu repo: **16/10/2026**.
> **CURRENT:** tài liệu/JSON và mã công cụ scaffold trong Git. **NOT VERIFIED:** chưa đọc file Figma live hoặc chạy plugin trong lượt chuẩn hóa.
> **EN — Codex / Agnes:** Read [the sync contract](../docs/13-figma-sync.md) first. Preserve Dev Mode copy, UI Kit first and the 375/1440 frame convention. Local scaffolding is not a verified Figma library.

## 1. Nội dung thư mục

| Artifact | Vai trò / trạng thái |
| --- | --- |
| [`tokens/tokens.json`](tokens/tokens.json) | Mirror 87 CSS token (cập nhật 21/10) của [`theme.css`](../marixa_react/src/theme.css), kèm UI metrics mục tiêu. Không phải tất cả metrics đã có thành CSS variables. |
| [`tokens/changelog.md`](tokens/changelog.md) | Nhật ký mới trên cùng; tách kết quả repo khỏi kết quả Figma live. Đọc trước khi tiếp tục task đồng bộ. |
| [`files-map.md`](files-map.md) | Inventory route thực tế + Page mục tiêu + coverage scaffold; gồm cả thiếu mapping và redirect. |
| [`plugins/setup-design-tokens.ts`](plugins/setup-design-tokens.ts) | Source scaffold: 60 định nghĩa Variables (50 màu + 9 số + 1 font), 10 component placeholders, 11 mục Page gồm cả nhóm route. Chưa xác minh build/package/thực thi. |
| [`plugins/setup-design-tokens.manifest.json`](plugins/setup-design-tokens.manifest.json) | Manifest scaffold; chưa có `main`. Không coi là package đã chạy được. |
| [`generated/`](generated/README.md) | Khu output codegen nháp nếu có task riêng sau này; chưa phải workflow chính. |

## 2. Thứ tự đọc

1. [`docs/00-README.md`](../docs/00-README.md), [`docs/13-figma-sync.md`](../docs/13-figma-sync.md): phạm vi, quyết định và quy trình.
2. [`tokens/changelog.md`](tokens/changelog.md): thay đổi gần nhất và phần còn chờ.
3. [`tokens/tokens.json`](tokens/tokens.json), [`theme.css`](../marixa_react/src/theme.css): snapshot so với giá trị code.
4. [`files-map.md`](files-map.md), [`App.jsx`](../marixa_react/src/App.jsx), [`modules.js`](../marixa_react/src/constants/modules.js): coverage và route.
5. [`docs/09-ui-design.md`](../docs/09-ui-design.md): trạng thái hiện tại, tiêu chí UI mục tiêu và kiểm tra.

## 3. Cách tiếp tục task Figma

File đích đã được chủ dự án ghi nhận: [Marixa001](https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001), key `2TYFTZweE1j2enGLUNXaq3`, team `Marixa_001`, owner `taiDz`. Quyền/plan/nội dung hiện tại **NOT VERIFIED**. Giữ **Dev Mode copy (Q4=C)**, **UI Kit trước (Q6=B)**, target **một Page/route hiển thị với hai frame mobile 375px và desktop 1440px (Q5=B)**.

Trước khi chạy plugin, kiểm tra source/manifest/build/package và API trong task được giao; không hứa “paste rồi Run” hoặc chạy lặp an toàn từ comment. Plugin chỉ dựng placeholders, chưa tạo thư viện có variants/bindings đầy đủ; 11 mục Page chưa bao phủ các route con. Tên Variables theo bảng ở [13 §4](../docs/13-figma-sync.md#4-mapping-variable-chính-xác), ví dụ `color/brand/th-primary`, `color/bg-surface/th-bg`, `typo/font`.

Khi đổi token: cập nhật CSS + mirror + changelog, kiểm tra phần code được giao; khi đổi route: cập nhật mapping và coverage. Figma live chỉ sửa trong task có phạm vi đó, dùng skill/tool phù hợp. Ghi URL/node id và bằng chứng riêng; build frontend pass không xác minh Figma.

## 4. Quy tắc lưu trữ

Chỉ lưu text references, JSON/MD và source công cụ tại đây; không commit `.fig` hoặc access token. Cập nhật tài liệu chính xác trong phạm vi được giao theo [00](../docs/00-README.md); thay đổi quyết định nghiệp vụ/bảo mật/kiến trúc cần owner decision. Trạng thái `done` cần bằng chứng tương ứng, không dùng dấu ✅ để thay cho kiểm tra live.
