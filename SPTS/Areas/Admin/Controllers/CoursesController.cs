using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    // Học phần + học phần tiên quyết (bảng của TV3, quản trị tại Admin)
    public class CoursesController : CatalogControllerBase
    {
        public CoursesController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = Db.Courses.AsNoTracking().Include(c => c.Prerequisites).ThenInclude(p => p.Prerequisite).Where(c => !c.IsDeleted);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.CourseCode.Contains(q) || c.CourseName.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderBy(c => c.CourseCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var c = id == null ? new Course { Credits = 3 } : await Db.Courses.Include(x => x.Prerequisites).FirstOrDefaultAsync(x => x.CourseId == id && !x.IsDeleted);
            if (c == null) return NotFound();
            await LoadListsAsync(c.CourseId, c.Prerequisites.Select(p => p.PrerequisiteId));
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] prerequisiteIds, [Bind("CourseId,CourseCode,CourseName,Credits,Description")] Course model)
        {
            prerequisiteIds = prerequisiteIds.Where(p => p != model.CourseId).Distinct().ToArray();
            if (ModelState.IsValid)
            {
                model.CourseCode = model.CourseCode.Trim().ToUpper();
                var course = model;
                if (id == null) Db.Courses.Add(model);
                else
                {
                    course = await Db.Courses.Include(x => x.Prerequisites).FirstOrDefaultAsync(x => x.CourseId == id);
                    if (course == null) return NotFound();
                    Db.Entry(course).CurrentValues.SetValues(model);
                    course.Prerequisites.Clear();
                }
                foreach (var p in prerequisiteIds)
                    course.Prerequisites.Add(new CoursePrerequisite { PrerequisiteId = p });
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Courses", () => course.CourseId,
                        new { course.CourseCode, course.CourseName, course.Credits, Prerequisites = prerequisiteIds }))
                    return RedirectToAction(nameof(Index));
            }
            await LoadListsAsync(model.CourseId, prerequisiteIds);
            return View(model);
        }

        // Xóa mềm vì học phần có thể đã có điểm
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await Db.Courses.FindAsync(id);
            if (c == null) return NotFound();
            c.IsDeleted = true;
            await Db.SaveChangesAsync();
            await Audit.LogAsync("SOFT_DELETE", "Courses", id, new { c.CourseCode, c.CourseName });
            TempData["Message"] = $"Đã xóa học phần {c.CourseCode}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadListsAsync(int courseId, IEnumerable<int> selected) =>
            ViewBag.AllCourses = new MultiSelectList(await Db.Courses.AsNoTracking().Where(c => !c.IsDeleted && c.CourseId != courseId)
                .OrderBy(c => c.CourseCode).Select(c => new { c.CourseId, Name = c.CourseCode + " - " + c.CourseName }).ToListAsync(),
                "CourseId", "Name", selected);
    }
}
