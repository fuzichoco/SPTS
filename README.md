# SPTS – Hệ thống theo dõi tiến độ học tập sinh viên

Repository của nhóm: [fuzichoco/SPTS](https://github.com/fuzichoco/SPTS) · nhánh tích hợp `main` · Private.

Bài tập lớn môn **Lập trình Web nâng cao** – Đề tài 8, Khoa CNTT, Trường ĐH Mở Hà Nội. Ứng dụng giúp sinh viên tra cứu tiến độ học tập, điểm số, học phần đã học và còn thiếu; giáo vụ quản lý điểm; quản trị viên quản lý tài khoản và nhật ký hệ thống. Viết bằng ASP.NET Core MVC (.NET 8), Entity Framework Core 8 và SQL Server, theo hướng Controller → Service → DbContext.

Hiện có: lớp dữ liệu đủ 18 bảng (migration + dữ liệu mẫu), đăng nhập bằng ASP.NET Core Identity trên bảng `Users` của nhóm, phân quyền theo vai trò, quản lý tài khoản (CRUD, tìm kiếm, sắp xếp, phân trang, trang AJAX), nhật ký hệ thống, Dashboard quản trị, Persistent Cookie và Session. Trang của giáo vụ (`/Staff/Grades`) và sinh viên (`/Student/Progress`) chưa làm; Area tương ứng là nơi triển khai tiếp. Đọc [thiết kế database](docs/database/README.md) và [hướng dẫn môi trường](docs/setup.md).

## Bắt đầu

1. Cài .NET SDK 8 trở lên và SQL Server Express (instance `.\SQLEXPRESS`).
2. Mở `SPTS.sln` bằng Visual Studio 2022, hoặc mở thư mục bằng VS Code.
3. Chạy ứng dụng; lần chạy đầu tự tạo database `SPTS_DB` và nạp dữ liệu mẫu.

```powershell
cd SPTS
dotnet run
```

Mở http://localhost:5223/Account/Login, đăng nhập `admin` / `Admin@123`. Tài khoản mẫu khác xem trong [hướng dẫn môi trường](docs/setup.md). Máy dùng LocalDB hoặc instance khác thì đổi `ConnectionStrings:DefaultConnection` (xem phần Cấu hình riêng).

## Cấu trúc

```text
SPTS/
├── Program.cs                   # Đăng ký DI, Identity, cookie, session, route
├── Data/
│   ├── ApplicationDbContext.cs  # 18 DbSet, Fluent API (khóa kép, unique, decimal)
│   └── SeedData.cs              # Dữ liệu mẫu
├── Models/                      # 18 entity theo SRS
│   └── ViewModels/              # ViewModel/EditModel cho View và JSON
├── Services/
│   ├── AuditLogService.cs       # Ghi nhật ký, dùng chung mọi module
│   ├── IdentityStores.cs        # UserStore cho ASP.NET Core Identity
│   └── VisitCookie.cs           # Persistent Cookie: lượt truy cập, chức năng hay dùng
├── Controllers/                 # Account (đăng nhập, đăng xuất), Home
├── Areas/
│   ├── Admin/                   # UC-01..03, Phan Anh: Users, AuditLogs, Dashboard
│   ├── Staff/                   # Quản lý điểm, tiến độ/GPA, caching – Dũng (chưa làm)
│   └── Student/                 # Tra cứu tiến độ, môn còn thiếu, báo cáo – TV3 (chưa làm)
├── Views/                       # Layout, Account, trang lỗi
├── Migrations/                  # EF Core Code First
└── wwwroot/js/                  # JavaScript từng trang (users-ajax.js …)

docs/                            # Thiết kế, phân công, quy ước, hướng dẫn
.github/                         # Mẫu Issue, PR, kiểm tra build tự động
SPTS.sln                         # Solution mở bằng Visual Studio
```

Mỗi module là một Area (`Areas/<Module>/Controllers`, `Areas/<Module>/Views`); entity, DbContext và service dùng chung đặt ở thư mục gốc của project. Cả nhóm chạy toàn bộ ứng dụng từ một project.

## Tài liệu làm việc nhóm

- [Thiết kế database, danh sách bảng và quan hệ](docs/database/README.md)
- [Phân công và điểm giao nhau](docs/team-assignments.md)
- [Kiến trúc và cách thêm một chức năng](docs/architecture.md)
- [Quy ước đặt tên](docs/naming-conventions.md)
- [Quy trình nhánh, review và ghép code](CONTRIBUTING.md)
- [Lấy code và thiết lập làm việc trên GitHub](docs/github-setup.md)
- [Quyết định kỹ thuật và nội dung còn cần chốt](docs/decisions.md)
- [Kết quả kiểm tra](docs/verification.md)
- Chức năng: [Xác thực](docs/features/auth.md) · [Tài khoản](docs/features/users.md) · [Nhật ký](docs/features/auditlogs.md) · [Điểm](docs/features/grades.md) · [Tiến độ](docs/features/progress.md) · [Danh mục](docs/features/catalog.md)

## Cấu hình riêng

`bin/`, `obj/`, `.vs/`, file `*.user` và `appsettings.*.local.json` được loại khỏi Git. Chuỗi kết nối dùng Windows Authentication nên không chứa mật khẩu. Máy nào cần chuỗi khác thì đặt riêng bằng `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi>"` trong thư mục `SPTS`, không sửa file chung. Chưa deploy lên server.