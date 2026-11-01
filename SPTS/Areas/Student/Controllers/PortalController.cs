using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Student.Controllers
{
    // Cổng sinh viên: trang dịch vụ, sơ yếu lý lịch, CTĐT, xem điểm, cảnh báo học vụ
    [Area("Student"), Authorize(Roles = Role.Student)]
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly GradeService _grades;

        public PortalController(ApplicationDbContext db, GradeService grades)
        {
            _db = db;
            _grades = grades;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: Student/Portal - "Dịch vụ của bạn"
        public async Task<IActionResult> Index()
        {
            ViewBag.Unread = await _db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);
            return View(await CurrentProgressAsync());
        }

        public async Task<IActionResult> Profile()
        {
            var s = await _db.Students.AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class).ThenInclude(c => c.Advisor)
                .Include(x => x.Class).ThenInclude(c => c.Program).ThenInclude(p => p.Major).ThenInclude(m => m.Faculty)
                .FirstOrDefaultAsync(x => x.UserId == UserId && !x.IsDeleted);
            return s == null ? View("~/Areas/Student/Views/Progress/NoProfile.cshtml") : View(s);
        }

        public async Task<IActionResult> Program() => await ProgressView();
        public async Task<IActionResult> Grades() => await ProgressView();
        public async Task<IActionResult> Warnings() => await ProgressView();

        private async Task<IActionResult> ProgressView()
        {
            var p = await CurrentProgressAsync();
            return p == null ? View("~/Areas/Student/Views/Progress/NoProfile.cshtml") : View(p);
        }

        private async Task<ProgressViewModel?> CurrentProgressAsync()
        {
            var id = await _db.Students.Where(s => s.UserId == UserId && !s.IsDeleted).Select(s => (int?)s.StudentId).FirstOrDefaultAsync();
            return id == null ? null : await _grades.GetProgressAsync(id.Value);
        }
    }
}
