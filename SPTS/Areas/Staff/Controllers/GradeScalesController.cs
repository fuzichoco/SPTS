using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Staff.Controllers
{
    // Thang điểm quy đổi (10 -> chữ -> 4). Sửa xong phải xóa cache thang điểm
    [Area("Staff"), Authorize(Roles = Role.Admin + "," + Role.Staff)]
    public class GradeScalesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly GradeService _grades;
        private readonly AuditLogService _audit;

        public GradeScalesController(ApplicationDbContext db, GradeService grades, AuditLogService audit)
        {
            _db = db;
            _grades = grades;
            _audit = audit;
        }

        public async Task<IActionResult> Index() =>
            View(await _db.GradeScales.AsNoTracking().OrderByDescending(s => s.MinScore).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Update([Bind("GradeScaleId,MinScore,MaxScore,LetterGrade,GradePoint")] GradeScale model)
        {
            if (model.MaxScore < model.MinScore) ModelState.AddModelError("", "Điểm tối đa phải ≥ điểm tối thiểu.");
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu thang điểm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }
            var old = await _db.GradeScales.AsNoTracking().FirstOrDefaultAsync(s => s.GradeScaleId == model.GradeScaleId);
            if (old == null) return NotFound();
            _db.GradeScales.Update(model);
            await _db.SaveChangesAsync();
            _grades.InvalidateScales();
            await _audit.LogAsync("UPDATE", "GradeScales", model.GradeScaleId, old, model);
            TempData["Message"] = $"Đã cập nhật mức {model.LetterGrade}. Bấm \"Tính lại điểm\" để áp dụng cho điểm đã nhập.";
            return RedirectToAction(nameof(Index));
        }

        // Tính lại điểm chữ / thang 4 cho các học kỳ chưa khóa theo thang điểm mới
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Recalculate()
        {
            var grades = await _db.Grades.Where(g => !g.Semester.IsLocked).ToListAsync();
            foreach (var g in grades)
            {
                await _grades.ApplyAsync(g);
                _grades.InvalidateProgress(g.StudentId);
            }
            await _db.SaveChangesAsync();
            await _audit.LogAsync("RECALCULATE_GRADES", "Grades", null, null, new { Count = grades.Count });
            TempData["Message"] = $"Đã tính lại {grades.Count} bản ghi điểm.";
            return RedirectToAction(nameof(Index));
        }
    }
}
