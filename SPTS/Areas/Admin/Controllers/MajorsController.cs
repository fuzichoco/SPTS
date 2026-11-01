using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    public class MajorsController : CatalogControllerBase
    {
        public MajorsController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int? facultyId, int page = 1)
        {
            var query = Db.Majors.AsNoTracking().Include(m => m.Faculty).Include(m => m.Programs).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(m => m.MajorCode.Contains(q) || m.MajorName.Contains(q));
            if (facultyId != null) query = query.Where(m => m.FacultyId == facultyId);
            ViewBag.Q = q;
            ViewBag.Faculties = await FacultyListAsync(facultyId);
            return View(await query.OrderBy(m => m.MajorCode).ToPagedAsync(page, PageSize));
        }

        // GET: Admin/Majors/ByFaculty?facultyId=1 - JSON cho dropdown phụ thuộc (fetch)
        public async Task<IActionResult> ByFaculty(int facultyId) =>
            Json(await Db.Majors.AsNoTracking().Where(m => m.FacultyId == facultyId)
                .OrderBy(m => m.MajorName).Select(m => new { m.MajorId, m.MajorName }).ToListAsync());

        public async Task<IActionResult> Edit(int? id)
        {
            var m = id == null ? new Major() : await Db.Majors.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Faculties = await FacultyListAsync(m.FacultyId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("MajorId,MajorCode,MajorName,FacultyId")] Major model)
        {
            if (ModelState.IsValid)
            {
                if (id == null) Db.Majors.Add(model); else Db.Majors.Update(model);
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Majors", () => model.MajorId,
                        new { model.MajorCode, model.MajorName, model.FacultyId }))
                    return RedirectToAction(nameof(Index));
            }
            ViewBag.Faculties = await FacultyListAsync(model.FacultyId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await Db.Majors.FindAsync(id);
            return await DeleteAsync(m, "Majors", id, new { m?.MajorCode, m?.MajorName });
        }

        private async Task<SelectList> FacultyListAsync(int? selected) =>
            new(await Db.Faculties.AsNoTracking().OrderBy(f => f.FacultyName).ToListAsync(), "FacultyId", "FacultyName", selected);
    }
}
