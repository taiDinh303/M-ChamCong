# 37 — Rà soát phần còn thiếu và tham khảo NoteTatCa

> Ngày 10/11/2026. Baseline M-ChamCong: `main` tại `0fb89ab`, pull fast-forward từ `614afdd`. Đây là rà soát source/docs local; không phải nghiệm thu production. Ưu tiên điều phối vẫn ở [01-plan](01-plan.md).

## 1. Kết luận hiện trạng

Nghiệp vụ chính đã có code: chấm công, ca, OT/sửa công, phép, kỳ công/snapshot, hồ sơ HR, công việc và kế toán chỉ đọc. Không cần bổ sung module lớn chỉ để tăng số chức năng. Phần còn thiếu tập trung ở vận hành, UI quản trị dữ liệu và tính nhất quán khi nhiều người/thiết bị cùng dùng.

| Ưu tiên | Khoảng trống | Điều kiện đóng / nguồn |
| --- | --- | --- |
| P0 trước hồ sơ thật | Private documents và ảnh fallback cần lưu bền; backup chưa bao phủ đầy đủ binary, bản ngoài máy và phục hồi khi mất host | Restore DB + file vào môi trường biệt lập, đối soát quyền tải/hash và đo thời gian; [22](22-backup-recovery.md), [34](34-risks-changes.md). Bản public DB ngày 18/10 không thay backup toàn hệ thống |
| P1 trước phát hành | Chưa có nghiệm thu mới đủ role, thao tác ghi, camera/GPS và thiết bị thật | Kiểm Employee/HR/Manager/Admin/Accountant, 403 trái quyền, kỳ khóa 409, vòng tạo–duyệt–hủy/hoàn phép; [06](06-testing.md), [18](18-release-checklist.md). Không đánh đồng test Node với browser E2E |
| P1 sản phẩm | UI danh mục dùng chung và chỉnh hợp đồng còn thiếu | Danh mục phòng ban/chức vụ/loại phép/ngày lễ/ngân hàng cần UI và quy tắc xử lý dữ liệu đang được tham chiếu. Ca đã có tạo/ngừng nhận lịch mới; không liệt kê ca là hoàn toàn chưa có. Xem [35](35-frontend-review.md), [36](36-workflow-improvements.md) |
| P1 chính sách | Scope Manager, ca qua đêm/ngày lễ/phép qua năm và cấp phép định kỳ chưa chốt đủ | Chủ dự án quyết định từng chính sách rồi gắn REQ/API/TC; giữ tính công và quyền đã xác nhận, không tự bật payroll engine/accrual |
| P1 vận hành | Worker ngày hiệu lực phụ thuộc app chạy, chưa có dashboard/cảnh báo tác vụ đến hạn | Hiện đã có catch-up/AppliedAt; cần phát hiện item quá hạn/lỗi và hướng dẫn xử lý, không chỉ báo API healthy. [27](27-offline-realtime-jobs.md), [20](20-observability.md) |
| P2 trải nghiệm | Thông báo và avatar chưa đồng bộ giữa thiết bị | Chuông tổng hợp dữ liệu nghiệp vụ; trạng thái đọc ở localStorage, avatar cũng local. Đây không phải inbox có receipt server. [NotificationBell](../marixa_react/src/components/layout/NotificationBell.jsx), [avatar](../marixa_react/src/services/avatar/avatar.js) |

## 2. Chọn lọc từ NoteTatCa — PROPOSED

Tham khảo checkout `D:/My Projects/NoteTatCa` tại HEAD `18d8708`. Checkout có thay đổi chưa commit, bao gồm console reminder và tài liệu ưu tiên; chúng chỉ là nguồn tham khảo local, không được coi đã phát hành hoặc kế thừa quyền/quyết định của Marixa. Đọc README, CURRENT-PRIORITIES, entity ReminderDelivery/NoteRevision và các adapter guest/offline. Không chỉnh sửa repo tham khảo.

| ID / thứ tự đề xuất | Ý tưởng phù hợp | Phạm vi Marixa và tiêu chí chấp nhận trước triển khai |
| --- | --- | --- |
| GAP-01 / sau gate P0 | Inbox server, đồng bộ đã đọc và liên kết đúng công việc | Bổ sung OT/sửa công cần xử lý và kết quả đơn cá nhân. Owner receipt kiểm ở API, read/read-all không tác động người khác, tổng unread đúng trên hai thiết bị, nội dung không lộ lương/hồ sơ ngoài quyền. Sự kiện/retry không tạo thông báo đôi |
| GAP-02 / cùng vận hành | Console tác vụ tự động chỉ đọc | Áp dụng cho promotion/handover đến hạn, hiển thị trạng thái chờ/đã áp/lỗi an toàn và số quá hạn. Quyền vận hành phải chốt; không sao chép console email của NoteTatCa vì Marixa chưa có pipeline tương ứng. Không thêm nút chạy lại bỏ qua guard hoặc lộ exception/credential |
| GAP-03 / sau UI cơ bản | Phát hiện sửa dữ liệu lỗi thời | Cân nhắc version/concurrency token ở hợp đồng, hồ sơ và thao tác duyệt. Hai người mở cùng bản: người thứ hai nhận conflict có hướng tải lại, không âm thầm ghi đè. Snapshot version kỳ công hiện có không đồng nghĩa mọi CRUD có chống mất cập nhật; cần rà từng endpoint và cả hai provider |
| GAP-04 / P2 | Demo dùng thử biệt lập | NoteTatCa có Guest local; Marixa hiện dùng tài khoản DEMO trên API. Đề xuất fixture/sandbox giả để tham quan không ghi vào hồ sơ thật, reset được, ghi rõ dữ liệu mẫu và không giả token/role. Không mở Guest đọc HR thật |
| GAP-05 / để sau | Offline có trạng thái đồng bộ và chống gửi trùng | Chỉ tham khảo cách tổ chức queue/conflict; không sao chép semantics note sang chấm công. Trước code phải chốt thời gian thiết bị/server, bằng chứng ảnh/GPS, replay, khóa kỳ và dữ liệu nhạy cảm trên máy. Test mất ACK/retry không tạo hai lần công; không tự ghi vào snapshot Locked |
| GAP-06 / tùy nhu cầu | Nhắc việc trong app | Có thể nhắc thiếu checkout, đơn chờ xử lý, hợp đồng sắp hết hạn. Cần chốt người nhận, thời điểm, tần suất và quyền; chống trùng theo occurrence, phân biệt pending/sent/read. Email/push là phạm vi riêng, không cam kết đã có |

Không chuyển rich-text notes, vault, mascot AI hoặc cộng tác note sang Marixa khi chưa có nhu cầu. Không ưu tiên đa múi giờ thay giờ Việt Nam hiện hành. Theme/dịch giao diện chỉ nên mở rộng sau luồng dữ liệu chính và kiểm thử accessibility.

## 3. Thứ tự thực hiện đề xuất

**Cập nhật sau phỏng vấn 10/11:** chủ dự án chọn **danh mục + hợp đồng trước**, chi tiết tại [38](38-priorities-interview.md). Danh sách dưới giữ nguồn đề xuất ban đầu, không còn là thứ tự đã chọn. Gate backup/storage vẫn phải đóng trước dùng hồ sơ thật.

1. Hoàn tất gate lưu trữ/backup và kiểm chứng release hiện hữu; xử lý lỗi kiểm thử thật nếu có.
2. Hoàn thiện UI danh mục còn thiếu và chỉnh hợp đồng sau chốt quyền/trạng thái. Kế toán tiếp tục chỉ đọc theo quyết định 21/10.
3. Chọn GAP-01 và GAP-02 nếu chủ dự án muốn mở rộng; lập contract/API/migration và test owner/concurrency riêng trước code.
4. GAP-03–06 là backlog đề xuất. Quyền sửa docs trong nhiệm vụ này không biến chúng thành quyết định nghiệp vụ được duyệt.

Kết quả kiểm tra thực tế của lượt rà soát ghi ở [worklog](worklog.md). Không deploy, migrate hoặc ghi dữ liệu live trong lượt này.
