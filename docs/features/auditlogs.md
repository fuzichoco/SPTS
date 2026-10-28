# Nhật ký hệ thống (UC-03) – TV1

**Trạng thái:** đã làm. Chỉ Admin, chỉ đọc.

- `/Admin/AuditLogs`: lọc theo người dùng, hành động, bảng, khoảng ngày (tối đa 90 ngày), mới nhất trước, phân trang.
- `/Admin/AuditLogs/Detail/{id}`: giá trị cũ/mới dạng JSON.
- Module khác ghi nhật ký bằng `AuditLogService.LogAsync(...)`.