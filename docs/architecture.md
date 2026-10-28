# Kiến trúc

## Tầng

```text
Browser ──HTTP──> Controller (Areas/<Module>/Controllers)
                     │  ViewModel / JSON
                     ▼
                  Service (Services/)  ── dùng chung: AuditLogService, UserStore
                     │
                     ▼
              ApplicationDbContext (Data/) ──EF Core──> SQL Server (SPTS_DB)
```

- **Controller:** nhận request, kiểm tra dữ liệu, gọi DbContext/Service, trả View hoặc JSON.
- **ViewModel (`Models/ViewModels`):** không đưa entity thẳng ra View, nhất là `User.PasswordHash`.
- **Service:** logic dùng lại ở nhiều nơi (ghi nhật ký, Identity store, cookie).
- **DbContext:** một context chung, cấu hình Fluent API trong `OnModelCreating`.

## Bảo mật dùng chung

- ASP.NET Core Identity (`AddIdentityCore<User>` + `UserStore`) trên bảng `Users` của nhóm.
- `[Authorize(Roles = "...")]` ở cấp controller của từng Area.
- Mọi form POST có `[ValidateAntiForgeryToken]`; AJAX gửi kèm token.
- Thao tác thêm/sửa/xóa gọi `AuditLogService.LogAsync(...)`.
- Không xóa cứng dữ liệu có liên kết: dùng `IsActive` / `IsDeleted`; mọi khóa ngoại `DeleteBehavior.Restrict`.

## Thêm một chức năng

1. Thêm entity hoặc cột: sửa `Models/`, cấu hình trong `ApplicationDbContext`, rồi `dotnet ef migrations add <TenMigration>` (báo nhóm trước).
2. Tạo controller trong `Areas/<Module>/Controllers` với `[Area("<Module>")]` và `[Authorize(Roles = ...)]`.
3. Tạo View trong `Areas/<Module>/Views/<Controller>/`; Area mới cần `_ViewImports.cshtml` và `_ViewStart.cshtml`.
4. JavaScript riêng đặt ở `wwwroot/js/<trang>.js`, nạp qua `@section Scripts`.
5. Thêm link menu trong `Views/Shared/_Layout.cshtml`, bọc trong kiểm tra vai trò.
6. Ghi tài liệu vào `docs/features/<module>.md`.