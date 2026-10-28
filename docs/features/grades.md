# Quản lý điểm, tiến độ/GPA, caching – TV2

**Trạng thái:** chưa làm.

- Area `Staff`, `GradesController` (route `/Staff/Grades`), `[Authorize(Roles = "Staff")]`.
- Nhập điểm CC/GK/CK, tự tính TotalScore, LetterGrade, GradePoint theo `GradeScales`; không sửa điểm học kỳ đã khóa (`Semesters.IsLocked`).
- Tính GPA, tín chỉ tích lũy, tiến độ (%); cache bằng `IMemoryCache`.
- Ghi nhật ký `UPDATE_GRADE` qua `AuditLogService`.