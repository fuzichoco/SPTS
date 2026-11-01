using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    public class FacultiesController : CatalogControllerBase
    {
        public FacultiesController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = Db.Faculties.AsNoTracking().Include(f => f.Majors).Include(f => f.Staffs).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(f => f.FacultyCode.Contains(q) || f.FacultyName.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderBy(f => f.FacultyCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return View(new Faculty());
            var f = await Db.Faculties.FindAsync(id);
            return f == null ? NotFound() : View(f);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("FacultyId,FacultyCode,FacultyName")] Faculty model)
        {
            if (!ModelState.IsValid) return View(model);
            model.FacultyCode = model.FacultyCode.Trim().ToUpper();
            if (id == null) Db.Faculties.Add(model); else Db.Faculties.Update(model);
            return await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Faculties", () => model.FacultyId,
                new { model.FacultyCode, model.FacultyName })
                ? RedirectToAction(nameof(Index)) : View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await Db.Faculties.FindAsync(id);
            return await DeleteAsync(f, "Faculties", id, new { f?.FacultyCode, f?.FacultyName });
        }
    }
}
