# MARIXA React

Frontend React/Vite được tổ chức theo module, kế thừa các trang nghiệp vụ từ `ChamCong`.

## Chạy local

1. Khởi động API .NET trong `../marixa_aspnet/M.API` bằng profile `M.API` (HTTP `http://localhost:57432`).
2. Tại thư mục này, chạy `npm install` một lần, sau đó `npm run dev`.
3. Mở URL Vite hiển thị trong terminal. Vite chuyển tiếp `/api` và `/uploads` tới API .NET; không cần bật CORS cho frontend local.

Có thể đặt `VITE_API_PROXY_TARGET` trong `.env.local` nếu API chạy ở địa chỉ khác. `VITE_API_BASE_URL` mặc định là `/api`, phù hợp với proxy local và khi frontend được ASP.NET host cùng origin.

## Cấu trúc mã nguồn

- `src/main.jsx`, `src/App.jsx`, `src/index.css`: điểm khởi động, định tuyến và kiểu nền ứng dụng.
- `src/components`: thành phần dùng chung và bố cục.
- `src/modules`: các tính năng theo nghiệp vụ (`admin`, `employees`/HR, `me`, `home`, `login`, `work`, `profile`, `activate`, `unauthorized`). API, trang, hook và thành phần riêng được đặt trong module tương ứng.
- `src/services`: client API, xác thực, phân quyền, avatar và ngôn ngữ.
- `src/constants`, `src/utils`, `src/assets`: cấu hình dùng chung, tiện ích và tài nguyên tĩnh.

Các trang hiện tại dùng hợp đồng API của `M.API` (`/api/...`). Backend Next.js mới trong `marixa_aspnet/web` cung cấp hợp đồng `/api/v1/...` khác và không tương thích trực tiếp với các module này.

## Lệnh

- `npm run dev`: chạy môi trường phát triển.
- `npm run build`: tạo bản build production.
- `npm run lint`: kiểm tra ESLint.
