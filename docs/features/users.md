# Quản lý tài khoản (UC-02) – TV1

**Trạng thái:** đã làm. Chỉ Admin.

- `/Admin/Users`: danh sách, tìm kiếm, lọc vai trò/trạng thái, sắp xếp, phân trang.
- Thêm, sửa, gán vai trò, đặt lại mật khẩu; "xóa" là khóa (`IsActive = false`); mở khóa.
- Luôn giữ ít nhất 1 Admin đang hoạt động.
- `/Admin/Users/Ajax`: XHR tải bảng, fetch tìm kiếm, jQuery thêm/khóa; JSON từ `List`, `CreateAjax`, `DeleteAjax`.

File: `Areas/Admin/Controllers/UsersController.cs`, `Areas/Admin/Views/Users/*`, `wwwroot/js/users-ajax.js`.