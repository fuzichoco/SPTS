using Microsoft.EntityFrameworkCore;
using SPTS.Models;

namespace SPTS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<Major> Majors => Set<Major>();
        public DbSet<TrainingProgram> Programs => Set<TrainingProgram>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
        public DbSet<ProgramCourse> ProgramCourses => Set<ProgramCourse>();
        public DbSet<ElectiveGroup> ElectiveGroups => Set<ElectiveGroup>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<Staff> Staffs => Set<Staff>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Semester> Semesters => Set<Semester>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<GradeScale> GradeScales => Set<GradeScale>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Khóa kép
            b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
            b.Entity<ProgramCourse>().HasKey(x => new { x.ProgramId, x.CourseId });
            b.Entity<CoursePrerequisite>().HasKey(x => new { x.CourseId, x.PrerequisiteId });

            // Ràng buộc unique
            b.Entity<User>().HasIndex(x => x.Username).IsUnique();
            b.Entity<User>().HasIndex(x => x.Email).IsUnique();
            b.Entity<Role>().HasIndex(x => x.RoleName).IsUnique();
            b.Entity<Faculty>().HasIndex(x => x.FacultyCode).IsUnique();
            b.Entity<Major>().HasIndex(x => x.MajorCode).IsUnique();
            b.Entity<Staff>().HasIndex(x => x.StaffCode).IsUnique();
            b.Entity<Course>().HasIndex(x => x.CourseCode).IsUnique();
            b.Entity<TrainingProgram>().HasIndex(x => x.ProgramCode).IsUnique();
            b.Entity<Class>().HasIndex(x => x.ClassCode).IsUnique();
            b.Entity<Student>().HasIndex(x => x.StudentCode).IsUnique();
            b.Entity<Student>().HasIndex(x => x.UserId).IsUnique();   // EF tự thêm filter IS NOT NULL
            b.Entity<Staff>().HasIndex(x => x.UserId).IsUnique();
            b.Entity<Grade>().HasIndex(x => new { x.StudentId, x.CourseId, x.SemesterId }).IsUnique();
            b.Entity<AuditLog>().HasIndex(x => x.CreatedAt);

            // Quan hệ 1-0..1 và quan hệ tự tham chiếu
            b.Entity<Student>().HasOne(x => x.User).WithOne(u => u.Student).HasForeignKey<Student>(x => x.UserId);
            b.Entity<Staff>().HasOne(x => x.User).WithOne(u => u.Staff).HasForeignKey<Staff>(x => x.UserId);
            b.Entity<CoursePrerequisite>().HasOne(x => x.Course).WithMany(c => c.Prerequisites).HasForeignKey(x => x.CourseId);
            b.Entity<CoursePrerequisite>().HasOne(x => x.Prerequisite).WithMany(c => c.RequiredFor).HasForeignKey(x => x.PrerequisiteId);
            b.Entity<Class>().HasOne(x => x.Advisor).WithMany(s => s.AdvisedClasses).HasForeignKey(x => x.AdvisorId);
            b.Entity<Grade>().HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedBy);

            // Kiểu decimal: điểm DECIMAL(3,1), thang 4 DECIMAL(2,1)
            foreach (var p in new[] { "AttendanceScore", "MidtermScore", "FinalScore", "TotalScore" })
                b.Entity<Grade>().Property(p).HasPrecision(3, 1);
            b.Entity<Grade>().Property(x => x.GradePoint).HasPrecision(2, 1);
            b.Entity<GradeScale>().Property(x => x.MinScore).HasPrecision(3, 1);
            b.Entity<GradeScale>().Property(x => x.MaxScore).HasPrecision(3, 1);
            b.Entity<GradeScale>().Property(x => x.GradePoint).HasPrecision(2, 1);

            // Check constraint điểm 0-10
            b.Entity<Grade>().ToTable(t => t.HasCheckConstraint("CK_Grades_TotalScore",
                "[TotalScore] IS NULL OR ([TotalScore] >= 0 AND [TotalScore] <= 10)"));

            // Không xóa dây chuyền: dữ liệu có liên kết thì xóa mềm
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
