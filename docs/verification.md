# Kết quả kiểm tra

Kiểm tra thủ công và bằng script HTTP trên máy phát triển (SQL Server Express, .NET 8).

| Chức năng | Trường hợp | Kết quả |
| --- | --- | --- |
| Database | Migration `InitialCreate` tạo 18 bảng + `__EFMigrationsHistory`; seed 6 tài khoản, 8 điểm | Đạt |
| Đăng nhập | admin → `/Admin/Dashboard`; sinh viên → `/Student/Portal` | Đạt |
| Phân quyền | Chưa đăng nhập vào `/Admin/Users` → Login; sinh viên → AccessDenied | Đạt |
| Khóa tài khoản | Sai 5 lần → khóa 15 phút, nhập đúng vẫn bị chặn | Đạt |
| Cookie | Không ghi nhớ: `SPTS.Auth` không có expires; ghi nhớ: 7 ngày; `SPTS.Visit.<id>` 30 ngày | Đạt |
| Tài khoản | Thêm, sửa, khóa, mở khóa; chặn khóa Admin cuối; kiểm tra trùng Username/Email | Đạt |
| Query | Tìm kiếm, sắp xếp, phân trang Skip/Take | Đạt |
| AJAX | XHR tải bảng, fetch tìm kiếm, jQuery thêm/khóa, trả JSON và mã 400 khi lỗi | Đạt |
| Nhật ký | Ghi CREATE, UPDATE, LOCK, UNLOCK, LOGIN_SUCCESS, LOGIN_FAILED, LOGOUT | Đạt |
| Staff, Student | Trang nghiệp vụ | Chưa làm |