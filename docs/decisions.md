# Quyết định kỹ thuật

| # | Quyết định | Lý do |
| --- | --- | --- |
| 1 | ASP.NET Core MVC .NET 8 + EF Core 8 Code First | Theo SRS nhóm và nội dung môn học |
| 2 | SQL Server Express thay LocalDB | LocalDB trên máy phát triển khởi động không ổn định |
| 3 | Entity CTĐT đặt tên `TrainingProgram`, bảng `Programs` | Tránh trùng lớp `Program` của `Program.cs` |
| 4 | Mọi khóa ngoại `Restrict`, xóa mềm bằng `IsActive`/`IsDeleted` | Quy tắc dữ liệu: không xóa cứng dữ liệu có liên kết |
| 5 | Identity dùng `UserStore` tự viết trên bảng `Users` | Giữ đúng thiết kế CSDL, không thêm bảng `AspNetUsers` |
| 6 | SecurityStamp kiểm tra mỗi request | Khóa tài khoản hoặc đổi mật khẩu thì cookie cũ mất hiệu lực ngay |
| 7 | Mỗi module một Area | Tách code theo người phụ trách, giảm xung đột |

## Còn cần chốt

- [ ] Username GitHub của từng thành viên để cập nhật `CODEOWNERS`.
- [ ] TV2: cache tiến độ/GPA bằng `IMemoryCache`, thời gian hết hạn.
- [ ] TV3: thư viện biểu đồ báo cáo (Chart.js) và các chỉ số thống kê.
- [ ] Có làm trang quản lý vai trò (`RolesController`) không.
- [ ] Môi trường deploy (nếu có).