using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;

namespace SPTS.Areas.Student.Controllers
{
    // TV3 - tra cứu học phần bằng AJAX (fetch), xem tiên quyết; Session lưu học phần xem gần đây
    [Area("Student"), Authorize]
    public class LookupController : Controller
    {
        private const string RecentKey = "RecentCourses";
        private readonly ApplicationDbContext _db;

        public LookupController(ApplicationDbContext db) => _db = db;

        public IActionResult Index()
        {
            ViewBag.Recent = ReadRecent();
            return View();
        }

        // GET: Student/Lookup/Search?q=&page= - JSON
        public async Task<IActionResult> Search(string? q, int page = 1)
        {
            const int size = 8;
            var query = _db.Courses.AsNoTracking().Where(c => !c.IsDeleted);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.CourseCode.Contains(q) || c.CourseName.Contains(q));
            var total = await query.CountAsync();
            var passed = await PassedCourseIdsAsync();
            var items = await query.OrderBy(c => c.CourseCode).Skip((Math.Max(1, page) - 1) * size).Take(size)
                .Select(c => new CourseLookupItemViewModel
                {
                    CourseId = c.CourseId, CourseCode = c.CourseCode, CourseName = c.CourseName, Credits = c.Credits, Description = c.Description,
                    Prerequisites = c.Prerequisites.Select(p => p.Prerequisite.CourseCode).ToList()
                }).ToListAsync();
            return Json(new
            {
                total, page, pages = (int)Math.Ceiling(total / (double)size),
                items = items.Select(i => new { i.CourseId, i.CourseCode, i.CourseName, i.Credits, i.Description, i.Prerequisites, passed = passed.Contains(i.CourseId) })
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.Courses.AsNoTracking()
                .Include(x => x.Prerequisites).ThenInclude(p => p.Prerequisite)
                .Include(x => x.RequiredFor).ThenInclude(p => p.Course)
                .Include(x => x.ProgramCourses).ThenInclude(pc => pc.Program)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.CourseId == id && !x.IsDeleted);
            if (c == null) return NotFound();
            ViewBag.Passed = await PassedCourseIdsAsync();

            // Session: lưu tối đa 5 học phần xem gần nhất trong phiên làm việc
            var recent = ReadRecent().Where(r => r.Id != id).Prepend(new RecentCourse(id, c.CourseCode, c.CourseName)).Take(5).ToList();
            HttpContext.Session.SetString(RecentKey, JsonSerializer.Serialize(recent));
            return View(c);
        }

        private List<RecentCourse> ReadRecent()
        {
            var raw = HttpContext.Session.GetString(RecentKey);
            return raw == null ? new() : JsonSerializer.Deserialize<List<RecentCourse>>(raw) ?? new();
        }

        private async Task<List<int>> PassedCourseIdsAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _db.Grades.AsNoTracking().Where(g => g.Student.UserId == userId && g.IsPassed)
                .Select(g => g.CourseId).Distinct().ToListAsync();
        }

        public record RecentCourse(int Id, string Code, string Name);
    }
}
