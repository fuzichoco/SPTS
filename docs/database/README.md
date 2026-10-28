# Thiết kế database

18 bảng theo SRS nhóm, tạo bằng EF Core Code First (`SPTS/Migrations`). Cấu hình ràng buộc trong `SPTS/Data/ApplicationDbContext.cs`.

| Nhóm | Bảng | Người phụ trách |
| --- | --- | --- |
| Tài khoản | Users, Roles, UserRoles, AuditLogs | TV1 |
| Danh mục | Faculties, Majors, Programs, Staff, Classes, Students | TV4 |
| Học phần | Courses, CoursePrerequisites, ElectiveGroups, ProgramCourses, Notifications | TV3 |
| Điểm | Semesters, Grades, GradeScales | TV2 |

## Quan hệ chính

```text
Faculties 1─n Majors 1─n Programs 1─n Classes 1─n Students 1─n Grades n─1 Courses
Programs n─n Courses (ProgramCourses)        Courses n─n Courses (CoursePrerequisites)
Grades n─1 Semesters                          Users n─n Roles (UserRoles)
Users 1─0..1 Students / Staff                 Users 1─n AuditLogs, Notifications
Staff 1─n Classes (cố vấn)                    GradeScales: bảng tra cứu độc lập
```

## Ràng buộc

- Khóa kép: `UserRoles(UserId, RoleId)`, `ProgramCourses(ProgramId, CourseId)`, `CoursePrerequisites(CourseId, PrerequisiteId)`.
- Unique: Username, Email, RoleName, các cột `*Code`, `Students.UserId`, `Staff.UserId`, `Grades(StudentId, CourseId, SemesterId)`.
- Điểm `decimal(3,1)` trong khoảng 0–10 (check constraint `TotalScore`); thang 4 `decimal(2,1)`.
- Mọi khóa ngoại `ON DELETE NO ACTION`; xóa mềm bằng `IsActive` / `IsDeleted`.

## Quy tắc nghiệp vụ

- Điểm tổng = CC × 10% + GK × 30% + CK × 60%; đạt khi ≥ 4.0.
- Quy đổi: A ≥ 8.5 (4.0), B+ ≥ 8.0 (3.5), B ≥ 7.0 (3.0), C+ ≥ 6.5 (2.5), C ≥ 5.5 (2.0), D+ ≥ 5.0 (1.5), D ≥ 4.0 (1.0), F (0).
- Học lại lấy điểm cao nhất. Tiến độ (%) = tín chỉ tích lũy / tổng tín chỉ CTĐT.