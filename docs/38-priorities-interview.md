# 38 — Ưu tiên và phỏng vấn chuẩn bị code

> Cập nhật 10/11/2026. Nguồn: trao đổi trực tiếp với chủ dự án trong nhiệm vụ cập nhật docs/push. Đây là đặc tả chuẩn bị; chưa triển khai code, schema hay production. [01-plan](01-plan.md) là nguồn điều phối, [37](37-gap-review.md) là bằng chứng rà soát khoảng trống.

## 1. Quyết định đã xác nhận

Tiếp nhiệm vụ cùng ngày: chủ dự án giao code/thiết kế Home và các trang, chọn cả sáng/tối, nút mặt trời/mặt trăng và header/footer có màu. Hiện trạng local/giới hạn tại [39](39-catalog-ui-implementation.md); nhãn chưa triển khai trong phỏng vấn ban đầu không thay evidence mới theo từng phần ở 39.

| ID | Quyết định của chủ dự án ngày 10/11 | Giới hạn |
| --- | --- | --- |
| DEC-1110-01 | Ưu tiên danh mục + hợp đồng | Thay thứ tự đề xuất ở 37; backup vẫn là gate trước dùng hồ sơ thật, không phải điều kiện chặn viết code UI trên fixture |
| DEC-1110-02 | Manager ngang Admin trong danh mục + hợp đồng; HR thấp hơn và cần được cấp quyền phù hợp | Chỉ hai phạm vi này. Không mở quyền role/tài khoản/cấu hình/khóa kỳ cho Manager; không thay chính sách duyệt công/OT/phép đã chốt |
| DEC-1110-03 | Được sửa trực tiếp hợp đồng nhưng lưu lịch sử | Không bắt buộc phụ lục/bản mới cho mọi sửa đổi. Không đồng nghĩa xóa bản cũ, ký số hoặc tính lại lương/công |
| DEC-1110-04 | Làm rõ ở vòng tiếp: HR phụ trách mọi nhân viên, được thêm/sửa thông tin nhân sự trực tiếp, không cần duyệt; xóa vĩnh viễn/ngừng tài khoản để cấp trên xử lý | Thay cách hiểu ban đầu rằng HR phải xin duyệt từng lần sửa hồ sơ. Không tự mở quyền quản trị tài khoản/role cho HR hoặc Manager; Manager ngang Admin vẫn chỉ trong danh mục/hợp đồng |
| DEC-1110-05 | Backup miễn phí, lưu tại `D:\backup`; chủ dự án thường online từ 19h trở đi | Chưa chốt giờ chạy/lịch missed-run/retention/khóa mã hóa. Chưa có bản ngoài máy, không coi ổ D bảo vệ được khi mất cả máy. Không tạo automation hoặc đọc DB trong nhiệm vụ docs |
| DEC-1110-06 | Cấp riêng quyền Danh mục/Hợp đồng; xin quyền ngoài app; grant có hiệu lực tới thu hồi, không expiry, Admin/Manager cấp/thu hồi và xem lịch sử | Cơ chế mở rộng được đồng ý. Vòng cuối DEC-1110-08 cho HR quyền mặc định nên không bắt cấp grant trước thao tác; không tự dùng grant để mở quản trị tài khoản/role |
| DEC-1110-07 | Danh mục đã dùng không xóa vĩnh viễn; sửa có người/thời điểm/lý do/trước–sau; nguồn cũ báo conflict; không tự tính lại công/lương kỳ cũ | Chủ dự án đồng ý toàn bộ quy tắc bảo toàn dữ liệu. Quy tắc stale Pending chỉ áp dụng nếu có luồng chờ ở phạm vi khác, không tự tạo workflow Pending trong đợt này |
| DEC-1110-08 | Vòng cuối: tạm thời HR toàn quyền CRU; delete là lưu trữ/xóa mềm | Áp dụng phạm vi đang bàn: nhân sự, năm danh mục và hợp đồng; HR mặc định đọc/thêm/sửa/lưu trữ không duyệt. Không suy quyền xóa vĩnh viễn, khóa tài khoản, gán role hoặc đổi công/lương từ chữ “toàn quyền” |
| DEC-1110-09 | Không đổi EmployeeId sau tạo hợp đồng; cho sửa hồi tố có lý do/lịch sử; ngày chồng lấn cảnh báo nhưng không chặn | Không tự tính lại lương/công; không dùng phụ lục bắt buộc thay khả năng sửa trực tiếp đã chốt |

Không có deadline đã xác nhận. Các đáp án của NoteTatCa không được dùng thay quyết định Marixa.

## 2. Thống kê ưu tiên

Sáu gói công việc dưới đây là cách chia đề xuất để giao code, không phải số lượng lỗi hoặc cam kết thời gian. P0 là mức rủi ro trước dùng thật; thứ tự code do chủ dự án chọn bắt đầu bằng PRI-01/02.

| Thứ tự code | ID / mức | Gói việc | Phụ thuộc và điều kiện hoàn tất |
| --- | --- | --- | --- |
| 1 | PRI-01 / P1 | Năm danh mục: phòng ban, chức vụ, loại phép, ngày lễ/ngày làm bù, ngân hàng | Quyền theo capability và lịch sử; UI list/detail/create/edit/ngừng hoạt động theo actor, tìm/lọc/phân trang; giữ tham chiếu cũ; test server role và dữ liệu xung đột |
| 2 | PRI-02 / P1 | Sửa hợp đồng và lịch sử trước–sau theo quyền | Dùng chung cơ chế actor/permission/history của PRI-01; lịch sử riêng tư, thay đổi nguyên tử; các role hợp lệ dùng cùng màn hình, Manager có đường vào đúng phạm vi |
| 3 | PRI-03 / P0 release | Backup DB + file bền và bài phục hồi | Free, đích D:\backup; phải chốt đích ngoài máy/retention/key recovery, kiểm storage private; chưa gọi là sẵn sàng production từ việc có script |
| 4 | PRI-04 / P1 release | Kiểm chứng luồng hiện hữu và thay đổi mới | Fixture cô lập, SQL Server/PostgreSQL, browser/mobile, quyền âm, dữ liệu cũ. Có thể làm regression song hành PRI-01/02; camera/GPS thiết bị thật cần người có thiết bị xác nhận |
| 5 | PRI-05 / P2 sản phẩm, P1 vận hành | Inbox server và console job chỉ đọc | Hai nhánh nhỏ GAP-01/02 ở 37; chốt recipient, quyền xem và ngưỡng quá hạn trước code; không gộp email/retry thủ công vào bản đầu |
| Sau | PRI-06 / P2 | Demo biệt lập, avatar server, nhắc việc, offline | Chưa chọn triển khai. Offline cần contract chống trùng/late event/khóa kỳ, không tự bật ghi offline |

Thống kê phạm vi PRI-01: **5 danh mục mới cần UI**, ca đã có quản lý riêng. “Danh mục” là các lựa chọn dùng chung khi nhập dữ liệu, ví dụ chọn phòng ban Kế toán cho nhân viên hoặc ngân hàng cho tài khoản chi trả. PRI-02 là **1 luồng hợp đồng dùng chung theo quyền**, không tạo hai workflow Admin/HR khác nhau. Không quy đổi bảng này thành phần trăm hoàn thành sản phẩm.

## 3. Baseline để người code không hiểu nhầm

- CURRENT: controller Department/Position/LeaveType/HolidayCalendar/Bank có GET và create/update/soft-delete/delete; nhánh ghi đang dùng `[Authorize(Roles = "Admin")]`. Manager ngang Admin là TARGET mới, phải sửa server và test; không chỉ hiện nút frontend.
- CURRENT: EmployeeContract có `EmployeeId`, `ContractNumber` (tối đa 100), `ContractType`, `StartDate`, `EndDate`, `Note`; DTO trả CreatedTime/LastUpdatedTime. Không có draft/approval state hoặc revision history chuyên biệt trong entity đã đọc. Metadata cập nhật không thay lịch sử trước–sau.
- CURRENT: hợp đồng get-all cho Admin/Manager/HR; create/update/delete chỉ Admin ở controller. GetById/by-employee đi qua service kiểm quyền nhân viên. Cần giữ self-read cho Employee và kiểm lịch sử hợp đồng riêng; không mở lịch sử HR cho mọi tài khoản có ID.
- CURRENT: Department/Position/LeaveType/Bank/HolidayCalendar có IsActive. Ngừng hoạt động phải kiểm từng consumer không cho dùng mới nhưng vẫn đọc được dữ liệu đã tham chiếu; chỉ thêm toggle UI không bảo đảm quy tắc này.
- CURRENT: hợp đồng kiểm số trùng, enum và thứ tự ngày trong service. Khi code phải rà normalization trước kiểm trùng và race ở DB, không giả rằng check AnyAsync loại được hai request đồng thời.
- Không thay một AttendanceRule active, công thức OT, hoàn phép, AppliedAt hoặc quyền Kế toán chỉ đọc từ thay đổi role đợt này.

Nguồn: [DepartmentController](../marixa_aspnet/M.API/Controllers/HumanResources/DepartmentController.cs), [EmployeeContractController](../marixa_aspnet/M.API/Controllers/HumanResources/EmployeeContractController.cs), [EmployeeContractService](../marixa_aspnet/M.Services/Services/HumanResources/EmployeeContractService.cs), [DTO hợp đồng](../marixa_aspnet/M.ModelViews/EmployeeContractModelView/EmployeeContractModelView.cs), [entities](../marixa_aspnet/M.Contract.Repositories/Entities/EmployeeContract.cs).

## 4. Gói đặc tả triển khai — TARGET, chờ chốt mục 5

### 4.1 Quyền và luồng thay đổi

Ma trận mặc định cho đợt này: Admin/Manager ngang nhau trong năm danh mục + hợp đồng; HR có read/create/update/archive/soft-delete trực tiếp; Employee giữ self-read, Accountant không được quyền ghi HR từ role kế toán. HR không được hard-delete/ngừng tài khoản/đổi role. Admin/Manager cũng không được hard-delete danh mục đã tham chiếu. Restore bản lưu trữ đề xuất cho cùng role có quyền lưu trữ, phải kiểm lại tham chiếu và trạng thái; chưa có quyết định cho việc chuyển hồ sơ lưu trữ thành active tự động.

Capability riêng cho Danh mục và Hợp đồng theo DEC-1110-06 là hướng mở rộng. HR đã có quyền mặc định theo DEC-1110-08 nên không cần grant để dùng quyền mặc định; không làm framework quyền động tổng quát trong PRI-01/02. Trước code cấp/thu hồi phải xác định actor/role nhận grant và quyền mặc định có thể bị thu hồi hay không, không tự mở sang Employee/Accountant. Mỗi request lấy actor từ phiên server; không nhận role từ payload để quyết định quyền, không gán role Admin cho HR. Mọi nhánh phải kiểm ở controller/service, kể cả API cũ, export và history.

Luồng bản đầu: xem quyền → nhập thay đổi/lý do → server kiểm quyền hiện tại, version nguồn và tham chiếu → áp dữ liệu + history trong cùng transaction. Admin/Manager/HR thao tác trực tiếp theo ma trận trên. Thiếu quyền trả 403, hướng dẫn trao đổi cấp trên ngoài app. Không tạo bảng yêu cầu thay đổi/approve/reject chỉ để mô phỏng việc xin quyền.

Nếu nguồn đổi sau khi mở form, báo conflict và yêu cầu tải lại/gửi lại. Không dùng last-write-wins hoặc tự merge trường nhạy cảm. Retry/mất ACK không tạo history hay bản ghi trùng: thiết kế khóa request/version cùng transaction, kiểm hai provider. Mã lỗi/endpoints cụ thể phải được chốt trong [15](15-api-contract.md) cùng inventory trước code; chưa tạo route giả trong tài liệu hiện trạng.

### 4.2 Dữ liệu và lịch sử

Đề xuất lưu entity/kind/ID, thao tác, version nguồn, actor, thời điểm UTC, lý do; history ghi trước–sau có allowlist sau áp dụng thành công. Cấp/thu hồi capability có lịch sử actor/target/scope/time riêng. Ngày hợp đồng/ngày lễ là business date VN, không dịch ngày theo UTC. History append-only qua đường ghi ứng dụng; không có endpoint sửa lịch sử. Tránh lưu token/password/credential trong payload hoặc audit log.

Không dùng metadata audit làm bản sao backup. Retention chưa chốt thì không tự chạy job xóa. Thay đổi schema additive cho hai provider, có index/unique chống trùng phù hợp và migration fixture; không sửa migration lịch sử. Capability/history/version có thể cần bảng/cột mới: thiết kế cụ thể cần review quan hệ và dữ liệu cũ trước khi thực hiện. Không dựng lại lịch sử trước–sau giả cho các lần sửa trước khi tính năng được triển khai.

Danh mục ngừng hoạt động vẫn hiển thị nhãn khi xem hồ sơ cũ, không còn chọn cho bản ghi mới. Cần phân biệt giữ nguyên một liên kết cũ tới danh mục inactive với đổi liên kết sang một danh mục inactive khác. Không cascade xóa nhân viên/đơn phép/hợp đồng/tài khoản ngân hàng.

### 4.3 Màn hình và validation

- Năm danh mục dùng phong cách bảng desktop/thẻ mobile hiện có; trạng thái active/inactive, bộ lọc, phân trang, retry khi lỗi, form báo lỗi tại trường. Màn hình quản trị cần đường vào cho Manager theo quyền mới; không mở toàn bộ `/admin/*` chỉ để truy cập danh mục.
- Phòng ban: code/name/description/manager; chức vụ: code/name/description; loại phép: code/name/MaxDays/IsPaid; ngân hàng: code/name/shortName; lịch ngày: date/year/type/name. Giới hạn trường theo DTO/server, whitelist enum, trim trước kiểm trùng; không suy MaxDays là job cấp phép tự động.
- Hợp đồng: xem dữ liệu hiện hành, sửa, lý do và lịch sử. Khóa EmployeeId sau tạo cả ở server; cho sửa hồi tố, thời gian chồng lấn hiển thị cảnh báo không chặn theo DEC-1110-09. ContractType theo enum hiện hành; EndDate không trước StartDate; lỗi không đóng form hoặc làm mất dữ liệu đang nhập.
- Xác nhận sửa: xem diff trước–sau; nút theo quyền; báo dữ liệu cũ hoặc đã xử lý rõ ràng. Nhập và xuất không được trở thành đường bỏ qua history/permission. UI cấp quyền chỉ cho actor được phép, không trở thành trang gán role tổng quát.

### 4.4 REQ và tiêu chí kiểm thử

| REQ / TC | Kết quả cần đạt |
| --- | --- |
| REQ-CAT-01 / TC-CAT-ROLE | Admin/Manager được thao tác đúng phạm vi đã chốt; HR mặc định read/create/update/archive/soft-delete; Employee/Accountant không được ghi; 401 anonymous, 403 thiếu quyền. HR hard-delete/khóa tài khoản bị chặn; Manager vẫn bị chặn quản trị role/tài khoản ngoài phạm vi |
| REQ-CAT-02 / TC-CAT-REF | Ngừng danh mục không mất nhãn/tham chiếu cũ, không gán mới vào inactive; không cascade; test bật lại nếu UI cho phép |
| REQ-CAT-03 / TC-CAT-VALID | Chuỗi rỗng/space-only/quá dài, enum sai, số/ngày không hợp lệ và mã trùng sau chuẩn hóa trả lỗi; hai request mã trùng không tạo đôi |
| REQ-CTR-01 / TC-CTR-HISTORY | Sửa thành công ghi đúng before/after/actor/lý do/version; lỗi SaveChanges không để lại history giả hoặc dữ liệu áp một nửa; người không có quyền không đọc được history |
| REQ-CTR-02 / TC-CTR-EDIT | Đổi EmployeeId bị từ chối dù sửa payload; sửa hồi tố có lý do/history, chồng lấn chỉ cảnh báo; sửa không tái tính payroll/công/snapshot kỳ cũ |
| REQ-GRANT-01 / TC-GRANT-ROLE — mở rộng sau ma trận mặc định | Khi thực hiện grant: chỉ Admin/Manager cấp/thu hồi hai scope theo phạm vi đã chốt; không expiry; HR không tự cấp; không nâng role; kiểm server cả khi client giữ phiên cũ. Cần chốt target và quan hệ grant/quyền mặc định trước code; không chặn PRI-01/02 |
| REQ-CONFLICT-01 / TC-CONFLICT-EDIT | Hai người sửa cùng nguồn: báo conflict, không ghi đè; source version kiểm nguyên tử lúc áp dụng; mất ACK/retry không tạo lịch sử giả hoặc create trùng |
| REQ-UI-1110 / TC-UI-1110 | Cùng workflow ở các role hợp lệ; desktop/mobile, keyboard, validation, empty/error/retry, giữ form khi lỗi; Employee chỉ xem dữ liệu của mình |
| REQ-REG-1110 / TC-REG-1110 | Luồng sửa mới không đổi công/lương kỳ khóa, tự duyệt phép/công hiện hành hoặc guard Admin cuối; regression provider và quyền server vẫn đạt |

Trước giao code: map REQ sang endpoint/DTO/entity/migration cụ thể, ghi error code vào 15, nguồn câu trả lời vào 01; chạy test HTTP/transaction với fixture chứ không chỉ assertion text source. Build/test/rollout có evidence riêng ở worklog; thời gian thực hiện chỉ ước lượng sau chốt phạm vi.

## 5. Phỏng vấn còn mở

| Q | Đề xuất / thông tin cần chốt | Tác động khi chưa trả lời |
| --- | --- | --- |
| Q-1110-01 | Đã chốt ở vòng cuối: HR mặc định CRU và delete là lưu trữ/xóa mềm, không duyệt từng lần | DEC-1110-08 thay giả định phải cấp scope trước khi HR sửa Danh mục/Hợp đồng |
| Q-1110-02 | Đã chốt grant riêng hai scope, xin ngoài app, hiệu lực tới thu hồi, Admin/Manager cấp/thu hồi và lịch sử. Còn target nhận grant và quan hệ với quyền role mặc định | Chỉ chặn module grant mở rộng; PRI-01/02 dùng quyền mặc định đã chốt, không hỏi lại hoặc chờ grant |
| Q-1110-03 | Đã chốt quy tắc bảo toàn dữ liệu tại DEC-1110-07 | Không hỏi lại; chưa triển khai |
| Q-1110-04 | Luồng Pending từng lần sửa không còn áp dụng cho thao tác nhập/sửa thường ngày có quyền | Không xây approval engine trong đợt này; giữ nguyên duyệt phép/công theo nghiệp vụ riêng |
| Q-1110-05 | Đã chốt khóa EmployeeId, cho hồi tố có lịch sử/lý do, chồng lấn cảnh báo không chặn, không tái tính công/lương | DEC-1110-09; không hỏi lại |
| Q-1110-06 | Backup: đề xuất chạy lúc 20h giờ VN khi máy online, chạy bù khi mở máy; giữ 14 bản ngày, mã hóa. Cần bản ngoài máy, người giữ khóa và quyền thực hiện backup thật | Chỉ chuẩn bị thiết kế/runbook; chưa lập lịch hoặc truy cập credential/live |

Không còn câu hỏi nghiệp vụ chặn ma trận mặc định PRI-01/02. Q-1110-02 phần mở rộng và Q-1110-06 chỉ phỏng vấn trước công việc grant/backup tương ứng, không buộc chủ dự án trả lời tất cả roadmap trong một lượt. Restore/re-enable, filter API và mapping route cụ thể cần được đặc tả trước khi code các thao tác đó; không tự suy ra từ soft-delete. RPO/RTO và retention ở đây là PROPOSED cho đến khi được xác nhận. Nhắc lịch backup cần công cụ quản lý lịch phù hợp trong nhiệm vụ riêng; không tạo automation chỉ vì tài liệu nêu giờ đề xuất.
