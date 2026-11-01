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
            // Dữ liệu bổ sung cho báo cáo, biểu đồ (chạy một lần, khi chưa có lớp 23A2)
        public static async Task SeedMoreAsync(ApplicationDbContext db, IPasswordHasher<User> hasher)
        {
            if (await db.Classes.AnyAsync(c => c.ClassCode == "23A2")) return;
            var prog = await db.Programs.FirstAsync();
            var advisor = await db.Staffs.FirstAsync();
            var studentRole = await db.Roles.FirstAsync(r => r.RoleName == Role.Student);
            var staffRole = await db.Roles.FirstAsync(r => r.RoleName == Role.Staff);
            var courses = await db.Courses.OrderBy(c => c.CourseCode).ToListAsync();
            var hk1 = await db.Semesters.OrderBy(x => x.StartDate).FirstAsync();
            var hk2 = await db.Semesters.OrderBy(x => x.StartDate).Skip(1).FirstAsync();
            var scales = await db.GradeScales.OrderByDescending(x => x.MinScore).ToListAsync();

            // Thêm khoa, ngành, cán bộ để danh mục có nhiều bản ghi
            var fac2 = new Faculty { FacultyCode = "KT", FacultyName = "Khoa Kinh tế" };
            db.Add(new Major { MajorCode = "7340101", MajorName = "Quản trị kinh doanh", Faculty = fac2 });
            db.Add(new Major { MajorCode = "7480103", MajorName = "Kỹ thuật phần mềm", FacultyId = prog.Major?.FacultyId ?? advisor.FacultyId });
            var cvUser = new User { Username = "covan", FullName = "Trần Quốc Cố Vấn", Email = "covan@spts.edu.vn" };
            cvUser.PasswordHash = hasher.HashPassword(cvUser, "Staff@123");
            cvUser.UserRoles.Add(new UserRole { RoleId = staffRole.RoleId });
            var advisor2 = new Staff { StaffCode = "CB002", FullName = cvUser.FullName, Position = "Cố vấn học tập", FacultyId = advisor.FacultyId, User = cvUser };
            var cls2 = new Class { ClassCode = "23A2", ProgramId = prog.ProgramId, Advisor = advisor2 };
            db.AddRange(fac2, advisor2, cls2);
            db.Courses.Add(new Course { CourseCode = "IT303", CourseName = "Phát triển ứng dụng di động", Credits = 3, Description = "Lập trình ứng dụng Android/iOS đa nền tảng." });
            var hk3 = new Semester { SemesterName = "HK1 2024-2025", StartDate = new DateTime(2024, 9, 9), EndDate = new DateTime(2025, 1, 19) };
            db.Semesters.Add(hk3);

            string[] names = { "Nguyễn Hoàng Dũng", "Vũ Thị Hà", "Đỗ Minh Khôi", "Bùi Thu Lan", "Hoàng Gia Minh", "Ngô Bảo Ngọc", "Phan Thanh Phúc", "Lý Mỹ Quyên", "Đặng Văn Sơn", "Trịnh Kim Tuyến" };
            var rnd = new Random(2026);
            for (int i = 0; i < names.Length; i++)
            {
                var code = (2310004 + i).ToString();
                var u = new User { Username = "sv" + code, FullName = names[i], Email = $"sv{code}@spts.edu.vn" };
                u.PasswordHash = hasher.HashPassword(u, "Student@123");
                u.UserRoles.Add(new UserRole { RoleId = studentRole.RoleId });
                var st = new Student
                {
                    StudentCode = code, FullName = names[i], User = u, Email = u.Email, Gender = i % 2 == 0 ? "Nam" : "Nữ",
                    DateOfBirth = new DateTime(2005, i % 12 + 1, 5 + i), Class = i < 5 ? cls2 : await db.Classes.FirstAsync(c => c.ClassCode == "23A1"),
                    Status = i == 9 ? "Bảo lưu" : "Đang học"
                };
                db.Students.Add(st);
                // Mức học lực khác nhau để biểu đồ phân bố có đủ loại
                var level = 4.5 + (i % 5) * 1.1;
                for (int c = 0; c < 4; c++)
                {
                    decimal Score(double spread) => (decimal)Math.Round(Math.Clamp(level + rnd.NextDouble() * spread - spread / 2, 0, 10), 1);
                    var g = new Grade
                    {
                        Student = st, CourseId = courses[c].CourseId, Semester = c < 2 ? hk1 : hk2,
                        AttendanceScore = Score(2) + 1 > 10 ? 10 : Score(2) + 1, MidtermScore = Score(3), FinalScore = Score(4),
                        UpdatedBy = advisor.UserId, UpdatedAt = DateTime.Now
                    };
                    var total = Math.Round(g.AttendanceScore.Value * 0.1m + g.MidtermScore.Value * 0.3m + g.FinalScore.Value * 0.6m, 1, MidpointRounding.AwayFromZero);
                    var sc = scales.First(x => total >= x.MinScore);
                    g.TotalScore = total; g.LetterGrade = sc.LetterGrade; g.GradePoint = sc.GradePoint; g.IsPassed = total >= 4.0m;
                    db.Grades.Add(g);
                }
                db.Notifications.Add(new Notification { User = u, Title = "Chào mừng đến SPTS", Content = "Theo dõi tiến độ học tập, GPA và lộ trình CTĐT của bạn tại mục Tiến độ học tập." });
            }
            foreach (var su in await db.Students.Where(s => s.UserId != null).Select(s => s.UserId!.Value).ToListAsync())
                db.Notifications.Add(new Notification { UserId = su, Title = "Lịch đăng ký học phần HK1 2024-2025", Content = "Sinh viên đăng ký học phần từ 15/08 đến 25/08. Kiểm tra điều kiện tiên quyết trước khi đăng ký." });
            await db.SaveChangesAsync();
        }
    }
}
