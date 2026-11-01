using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Student.Controllers
{
    // TV3 - sinh viên xem tiến độ, GPA, kết quả theo học kỳ, CTĐT
    [Area("Student"), Authorize(Roles = Role.Student)]
    public class ProgressController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly GradeService _grades;

        public ProgressController(ApplicationDbContext db, GradeService grades)
        {
            _db = db;
            _grades = grades;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var studentId = await _db.Students.Where(s => s.UserId == userId && !s.IsDeleted).Select(s => (int?)s.StudentId).FirstOrDefaultAsync();
            if (studentId == null) return View("NoProfile");
            return View(await _grades.GetProgressAsync(studentId.Value));
        }
    }
}
