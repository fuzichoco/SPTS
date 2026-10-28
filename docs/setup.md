# Hướng dẫn môi trường

## Cần cài

| Công cụ | Phiên bản | Ghi chú |
| --- | --- | --- |
| .NET SDK | 8.0 trở lên | Project target `net8.0` |
| SQL Server Express | 2019 trở lên | Instance `.\SQLEXPRESS`, đăng nhập Windows |
| Visual Studio 2022 hoặc VS Code | | VS Code cần extension C# Dev Kit |
| SSMS hoặc extension SQL Server (mssql) | | Xem dữ liệu |

## Chạy lần đầu

```powershell
git clone https://github.com/fuzichoco/SPTS.git
cd SPTS/SPTS
dotnet tool restore     # cài dotnet-ef theo dotnet-tools.json
dotnet run
```

Khi khởi động, `Program.cs` gọi `Database.MigrateAsync()` rồi `SeedData.InitializeAsync()`: database `SPTS_DB` được tạo và nạp dữ liệu mẫu nếu còn trống.

## Tài khoản mẫu

| Tài khoản | Mật khẩu | Vai trò | Trang sau khi đăng nhập |
| --- | --- | --- | --- |
| admin | Admin@123 | Admin | /Admin/Dashboard |
| giaovu | Staff@123 | Staff | /Staff/Grades |
| sv2310001, sv2310002, sv2310003 | Student@123 | Student | /Student/Progress |

## Chuỗi kết nối khác

Máy không có SQL Express (dùng LocalDB hoặc instance khác), đặt chuỗi riêng, không sửa `appsettings.json`:

```powershell
cd SPTS
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=SPTS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## Làm lại database từ đầu

```powershell
dotnet ef database drop -f
dotnet run
```

## Lỗi thường gặp

| Lỗi | Cách xử lý |
| --- | --- |
| `file is locked by SPTS` | Tắt bản đang chạy: `Get-Process SPTS \| Stop-Process -Force` |
| Lỗi kết nối SQL | Kiểm tra service `MSSQL$SQLEXPRESS` đang chạy hoặc chuỗi kết nối |
| Trang 404 ở /Staff, /Student | Bình thường: các Area này chưa làm |