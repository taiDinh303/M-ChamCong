# MARIXA React

React 19 / Vite SPA dùng API ASP.NET Core 8 tại `../marixa_aspnet/M.API`. Đọc [quyết định](../docs/01-plan.md), [UI](../docs/09-ui-design.md), [API](../docs/15-api-contract.md) và [worklog](../docs/worklog.md) trước khi thay nghiệp vụ. Không có backend Next.js trong đường chạy hiện hành.

## Chạy và kiểm tra

Khởi động API với database local riêng theo [setup](../docs/10-setup-local.md). Từ thư mục `marixa_react`:

```powershell
npm ci
npm test
npm run lint
npm run build
npm run dev
```

Vite proxy `/api` và `/uploads` đến `http://localhost:57432`. Đặt `VITE_API_PROXY_TARGET` trong `.env.local` khi API local dùng cổng khác. `VITE_API_BASE_URL` mặc định `/api`; web/API khác origin cần URL có `/api` lúc build và CORS ở API. `VITE_*` là cấu hình public, không chứa JWT signing key, mật khẩu DB hoặc Supabase service key. Không dùng database/bucket thật cho kiểm thử.

## Cấu trúc và hợp đồng

- `src/App.jsx`, `constants/modules.js`, `services/auth/permission.js`: route, menu và quyền. `RequireSession` bảo vệ phiên; `RequireModule` áp quyền theo đường dẫn dài nhất. API vẫn quyết định role/owner.
- `src/services/api`: Axios, lỗi API/file, collection response và phân trang. Dùng `getAllPages` cho API phân trang khi cần lọc/tổng hợp toàn bộ; không giả định `pageSize=500` lấy hết dữ liệu.
- `src/modules/{me,hr,admin,work,...}`: trang, API, hook và component theo nghiệp vụ. Các luồng được duyệt ngày 17/10 dùng trang hiện có: OT/sửa công trong Lịch sử công; kỳ công/XLSX trong Thống kê; sổ phép trong Nghỉ phép; chỉnh rule trong Quy định.
- `src/utils/vnTime.js`: ngày nghiệp vụ VN, giờ nhập theo UTC+7, công thường/OT từ API. Không suy OT từ giờ ra trễ hoặc tự làm tròn công thành số nguyên.
- `src/modules/me/workforce.css`: giao diện bổ sung dùng token `theme.css`; giữ layout chính. Hộp thoại mới dùng native dialog để khóa focus và hỗ trợ Escape.
- `tests`: Node test runner, không cần API/credential. GitHub Actions chạy npm ci → test → lint → build tại [frontend.yml](../.github/workflows/frontend.yml).

Không chọn Ghi nhớ đăng nhập: JWT ở `sessionStorage`; có chọn: `localStorage`. Hết hạn/401 đúng phiên sẽ yêu cầu đăng nhập lại. Không lưu mật khẩu. API nghỉ cá nhân dùng `/LeaveRequest/mine`; `/by-employee` và `/get-all` dành staff theo quyền server.

## Kiểm tra giao diện và phát hành

Kịch bản/bằng chứng có phạm vi tại [06-testing](../docs/06-testing.md) và worklog; Node tests/lint/build không thay nghiệm thu thiết bị, camera/GPS thật hoặc production. Frontend cần schema backend tương thích. Lỗi 500 thiếu cột/bảng phải nâng schema theo [23](../docs/23-data-migration.md); không nuốt lỗi rồi hiển thị dữ liệu bằng 0. Chủ dự án tự chạy SQL live trong nhiệm vụ này.

Các phần chưa triển khai: offline queue, tự cấp phép hằng tháng, geofence, quyền riêng cho Accountant/Sale/Marketing. Không suy ra đã có tính năng từ nhãn menu. Trước rollout theo [18-release-checklist](../docs/18-release-checklist.md).
