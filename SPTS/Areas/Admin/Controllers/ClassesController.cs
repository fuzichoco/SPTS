using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    public class ClassesController : CatalogControllerBase
    {
        public ClassesController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = Db.Classes.AsNoTracking().Include(c => c.Program).Include(c => c.Advisor)
                .Include(c => c.Students.Where(s => !s.IsDeleted)).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.ClassCode.Contains(q) || c.Program.ProgramName.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderBy(c => c.ClassCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var c = id == null ? new Class() : await Db.Classes.FindAsync(id);
            if (c == null) return NotFound();
            await LoadListsAsync(c);
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("ClassId,ClassCode,ProgramId,AdvisorId")] Class model)
        {
            if (ModelState.IsValid)
            {
                model.ClassCode = model.ClassCode.Trim().ToUpper();
                if (id == null) Db.Classes.Add(model); else Db.Classes.Update(model);
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Classes", () => model.ClassId,
                        new { model.ClassCode, model.ProgramId, model.AdvisorId }))
                    return RedirectToAction(nameof(Index));
            }
            await LoadListsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await Db.Classes.FindAsync(id);
            return await DeleteAsync(c, "Classes", id, new { c?.ClassCode });
        }

        private async Task LoadListsAsync(Class c)
        {
            ViewBag.Programs = new SelectList(await Db.Programs.AsNoTracking().OrderBy(p => p.ProgramCode).ToListAsync(), "ProgramId", "ProgramName", c.ProgramId);
            ViewBag.Advisors = new SelectList(await Db.Staffs.AsNoTracking().OrderBy(s => s.FullName).ToListAsync(), "StaffId", "FullName", c.AdvisorId);
        }
    }
}
