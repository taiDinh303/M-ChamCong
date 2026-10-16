# 06 — Chiến lược kiểm thử Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **16/10/2026**.
> Tài liệu này là kế hoạch và ma trận kiểm thử; **không phải báo cáo các ca đã chạy**. Lượt cập nhật tài liệu chỉ kiểm tra tĩnh source và Markdown.

## 1. Hiện trạng và cách sử dụng

**CURRENT:** [package.json](../marixa_react/package.json) có `dev`, `build`, `lint`, `preview`; **không có script `test`**. [M.BE.sln](../marixa_aspnet/M.BE.sln) chưa có project test được khai báo; chưa thấy bộ unit/integration/E2E tự động trong source đang rà soát. `dotnet test` thoát 0 khi không tìm thấy test không được coi là bằng chứng kiểm thử.

Theo [00-README.md](00-README.md), CURRENT là quan sát source; TARGET/PENDING là yêu cầu hoặc phương án chưa hoàn tất; NOT VERIFIED là chưa có bằng chứng thực thi. Quy tắc ở [02-business-analysis.md](02-business-analysis.md), hợp đồng ở [05-workflow.md](05-workflow.md), dữ liệu ở [04-database-design.md](04-database-design.md). Danh sách việc cần xử lý duy trì trong [01-plan.md](01-plan.md).

**EN — AI contract:** Do not claim tests passed without command, exit code, test count and evidence. Separate characterization of current behavior from business acceptance. Keep unavailable features in the pending matrix. Use isolated test data; never run destructive, seeding or concurrency tests against production. Resolve disputed OT/self-approval policy before writing definitive acceptance assertions.

## 2. Mức kiểm tra và lệnh có sẵn

| Mức | CURRENT | TARGET/PENDING |
| --- | --- | --- |
| Tĩnh/build | .NET build, Vite build, ESLint có thể chạy khi đủ dependency. | Không thay thế kiểm thử nghiệp vụ, quyền hoặc DB. |
| Unit | Chưa có suite. | Hàm giờ công, ranh giới ngày, transition và tổng hợp ledger bằng test runner được bổ sung trong nhiệm vụ phù hợp. xUnit là một lựa chọn, chưa là dependency hiện có. |
| Integration | Chưa có suite. | Controller → service → SQL Server/PostgreSQL test, claim/ownership, transaction, upload và lỗi. TestHost là một lựa chọn chưa được cấu hình. |
| E2E | Chưa có suite tự động hoặc kết quả browser mới trong lượt này. | Nhân viên → người duyệt → Admin trên dữ liệu test, desktop/mobile, reload và lỗi mạng. |

Các lệnh sau chạy từ **repo root** sau khi setup theo [10-setup-local.md](10-setup-local.md):

```powershell
dotnet build .\marixa_aspnet\M.BE.sln
npm --prefix .\marixa_react run lint
npm --prefix .\marixa_react run build
```

Chỉ chạy lệnh test sau khi đã có suite và đọc script/project thực tế. Không dùng `npm test` như bước có sẵn. Khi báo build/lint phải giữ lỗi sẵn có và phạm vi thay đổi rõ ràng, không ghi “đạt toàn bộ” nếu một lệnh chưa chạy/thất bại. Không cần chạy app build cho một chỉnh sửa Markdown đơn thuần; kiểm tra link/cấu trúc và đối chiếu source là bằng chứng phù hợp cho thay đổi tài liệu.

## 3. Chuẩn bị dữ liệu và bằng chứng

- Tạo môi trường test riêng; biết provider, schema và cấu hình Storage đang dùng. Nhánh SQL Server/PostgreSQL phải được kiểm tra riêng khi thay đổi EF/schema, không suy từ một provider sang provider kia.
- Chuẩn bị hai nhân viên A/B, HR, Manager, Admin, thêm một Admin dự phòng cho ca kiểm tra admin cuối; thêm tài khoản nhiều role và ba role Accountant/Sale/Marketing. Ghi ID/role dùng trong test nhưng không lưu password/token/secret vào log hoặc docs.
- Chuẩn bị một rule chung 08:00–17:00, nghỉ 60 phút cho các ví dụ calculator; loại nghỉ có MaxDays và không MaxDays; ledger mẫu có Grant/Adjust/Deduct/Refund; dữ liệu Pending/Approved/Rejected/Cancelled. Đây là fixture đề xuất, không khẳng định đã được seed.
- Với thời gian, ưu tiên clock có thể điều khiển trong test thay vì đổi đồng hồ hệ điều hành. Nếu code chưa hỗ trợ clock, ghi giới hạn tái hiện và triển khai seam phù hợp trong nhiệm vụ sửa code.
- Ghi trước/sau: số Attendance, log, đơn, ledger; mốc UTC/UTC+7; response HTTP/envelope; người thao tác và nguồn dữ liệu. Khi dùng browser, ghi Network request và dữ liệu DB qua kênh được phép để chứng minh lưu thật.
- Không khởi động lại API trên DB live để thử seeder. Boot hiện có tác động schema/seed; dùng môi trường tách riêng.

## 4. Ma trận quyền ưu tiên

Các kỳ vọng ở bảng này là mục tiêu bảo vệ dữ liệu; những chỗ source đang thiếu guard phải ghi **FAIL hoặc NOT VERIFIED**, không sửa kỳ vọng để khớp lỗ hổng.

| Mã ca | Thao tác | Kỳ vọng cần đạt / chú ý CURRENT |
| --- | --- | --- |
| AUTH-01 | Không token/hết hạn gọi API được bảo vệ. | 401 hoặc phản hồi xác thực tương ứng; không đọc/ghi dữ liệu. Kiểm tra từng endpoint, không suy từ controller khác. |
| AUTH-02 | Employee A gọi Attendance get-by-id/by-employee của B hoặc checkin với employeeId=B. | Không nhận dữ liệu/ghi công của B. Source hiện chưa đối chiếu ownership ở các đường này. |
| AUTH-03 | Employee gọi LeaveRequest/get-all. | Không lộ mọi đơn; source hiện chỉ yêu cầu đăng nhập, không lọc chủ đơn. |
| AUTH-04 | A đọc/sửa/tạo OT/correction dùng employeeId hoặc attendanceId của B. | Server kiểm tra quyền, liên kết cùng chủ và ngày. UI ẩn nút không tính là pass. |
| AUTH-05 | HR/Manager/Admin giả GUID approvedBy/reviewedBy khác người đăng nhập. | Người duyệt phải xác thực từ phiên; cần kiểm tra các API nhận GUID qua body. |
| AUTH-06 | Employee hoặc ba role bổ sung gọi API quản trị. | Đúng ma trận role đã chốt; role đã seed không mặc định có quyền quản trị hoặc quyền kế toán/sale/marketing riêng. |
| AUTH-07 | Tự duyệt từng loại, cả tài khoản nhiều role. | Ghi nhận CURRENT: Leave luôn chặn; OT/correction có HR/Admin exception; Attendance chưa có self-check. Tiêu chí thống nhất chờ quyết định chủ dự án. |
| AUTH-08 | HR/Manager duyệt Leave của HR/Manager/Admin. | Đối chiếu điều kiện service và ma trận được duyệt; nhiều role không được bỏ sót. Manager đọc chi tiết Leave khác chủ hiện khác quyền by-employee, cần kiểm tra tính nhất quán. |
| AUTH-09 | Khóa/hạ quyền Admin cuối, reset/kích hoạt, token sau khóa. | Xác nhận guard thực tế và hiệu lực phiên; không đánh dấu đạt chỉ từ thông báo UI. |
| AUTH-10 | Đổi mật khẩu, upload, đọc ảnh, thay đổi role. | Kiểm tra auth/quyền theo hợp đồng; ảnh public là quyết định đang giữ. Không lấy URL đọc public làm bằng chứng API ghi ảnh được phép anonymous. |

Phạm vi controller còn lại rà theo [03-architecture.md](03-architecture.md). Danh sách trên ưu tiên các khoảng trống đã thấy, không thay thế inventory API đầy đủ trước nghiệm thu bảo mật.

## 5. Chấm công, thời gian và cấu hình

| Mã ca | Dữ liệu/thao tác | Kết quả và cách đánh giá |
| --- | --- | --- |
| ATT-01 | 08:00 → 17:00, nghỉ 12:00–13:00. | Characterization calculator CURRENT: ActualHours=8, OT=0. E2E phải có log và mốc đúng; không giả ngày làm tuần khi chưa có lịch. |
| ATT-02 | Chỉ vào hoặc chỉ ra; ra trước vào. | Calculator thiếu cặp/ra trước vào trả null. Không tự tạo mốc còn thiếu; UI thể hiện chưa hoàn chỉnh. Kiểm tra API có chấp nhận chuỗi sự kiện sai thứ tự không. |
| ATT-03 | Bấm lại cùng loại dưới 2 phút; sau 2 phút; sát biên đúng 2 phút. | CURRENT dưới cửa sổ trả alreadyRecorded=true. Test sát biên/dữ liệu DB; sau cửa sổ có thể thêm log, không nhầm giới hạn hai log/ngày. |
| ATT-04 | Hai thiết bị/request đồng thời tạo Attendance/log. | Không có ngày/log trùng ngoài quy tắc được chốt; kiểm tra constraint/transaction. Check-then-insert chưa chứng minh đạt. |
| ATT-05 | 00:30 ngày D ở Việt Nam, tức 17:30 UTC ngày D−1; thêm ca 23:59 → 00:01. | Yêu cầu ngày công Việt Nam. Source đang tạo theo UTC: ca hồi quy phải làm lộ sai khác; kiểm tra UI/trình duyệt khác múi giờ. |
| ATT-06 | 08:00 → 11:00; 12:00 → 13:00; 11:30 → 12:30. | CURRENT: lần lượt 3h, 0h, net 30 phút làm tròn 0h theo Math.Round mặc định. Đây là characterization, không tự coi làm tròn 0h là chính sách được chấp thuận. |
| ATT-07 | 08:00 → 16:30; 08:00 → 17:30. | CURRENT: net 7,5h → 8; net 8,5h → 8, OT 0,5. Chốt độ chính xác/làm tròn trước khi đổi tiêu chí nghiệm thu. |
| ATT-08 | Ca qua đêm, đúng 24h, nhiều ngày; break qua đêm. | Kiểm tra bỏ phần ngày/TimeOnly gây mất giờ; không khẳng định hỗ trợ đầy đủ ca qua đêm từ comment helper. |
| ATT-09 | Đổi giờ vào rule sang 09:00, grace >0; checkin 08:30. | Mục tiêu dùng rule chung nhất quán. CURRENT status dùng PlannedShift hoặc mốc cố định 08:00; cần tách quan sát và kỳ vọng sau sửa. |
| ATT-10 | Đổi rule, đọc lại công cũ. | CURRENT enrich dùng rule active hiện tại nên có thể đổi kết quả đọc. Cần quyết định cách giữ lịch sử khi chưa có snapshot/version rule; không tự bổ sung version/effective-date. |
| ATT-11 | Tạo hai rule active; sửa DEFAULT-CA rồi restart. | Mục tiêu một active và giữ cấu hình. Source hiện chọn active mới nhất; seeder ghi lại giờ/nghỉ/active của DEFAULT-CA. Thử trong DB test và so trước/sau. |
| ATT-12 | Duyệt ngày rồi update/checkin/correction/xóa. | Test tất cả đường ghi. Update trực tiếp có guard Approved, các đường khác chưa được bảo đảm tương đương; Approved không là kỳ khóa. |

## 6. Ảnh, GPS và lỗi mạng

| Mã ca | Kịch bản | Kỳ vọng/giới hạn |
| --- | --- | --- |
| EVD-01 | Cho phép camera, chụp lại, vào/ra, reload. | Ảnh mới đúng lượt; URL gắn log/ngày công, render sau reload. |
| EVD-02 | Chặn camera hoặc không có thiết bị. | Mục tiêu vẫn chấm được theo quyết định ảnh tùy chọn và thể hiện thiếu bằng chứng. CURRENT UI chặn vì chưa có pendingPhoto. |
| EVD-03 | Cho/chặn GPS; tọa độ trong/ngoài văn phòng. | Mục tiêu GPS tùy chọn, nếu có thì lưu đúng. CURRENT chỉ hiển thị; chưa assert geofence đã có trước khi API/service được bổ sung. |
| EVD-04 | File jpg/jpeg/png/webp <=5 MiB; quá dung lượng/đuôi sai/nội dung giả. | Xác minh giới hạn thực tế; source mới kiểm tra đuôi/length, không tuyên bố đã kiểm MIME. |
| EVD-05 | Storage cấu hình đủ/thiếu/sai key; local fallback. | Dùng đúng `SUPABASE__SERVICEKEY`; cloud object không có attendance/ prefix; local có. Kiểm tra URL, nội dung, render, độ bền restart trên môi trường test. |
| EVD-06 | Upload trả HTTP 200 nhưng envelope lỗi. | Client không coi message lỗi là photoUrl và không ghi URL sai vào công. Kiểm tra cả statusCode/code/data. |
| EVD-07 | Upload thành công → checkin thất bại; mất mạng/retry. | Không báo chấm thành công giả; đọc lại server trước retry; ghi nhận ảnh mồ côi nếu có. Chưa có transaction/cleanup hoặc queue offline. |

## 7. Sửa công, nghỉ và OT

| Mã ca | Kịch bản | Kết quả cần kiểm tra |
| --- | --- | --- |
| REQ-01 | Correction không có proposal; cặp ra <= vào; attendanceId khác chủ/ngày. | Validation và quyền đúng. Source đã kiểm ít nhất một proposal và thứ tự khi cùng có; linkage còn cần bảo vệ. |
| REQ-02 | Duyệt correction có Attendance/không có; công đổi giữa tạo và duyệt. | Override/audit đúng; log gốc giữ nguyên. CURRENT không tự tạo Attendance thiếu; BeforeJson chụp lúc tạo nên cần kiểm stale snapshot. |
| REQ-03 | Duyệt lại correction/OT đã xử lý, enum status không hợp lệ. | Không tái áp dụng hoặc lưu trạng thái ngoài transition được chốt. Enum có Cancelled không đủ chứng minh đường hủy. |
| LEA-01 | Nghỉ một ngày/nhiều ngày, cuối tuần/lễ, TotalDays nhập tay, đổi năm. | CURRENT mặc định ngày lịch gồm hai đầu; kiểm số ngày dương/phạm vi/hạn mức. Giữ chính sách phép, ghi rõ điểm chưa có quyết định thay vì tự trừ ngày nghỉ. |
| LEA-02 | Hai đơn giao khoảng, loại inactive, Employee ngoài trạng thái cho phép. | OVERLAP/INACTIVE_LEAVE_TYPE/INVALID_STATUS tương ứng; kiểm dữ liệu không ghi khi lỗi. |
| LEA-03 | Approve trong/ngoài MaxDays; từ chối; retry/hai người duyệt cùng lúc. | Deduct đúng một lần khi áp dụng hạn mức; từ chối không trừ; kiểm số dòng, transaction và số dư. |
| LEA-04 | Hủy Pending không Deduct/có Deduct tồn dư; hủy Approved. | CURRENT hủy Pending, có nhánh refund tồn dư; hủy Approved bị INVALID_STATUS. Không coi hoàn phép sau duyệt đã triển khai. |
| LEA-05 | Ledger có Adjust âm/dương, Deduct/Refund, soft-deleted entry. | So sánh cả hai đường summary và quy tắc MaxDays. Source hiện khác nhau; ghi lệch rõ, không chỉ kiểm một màn hình. |
| OT-01 | Chấm ra sau 17:00 khi chưa có đơn; thêm đơn Pending/Approved/Rejected. | Characterization: Attendance OT tự tính, chưa đọc đơn. Kiểm thử chứng minh sự độc lập; chính sách nghiệm thu cuối cùng **chờ quyết định** tự động/có duyệt/kết hợp. |
| OT-02 | OT nhập tay, thiếu cặp, end<start, qua ngày, trùng Pending. | Kiểm dữ liệu âm/không hợp lệ, rounding 2 số của đơn vs 1 số ngày công; không tự khẳng định mọi validation đã có. |
| OT-03 | Nếu chủ dự án chọn phương án có duyệt và phần giao. | **PENDING:** bổ sung kiểm phần giao thực tế/Approved, nhiều đơn và loại ngày theo quyết định rồi mới có tiêu chí cuối. |

## 8. UI và chức năng chưa có

Kiểm thử ở chiều rộng 375/768/1024/1440px; camera preview/chụp lại, nút busy và lỗi, bàn phím/focus/label, trạng thái có chữ, bảng/filter/phân trang, refresh và Back/Forward. Kiểm tra date hiển thị với trình duyệt đặt múi giờ khác Việt Nam. Các kích thước là mục tiêu test, không là kết quả đã đạt.

| Chức năng | Trạng thái test |
| --- | --- |
| Excel HR, CSV công/admin hiện thời | Có code; test đúng quyền, phạm vi dòng, dấu tiếng Việt, giá trị ngày/giờ và định dạng riêng. |
| Report version/tệp/event | Có model/API; test liên kết report/version/tệp, quyền download và lịch sử hành động. |
| Offline/IndexedDB | **TARGET/PENDING:** chưa có suite/luồng để nghiệm thu. |
| HR kiểm tra → Admin khóa/mở kỳ → snapshot | **TARGET/PENDING:** chưa triển khai hoàn chỉnh; sau khi có phải test mọi đường ghi và tính ổn định snapshot. |
| XLSX bảng công theo snapshot/PDF đơn nghỉ | **TARGET/PENDING:** chưa nghiệm thu; không đánh dấu pass từ CSV hoặc file upload. |
| Backup/restore live | **NOT VERIFIED:** phải có bài restore trên môi trường tách riêng trước khi chứng nhận khả năng phục hồi. |

## 9. Ghi kết quả và tiêu chí hoàn tất

Mỗi lần kiểm thử ghi: ngày, commit/phạm vi thay đổi, môi trường/provider, lệnh hoặc bước tái hiện, dữ liệu test, expected, actual, PASS/FAIL/NOT RUN/BLOCKED, exit code/test count khi có runner và bằng chứng đã lược secret. Lỗi hiện tại phải có triệu chứng, ảnh hưởng và việc theo dõi ở [01-plan.md](01-plan.md); kết quả thực thi ghi [worklog.md](worklog.md).

Hoàn tất một nhiệm vụ khi kiểm tra phù hợp với thay đổi đã chạy, lỗi phát sinh được xử lý, các ca chưa chạy được nêu rõ và docs phản ánh đúng kết quả. Không tuyên bố nghiệm thu production khi còn vượt quyền, mất/nhân đôi dữ liệu hoặc sai ngày/số liệu chưa giải quyết. Với chức năng chưa thuộc phạm vi triển khai, giữ trạng thái PENDING và không dùng nó để tuyên bố cả hệ thống đã hoàn tất.
