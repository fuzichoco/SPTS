using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Staff.Controllers
{
    // TV2 - UC quản lý điểm: nhập/sửa điểm, tự tính tổng, quy đổi chữ và thang 4
    [Area("Staff"), Authorize(Roles = Role.Admin + "," + Role.Staff)]
    public class GradesController : Controller
    {
        private const string SemesterCookie = "SPTS.Staff.Semester";
        private const int PageSize = 15;
        private readonly ApplicationDbContext _db;
        private readonly GradeService _grades;
        private readonly AuditLogService _audit;

        public GradesController(ApplicationDbContext db, GradeService grades, AuditLogService audit)
        {
            _db = db;
            _grades = grades;
            _audit = audit;
        }

        // GET: Staff/Grades?SemesterId=&ClassId=&CourseId=&Keyword=&Page=
        public async Task<IActionResult> Index(GradeFilterViewModel f)
        {
            // Persistent Cookie: nhớ học kỳ giáo vụ chọn lần trước (30 ngày)
            if (!Request.Query.ContainsKey(nameof(f.SemesterId)) && int.TryParse(Request.Cookies[SemesterCookie], out var saved))
                f.SemesterId = saved;
            else if (f.SemesterId != null)
                Response.Cookies.Append(SemesterCookie, f.SemesterId.Value.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(30), HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true
                });

            var q = _db.Grades.AsNoTracking().Where(g => !g.Student.IsDeleted);
            if (f.SemesterId != null) q = q.Where(g => g.SemesterId == f.SemesterId);
            if (f.ClassId != null) q = q.Where(g => g.Student.ClassId == f.ClassId);
            if (f.CourseId != null) q = q.Where(g => g.CourseId == f.CourseId);
            if (!string.IsNullOrWhiteSpace(f.Keyword))
            {
                var k = f.Keyword.Trim();
                q = q.Where(g => g.Student.StudentCode.Contains(k) || g.Student.FullName.Contains(k));
            }
            f.Result = await q.OrderBy(g => g.Student.Class.ClassCode).ThenBy(g => g.Student.StudentCode).ThenBy(g => g.Course.CourseCode)
                .Select(g => new GradeListItemViewModel
                {
                    GradeId = g.GradeId, StudentCode = g.Student.StudentCode, StudentName = g.Student.FullName, ClassCode = g.Student.Class.ClassCode,
                    CourseCode = g.Course.CourseCode, CourseName = g.Course.CourseName, SemesterName = g.Semester.SemesterName, SemesterLocked = g.Semester.IsLocked,
                    AttemptNo = g.AttemptNo, AttendanceScore = g.AttendanceScore, MidtermScore = g.MidtermScore, FinalScore = g.FinalScore,
                    TotalScore = g.TotalScore, LetterGrade = g.LetterGrade, IsPassed = g.IsPassed
                })
                .ToPagedAsync(f.Page, PageSize);
            await LoadFilterListsAsync(f);
            return View(f);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var m = new GradeEditModel { SemesterId = int.TryParse(Request.Cookies[SemesterCookie], out var s) ? s : null };
            if (id != null)
            {
                var g = await _db.Grades.AsNoTracking().FirstOrDefaultAsync(x => x.GradeId == id);
                if (g == null) return NotFound();
                m = new GradeEditModel
                {
                    GradeId = g.GradeId, StudentId = g.StudentId, CourseId = g.CourseId, SemesterId = g.SemesterId, AttemptNo = g.AttemptNo,
                    AttendanceScore = g.AttendanceScore, MidtermScore = g.MidtermScore, FinalScore = g.FinalScore
                };
            }
            await LoadEditListsAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GradeEditModel m)
        {
            if (ModelState.IsValid)
            {
                var semester = await _db.Semesters.FindAsync(m.SemesterId);
                if (semester == null || semester.IsLocked)
                    ModelState.AddModelError(string.Empty, "Học kỳ đã khóa, không thể nhập/sửa điểm.");
                else if (await _db.Grades.AnyAsync(g => g.GradeId != (m.GradeId ?? 0) && g.StudentId == m.StudentId && g.CourseId == m.CourseId && g.SemesterId == m.SemesterId))
                    ModelState.AddModelError(string.Empty, "Sinh viên đã có điểm học phần này trong học kỳ đã chọn.");
            }
            if (!ModelState.IsValid)
            {
                await LoadEditListsAsync();
                return View(m);
            }

            Grade? g;
            object? old = null;
            if (m.GradeId == null)
            {
                g = new Grade();
                _db.Grades.Add(g);
            }
            else
            {
                g = await _db.Grades.Include(x => x.Semester).FirstOrDefaultAsync(x => x.GradeId == m.GradeId);
                if (g == null) return NotFound();
                if (g.Semester.IsLocked)
                {
                    TempData["Error"] = "Học kỳ của bản ghi này đã khóa.";
                    return RedirectToAction(nameof(Index));
                }
                old = Snapshot(g);
            }
            g.StudentId = m.StudentId!.Value; g.CourseId = m.CourseId!.Value; g.SemesterId = m.SemesterId!.Value; g.AttemptNo = m.AttemptNo;
            g.AttendanceScore = m.AttendanceScore; g.MidtermScore = m.MidtermScore; g.FinalScore = m.FinalScore;
            await SaveGradeAsync(g, old);
            TempData["Message"] = $"Đã lưu điểm. Tổng kết: {g.TotalScore?.ToString("0.0") ?? "chưa đủ điểm"} {g.LetterGrade}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Staff/Grades/SaveAjax - lưu nhanh 3 cột điểm trên bảng (fetch + FormData), trả JSON
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(GradeQuickSaveModel m)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Điểm phải trong khoảng 0 - 10." });
            var g = await _db.Grades.Include(x => x.Semester).FirstOrDefaultAsync(x => x.GradeId == m.GradeId);
            if (g == null) return NotFound(new { message = "Không tìm thấy bản ghi điểm." });
            if (g.Semester.IsLocked) return BadRequest(new { message = "Học kỳ đã khóa." });
            var old = Snapshot(g);
            g.AttendanceScore = m.AttendanceScore; g.MidtermScore = m.MidtermScore; g.FinalScore = m.FinalScore;
            await SaveGradeAsync(g, old);
            return Json(new { total = g.TotalScore, letter = g.LetterGrade, gradePoint = g.GradePoint, isPassed = g.IsPassed });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.Grades.Include(x => x.Semester).FirstOrDefaultAsync(x => x.GradeId == id);
            if (g == null) return NotFound();
            if (g.Semester.IsLocked) TempData["Error"] = "Học kỳ đã khóa, không thể xóa điểm.";
            else
            {
                _db.Grades.Remove(g);
                await _db.SaveChangesAsync();
                await _audit.LogAsync("DELETE_GRADE", "Grades", id, Snapshot(g));
                _grades.InvalidateProgress(g.StudentId);
                TempData["Message"] = "Đã xóa bản ghi điểm.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Tính điểm, lưu, ghi nhật ký, xóa cache tiến độ, gửi thông báo cho sinh viên
        private async Task SaveGradeAsync(Grade g, object? old)
        {
            await _grades.ApplyAsync(g);
            g.UpdatedBy = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
            g.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var info = await _db.Grades.AsNoTracking().Where(x => x.GradeId == g.GradeId)
                .Select(x => new { x.Student.UserId, x.Course.CourseCode, x.Course.CourseName, x.Semester.SemesterName }).FirstAsync();
            if (info.UserId != null && g.TotalScore != null)
                _db.Notifications.Add(new Notification
                {
                    UserId = info.UserId.Value,
                    Title = $"Cập nhật điểm {info.CourseCode}",
                    Content = $"Học phần {info.CourseName} ({info.SemesterName}): tổng kết {g.TotalScore:0.0} - {g.LetterGrade} ({(g.IsPassed ? "Đạt" : "Chưa đạt")})."
                });
            await _audit.LogAsync(old == null ? "CREATE_GRADE" : "UPDATE_GRADE", "Grades", g.GradeId, old, Snapshot(g));
            _grades.InvalidateProgress(g.StudentId);
        }

        private static object Snapshot(Grade g) => new
        {
            g.StudentId, g.CourseId, g.SemesterId, g.AttemptNo, g.AttendanceScore, g.MidtermScore, g.FinalScore, g.TotalScore, g.LetterGrade
        };

        private async Task LoadFilterListsAsync(GradeFilterViewModel f)
        {
            ViewBag.Semesters = new SelectList(await _db.Semesters.AsNoTracking().OrderByDescending(s => s.StartDate).ToListAsync(), "SemesterId", "SemesterName", f.SemesterId);
            ViewBag.Classes = new SelectList(await _db.Classes.AsNoTracking().OrderBy(c => c.ClassCode).ToListAsync(), "ClassId", "ClassCode", f.ClassId);
            ViewBag.Courses = new SelectList(await _db.Courses.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.CourseCode)
                .Select(c => new { c.CourseId, Name = c.CourseCode + " - " + c.CourseName }).ToListAsync(), "CourseId", "Name", f.CourseId);
        }

        private async Task LoadEditListsAsync()
        {
            ViewBag.Students = new SelectList(await _db.Students.AsNoTracking().Where(s => !s.IsDeleted).OrderBy(s => s.StudentCode)
                .Select(s => new { s.StudentId, Name = s.StudentCode + " - " + s.FullName + " (" + s.Class.ClassCode + ")" }).ToListAsync(), "StudentId", "Name");
            ViewBag.Courses = new SelectList(await _db.Courses.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.CourseCode)
                .Select(c => new { c.CourseId, Name = c.CourseCode + " - " + c.CourseName + " (" + c.Credits + " TC)" }).ToListAsync(), "CourseId", "Name");
            ViewBag.Semesters = new SelectList(await _db.Semesters.AsNoTracking().Where(s => !s.IsLocked).OrderByDescending(s => s.StartDate).ToListAsync(), "SemesterId", "SemesterName");
        }
    }
}
