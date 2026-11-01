using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Staff.Controllers
{
    [Area("Staff"), Authorize(Roles = Role.Admin + "," + Role.Staff)]
    public class SemestersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AuditLogService _audit;

        public SemestersController(ApplicationDbContext db, AuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IActionResult> Index() =>
            View(await _db.Semesters.AsNoTracking().Include(s => s.Grades).OrderByDescending(s => s.StartDate).ToListAsync());

        public async Task<IActionResult> Edit(int? id)
        {
            var s = id == null ? new Semester { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(4) } : await _db.Semesters.FindAsync(id);
            return s == null ? NotFound() : View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("SemesterId,SemesterName,StartDate,EndDate,IsLocked")] Semester model)
        {
            if (model.EndDate <= model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            if (!ModelState.IsValid) return View(model);
            if (id == null) _db.Semesters.Add(model); else _db.Semesters.Update(model);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(id == null ? "CREATE" : "UPDATE", "Semesters", model.SemesterId, null,
                new { model.SemesterName, model.StartDate, model.EndDate, model.IsLocked });
            TempData["Message"] = "Đã lưu học kỳ.";
            return RedirectToAction(nameof(Index));
        }

        // Khóa học kỳ: sau khi khóa, không ai sửa được điểm của học kỳ đó
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var s = await _db.Semesters.FindAsync(id);
            if (s == null) return NotFound();
            s.IsLocked = !s.IsLocked;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(s.IsLocked ? "LOCK_SEMESTER" : "UNLOCK_SEMESTER", "Semesters", id);
            TempData["Message"] = $"{(s.IsLocked ? "Đã khóa" : "Đã mở khóa")} {s.SemesterName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Semesters.FindAsync(id);
            if (s == null) return NotFound();
            _db.Semesters.Remove(s);
            try
            {
                await _db.SaveChangesAsync();
                await _audit.LogAsync("DELETE", "Semesters", id, new { s.SemesterName });
                TempData["Message"] = "Đã xóa học kỳ.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Học kỳ đã có điểm, không thể xóa.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
