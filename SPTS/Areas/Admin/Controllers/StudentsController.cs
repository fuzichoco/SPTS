using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    // Area "Student" trùng tên với lớp Models.Student nên khai báo alias trong namespace
    using Student = SPTS.Models.Student;

    public class StudentsController : CatalogControllerBase
    {
        public static readonly string[] Statuses = { "Đang học", "Bảo lưu", "Thôi học", "Tốt nghiệp" };
        private const string DefaultPassword = "Student@123";
        private readonly IPasswordHasher<User> _hasher;

        public StudentsController(ApplicationDbContext db, AuditLogService audit, IPasswordHasher<User> hasher) : base(db, audit) => _hasher = hasher;

        public async Task<IActionResult> Index(string? q, int? classId, string? status, int page = 1)
        {
            var query = Db.Students.AsNoTracking().Include(s => s.Class).Include(s => s.User).Where(s => !s.IsDeleted);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(s => s.StudentCode.Contains(q) || s.FullName.Contains(q) || (s.Email != null && s.Email.Contains(q)));
            if (classId != null) query = query.Where(s => s.ClassId == classId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
            ViewBag.Q = q;
            ViewBag.Status = status;
            ViewBag.Classes = await ClassListAsync(classId);
            return View(await query.OrderBy(s => s.StudentCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var s = id == null ? new Student() : await Db.Students.FirstOrDefaultAsync(x => x.StudentId == id && !x.IsDeleted);
            if (s == null) return NotFound();
            ViewBag.Classes = await ClassListAsync(s.ClassId);
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, bool createAccount,
            [Bind("StudentId,StudentCode,FullName,DateOfBirth,Gender,Email,Phone,ClassId,Status")] Student model)
        {
            if (ModelState.IsValid)
            {
                model.StudentCode = model.StudentCode.Trim();
                if (id == null)
                {
                    Db.Students.Add(model);
                    // Tạo luôn tài khoản đăng nhập sv{mã} với mật khẩu mặc định (module TV1)
                    if (createAccount) model.User = await NewAccountAsync(model);
                }
                else
                {
                    var s = await Db.Students.FirstOrDefaultAsync(x => x.StudentId == id && !x.IsDeleted);
                    if (s == null) return NotFound();
                    var userId = s.UserId;   // form không gửi UserId, giữ tài khoản đã liên kết
                    Db.Entry(s).CurrentValues.SetValues(model);
                    s.UserId = userId;
                    model = s;
                }
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Students", () => model.StudentId,
                        new { model.StudentCode, model.FullName, model.ClassId, model.Status }))
                {
                    if (createAccount && id == null) TempData["Message"] = $"Đã thêm sinh viên và tài khoản sv{model.StudentCode} / {DefaultPassword}";
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.Classes = await ClassListAsync(model.ClassId);
            return View(model);
        }

        // Xóa mềm: sinh viên có điểm nên chỉ đánh dấu IsDeleted, đồng thời khóa tài khoản
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await Db.Students.Include(x => x.User).FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return NotFound();
            s.IsDeleted = true;
            if (s.User != null) s.User.IsActive = false;
            await Db.SaveChangesAsync();
            await Audit.LogAsync("SOFT_DELETE", "Students", id, new { s.StudentCode, s.FullName });
            TempData["Message"] = $"Đã xóa sinh viên {s.StudentCode}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<User> NewAccountAsync(Student s)
        {
            var role = await Db.Roles.FirstAsync(r => r.RoleName == Role.Student);
            var u = new User
            {
                Username = "sv" + s.StudentCode,
                FullName = s.FullName,
                Email = string.IsNullOrWhiteSpace(s.Email) ? $"sv{s.StudentCode}@spts.edu.vn" : s.Email
            };
            u.PasswordHash = _hasher.HashPassword(u, DefaultPassword);
            u.UserRoles.Add(new UserRole { RoleId = role.RoleId });
            return u;
        }

        private async Task<SelectList> ClassListAsync(int? selected) =>
            new(await Db.Classes.AsNoTracking().OrderBy(c => c.ClassCode).ToListAsync(), "ClassId", "ClassCode", selected);
    }
}
