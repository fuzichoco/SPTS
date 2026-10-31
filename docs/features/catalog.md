# Danh mục, CTĐT, sinh viên – TV4

**Trạng thái:** đã bổ sung mã nguồn quản lý Khoa và Ngành. Các danh mục còn lại mới có bảng và dữ liệu mẫu.

## Khoa và Ngành

- `/Admin/Faculties`: danh sách, chi tiết, thêm, sửa, xác nhận xóa khoa.
- `/Admin/Majors`: danh sách, chi tiết, thêm, sửa, xác nhận xóa ngành; chọn/lọc theo khoa.
- Chỉ Admin được truy cập, kể cả khi gửi request trực tiếp. Mọi POST kiểm tra anti-forgery token.
- EditModel kiểm tra bắt buộc, độ dài; mã tối đa 10 ký tự không dấu (chữ, số, `_`, `-`). Service kiểm tra mã trùng và khoa tồn tại; unique index/FK bảo vệ khi có request đồng thời.
- Tìm theo mã/tên; sắp xếp mã/tên tăng/giảm, thêm ID để thứ tự ổn định; CountAsync trước Skip/Take; giới hạn 5–50 dòng/trang, đưa trang vượt phạm vi về trang hợp lệ. Link phân trang giữ nguyên bộ lọc.
- Khoa có ngành **hoặc cán bộ**: chặn xóa. Ngành có CTĐT: chặn xóa. Hai entity chưa có cờ xóa mềm; chỉ xóa cứng bản ghi không có tham chiếu, không đổi schema chung.
- Ghi CREATE/UPDATE/DELETE cùng dữ liệu trong một giao dịch. Lỗi lưu nhật ký làm giao dịch hoàn tác.
- Cache danh sách lựa chọn khoa 60 phút bằng IMemoryCache; vô hiệu hóa sau commit. Danh sách quản lý và kiểm tra quan hệ luôn truy vấn DB, không dùng cache để quyết định tính hợp lệ.

Luồng: Razor View → Controller (quyền, ModelState) → CatalogService (nghiệp vụ, giao dịch, cache, nhật ký) → CatalogRepository (EF Core) → SQL Server.

Các file chính: `Models/ViewModels/CatalogViewModels.cs`, `Repositories/CatalogRepository.cs`, `Services/CatalogService.cs`, `Areas/Admin/Controllers/FacultiesController.cs`, `Areas/Admin/Controllers/MajorsController.cs`, và các View cùng tên.

### Kiểm tra khi có môi trường .NET 8 và SQL Server

1. Build solution; chạy ứng dụng với DB thử nghiệm. Đăng nhập Admin và mở hai mục Khoa/Ngành.
2. Tạo khoa `TESTK`, sửa tên; kiểm tra Details, danh sách và nhật ký CREATE/UPDATE.
3. Tạo trùng mã, bỏ trống tên, nhập mã quá dài hoặc có ký tự không hợp lệ: không lưu, giữ form và hiển thị lỗi.
4. Tạo ngành `TESTN` thuộc `TESTK`; kiểm tra lọc theo khoa. Gửi FacultyId không tồn tại: báo lỗi, không thêm ngành.
5. Thử xóa `TESTK` khi có `TESTN`: bị chặn. Thử xóa khoa chỉ có cán bộ và ngành có CTĐT: cũng bị chặn.
6. Xóa `TESTN`, rồi `TESTK`: thành công; có nhật ký DELETE; danh sách chọn khoa cập nhật.
7. Với hơn 10 bản ghi: tìm kiếm + sắp xếp + chuyển trang giữ bộ lọc; page âm/quá lớn, PageSize 0/quá lớn và danh sách rỗng không gây lỗi.
8. Chưa đăng nhập: chuyển Login; Staff/Student: từ chối. POST thiếu token: 400. GET Delete chỉ hiển thị xác nhận.
9. Dùng hai phiên đồng thời tạo cùng mã hoặc thêm quan hệ trước khi xóa: DB từ chối thao tác xung đột, giao diện không lộ lỗi SQL.

Đã kiểm chứng ngày 01/11/2026: restore và build thành công (0 lỗi, 0 cảnh báo). Chạy bằng ASP.NET Core Runtime 8.0.33 tại `.local/dotnet`, SQL Server LocalDB `SPTSDev`, database riêng `SPTS_CatalogVerify`. Script `scripts/verify-catalog.py --allow-test-writes` qua 80 kiểm tra HTTP, gồm CRUD, validation, phân trang, cache lựa chọn khoa, nhật ký, CSRF và từ chối Student. Đây là kiểm thử HTTP, chưa phải kiểm tra giao diện bằng trình duyệt hoặc kiểm thử đồng thời.

Chạy lại trên máy đã chuẩn bị: `powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1`. Mở `http://localhost:5223/Account/Login`, đăng nhập tài khoản mẫu Admin, chọn Khoa/Ngành. Script dùng DB phát triển riêng, không sửa chuỗi kết nối SQL Express chung. Dừng bằng Ctrl+C; nếu cổng đang bận, dùng `-Port 5224`.

## Phần còn lại

- CRUD Programs, Courses (phối hợp TV3), Classes, Staff, Students, Semesters.
- Xóa mềm; kiểm tra trùng mã (`*Code`).
- Phối hợp TV1 khi tạo tài khoản cho sinh viên/cán bộ (`Students.UserId`, `Staff.UserId`).
