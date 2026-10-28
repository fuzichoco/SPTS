# Xác thực và phân quyền (UC-01) – TV1

**Trạng thái:** đã làm.

- Đăng nhập bằng ASP.NET Core Identity: `SignInManager.CheckPasswordSignInAsync` + `SignInAsync`.
- Cookie `SPTS.Auth`: HttpOnly, SameSite=Lax, hết hạn sau 30 phút không hoạt động; "Ghi nhớ đăng nhập" giữ 7 ngày.
- Sai 5 lần khóa 15 phút; một thông báo lỗi chung; ghi `LOGIN_SUCCESS`, `LOGIN_FAILED`, `LOGOUT`.
- Chuyển trang theo vai trò: Admin → `/Admin/Dashboard`, Staff → `/Staff/Grades`, Student → `/Student/Progress`.
- Session lưu họ tên, vai trò, giờ đăng nhập (30 phút). Persistent Cookie `SPTS.Visit.<id>` lưu số lần truy cập, chức năng hay dùng.

File: `Controllers/AccountController.cs`, `Services/IdentityStores.cs`, `Services/VisitCookie.cs`, `Views/Account/*`.