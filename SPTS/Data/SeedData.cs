using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPTS.Models;

namespace SPTS.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext db, IPasswordHasher<User> hasher)
        {
            if (await db.Roles.AnyAsync()) return;   // Đã seed

            // 1. Vai trò
            var admin = new Role { RoleName = Role.Admin, Description = "Quản trị viên" };
            var staff = new Role { RoleName = Role.Staff, Description = "Giáo vụ khoa, cố vấn học tập" };
            var student = new Role { RoleName = Role.Student, Description = "Sinh viên" };
            db.Roles.AddRange(admin, staff, student);

            // 2. Tài khoản (mật khẩu được băm bằng PasswordHasher)
            User NewUser(string username, string fullName, string password, Role role)
            {
                var u = new User { Username = username, FullName = fullName, Email = $"{username}@spts.edu.vn" };
                u.PasswordHash = hasher.HashPassword(u, password);
                u.UserRoles.Add(new UserRole { Role = role });
                db.Users.Add(u);
                return u;
            }
            NewUser("admin", "Quản trị hệ thống", "Admin@123", admin);
            var uStaff = NewUser("giaovu", "Nguyễn Thị Giáo Vụ", "Staff@123", staff);
            var svUsers = new[]
            {
                NewUser("sv2310001", "Trần Văn An", "Student@123", student),
                NewUser("sv2310002", "Lê Thị Bình", "Student@123", student),
                NewUser("sv2310003", "Phạm Minh Cường", "Student@123", student)
            };

            // 3. Danh mục, CTĐT, lớp
            var fac = new Faculty { FacultyCode = "CNTT", FacultyName = "Khoa Công nghệ Thông tin" };
            var major = new Major { MajorCode = "7480201", MajorName = "Công nghệ thông tin", Faculty = fac };
            var prog = new TrainingProgram { ProgramCode = "CNTT-K30", ProgramName = "CTĐT Công nghệ thông tin K30", Major = major, CohortYear = 2023, TotalCredits = 130 };
            var staffInfo = new Staff { StaffCode = "CB001", User = uStaff, FullName = uStaff.FullName, Position = "Cố vấn học tập", Faculty = fac };
            var cls = new Class { ClassCode = "23A1", Program = prog, Advisor = staffInfo };
            db.AddRange(fac, major, prog, staffInfo, cls);

            // 4. Học phần
            var courses = new[]
            {
                new Course { CourseCode = "IT101", CourseName = "Nhập môn lập trình", Credits = 3 },
                new Course { CourseCode = "IT102", CourseName = "Cấu trúc dữ liệu và giải thuật", Credits = 3 },
                new Course { CourseCode = "IT201", CourseName = "Cơ sở dữ liệu", Credits = 3 },
                new Course { CourseCode = "IT202", CourseName = "Lập trình Web", Credits = 3 },
                new Course { CourseCode = "IT301", CourseName = "Lập trình Web nâng cao", Credits = 3 },
                new Course { CourseCode = "IT302", CourseName = "Trí tuệ nhân tạo", Credits = 3 }
            };
            db.Courses.AddRange(courses);
            db.CoursePrerequisites.AddRange(
                new CoursePrerequisite { Course = courses[1], Prerequisite = courses[0] },
                new CoursePrerequisite { Course = courses[4], Prerequisite = courses[3] });

            var elective = new ElectiveGroup { Program = prog, GroupName = "Tự chọn chuyên ngành", MinCredits = 3 };
            db.ElectiveGroups.Add(elective);
            for (int i = 0; i < courses.Length; i++)
            {
                bool isElective = i == courses.Length - 1;
                db.ProgramCourses.Add(new ProgramCourse
                {
                    Program = prog, Course = courses[i],
                    KnowledgeBlock = i < 2 ? "Cơ sở ngành" : "Chuyên ngành",
                    IsRequired = !isElective,
                    ElectiveGroup = isElective ? elective : null,
                    SuggestedSemester = i / 2 + 1
                });
            }

            // 5. Sinh viên
            var students = svUsers.Select((u, i) => new Student
            {
                StudentCode = u.Username.Substring(2), User = u, FullName = u.FullName,
                DateOfBirth = new DateTime(2005, i + 1, 10), Gender = i == 1 ? "Nữ" : "Nam",
                Email = u.Email, Class = cls
            }).ToArray();
            db.Students.AddRange(students);

            // 6. Học kỳ
            var hk1 = new Semester { SemesterName = "HK1 2023-2024", StartDate = new DateTime(2023, 9, 4), EndDate = new DateTime(2024, 1, 14), IsLocked = true };
            var hk2 = new Semester { SemesterName = "HK2 2023-2024", StartDate = new DateTime(2024, 2, 19), EndDate = new DateTime(2024, 6, 30) };
            db.Semesters.AddRange(hk1, hk2);

            // 7. Thang điểm
            var scales = new[]
            {
                new GradeScale { MinScore = 8.5m, MaxScore = 10m, LetterGrade = "A",  GradePoint = 4.0m },
                new GradeScale { MinScore = 8.0m, MaxScore = 8.4m, LetterGrade = "B+", GradePoint = 3.5m },
                new GradeScale { MinScore = 7.0m, MaxScore = 7.9m, LetterGrade = "B",  GradePoint = 3.0m },
                new GradeScale { MinScore = 6.5m, MaxScore = 6.9m, LetterGrade = "C+", GradePoint = 2.5m },
                new GradeScale { MinScore = 5.5m, MaxScore = 6.4m, LetterGrade = "C",  GradePoint = 2.0m },
                new GradeScale { MinScore = 5.0m, MaxScore = 5.4m, LetterGrade = "D+", GradePoint = 1.5m },
                new GradeScale { MinScore = 4.0m, MaxScore = 4.9m, LetterGrade = "D",  GradePoint = 1.0m },
                new GradeScale { MinScore = 0m,   MaxScore = 3.9m, LetterGrade = "F",  GradePoint = 0m }
            };
            db.GradeScales.AddRange(scales);

            // 8. Điểm mẫu: tổng = CC*10% + GK*30% + CK*60%
            Grade NewGrade(Student s, Course c, Semester hk, decimal cc, decimal gk, decimal ck)
            {
                var total = Math.Round(cc * 0.1m + gk * 0.3m + ck * 0.6m, 1);
                var scale = scales.First(x => total >= x.MinScore);
                return new Grade
                {
                    Student = s, Course = c, Semester = hk, AttendanceScore = cc, MidtermScore = gk, FinalScore = ck,
                    TotalScore = total, LetterGrade = scale.LetterGrade, GradePoint = scale.GradePoint,
                    IsPassed = total >= 4.0m, UpdatedByUser = uStaff, UpdatedAt = DateTime.Now
                };
            }
            db.Grades.AddRange(
                NewGrade(students[0], courses[0], hk1, 10, 8, 9),
                NewGrade(students[0], courses[1], hk1, 9, 7, 7.5m),
                NewGrade(students[0], courses[2], hk2, 10, 8.5m, 8),
                NewGrade(students[1], courses[0], hk1, 8, 6, 5),
                NewGrade(students[1], courses[1], hk1, 5, 3, 2),     // trượt
                NewGrade(students[1], courses[2], hk2, 9, 7, 6.5m),
                NewGrade(students[2], courses[0], hk1, 10, 9, 9.5m),
                NewGrade(students[2], courses[3], hk2, 10, 8, 8.5m));

            db.AuditLogs.Add(new AuditLog { Action = "SEED", TableName = "System", NewValue = "{\"note\":\"Khởi tạo dữ liệu mẫu\"}" });

            await db.SaveChangesAsync();
        }
    }
}
