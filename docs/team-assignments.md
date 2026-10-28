# Phân công

| Thành viên | Module | Bảng phụ trách | Area / thư mục |
| --- | --- | --- | --- |
| TV1 – Đoàn Phan Anh (nhóm trưởng) | Xác thực, phân quyền, nhật ký hệ thống | Users, Roles, UserRoles, AuditLogs | `Areas/Admin`, `Controllers/AccountController`, `Services/` |
| TV2 – Nguyễn Xuân Dũng | Quản lý điểm, tính tiến độ/GPA, caching | Semesters, Grades, GradeScales | `Areas/Staff` |
| TV3 | Giao diện AJAX, tra cứu học phần, báo cáo thống kê | Courses, CoursePrerequisites, ElectiveGroups, ProgramCourses, Notifications | `Areas/Student` |
| TV4 – Dương Đức Hợp | Danh mục, CTĐT, sinh viên; thiết kế CSDL, kiểm thử | Faculties, Majors, Programs, Staff, Classes, Students | `Areas/Admin` (danh mục), `Data/` |

## Điểm giao nhau

| Chỗ dùng chung | Ai dùng | Quy ước |
| --- | --- | --- |
| `AuditLogService` | Mọi module | Gọi `LogAsync(action, table, recordId, old, new)` sau khi lưu |
| Đăng nhập, vai trò | Mọi module | Lấy UserId từ `ClaimTypes.NameIdentifier`; phân quyền bằng `[Authorize(Roles)]` |
| Chuyển hướng sau đăng nhập | TV2, TV3 | Staff → `/Staff/Grades` (`GradesController` trong Area `Staff`); Student → `/Student/Progress` (`ProgressController` trong Area `Student`) |
| `Grades` ↔ `Students`, `Courses` | TV2, TV3, TV4 | Điểm tổng = CC×10% + GK×30% + CK×60%; đạt khi ≥ 4.0; học lại lấy điểm cao nhất |
| Migration | Cả nhóm | Mỗi lúc một người thêm migration, báo trong nhóm |