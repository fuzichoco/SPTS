using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    // Chương trình đào tạo + danh sách học phần, nhóm tự chọn của CTĐT
    public class ProgramsController : CatalogControllerBase
    {
        public ProgramsController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = Db.Programs.AsNoTracking().Include(p => p.Major).ThenInclude(m => m.Faculty)
                .Include(p => p.ProgramCourses).Include(p => p.Classes).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.ProgramCode.Contains(q) || p.ProgramName.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderByDescending(p => p.CohortYear).ThenBy(p => p.ProgramCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Details(int id)
        {
            var p = await Db.Programs.AsNoTracking()
                .Include(x => x.Major)
                .Include(x => x.ElectiveGroups)
                .Include(x => x.ProgramCourses).ThenInclude(pc => pc.Course)
                .Include(x => x.ProgramCourses).ThenInclude(pc => pc.ElectiveGroup)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.ProgramId == id);
            if (p == null) return NotFound();
            var used = p.ProgramCourses.Select(pc => pc.CourseId).ToList();
            ViewBag.Courses = new SelectList(await Db.Courses.AsNoTracking().Where(c => !c.IsDeleted && !used.Contains(c.CourseId))
                .OrderBy(c => c.CourseCode).Select(c => new { c.CourseId, Name = c.CourseCode + " - " + c.CourseName }).ToListAsync(), "CourseId", "Name");
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCourse([Bind("ProgramId,CourseId,KnowledgeBlock,IsRequired,ElectiveGroupId,SuggestedSemester")] ProgramCourse pc)
        {
            if (!pc.IsRequired && pc.ElectiveGroupId == null)
                TempData["Error"] = "Học phần tự chọn phải thuộc một nhóm tự chọn.";
            else if (!ModelState.IsValid)
                TempData["Error"] = "Dữ liệu không hợp lệ.";
            else
            {
                if (pc.IsRequired) pc.ElectiveGroupId = null;
                Db.ProgramCourses.Add(pc);
                if (!await TrySaveAsync("CREATE", "ProgramCourses", () => $"{pc.ProgramId}-{pc.CourseId}",
                        new { pc.ProgramId, pc.CourseId, pc.KnowledgeBlock, pc.SuggestedSemester }))
                    TempData["Error"] = "Không thêm được học phần.";
            }
            return RedirectToAction(nameof(Details), new { id = pc.ProgramId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCourse(int programId, int courseId)
        {
            var pc = await Db.ProgramCourses.FindAsync(programId, courseId);
            if (pc != null)
            {
                Db.ProgramCourses.Remove(pc);
                await Db.SaveChangesAsync();
                await Audit.LogAsync("DELETE", "ProgramCourses", $"{programId}-{courseId}");
                TempData["Message"] = "Đã gỡ học phần khỏi CTĐT.";
            }
            return RedirectToAction(nameof(Details), new { id = programId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddElectiveGroup([Bind("ProgramId,GroupName,MinCredits")] ElectiveGroup g)
        {
            if (ModelState.IsValid)
            {
                Db.ElectiveGroups.Add(g);
                await TrySaveAsync("CREATE", "ElectiveGroups", () => g.ElectiveGroupId, new { g.GroupName, g.MinCredits });
            }
            return RedirectToAction(nameof(Details), new { id = g.ProgramId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveElectiveGroup(int id)
        {
            var g = await Db.ElectiveGroups.FindAsync(id);
            if (g == null) return NotFound();
            Db.ElectiveGroups.Remove(g);
            try { await Db.SaveChangesAsync(); TempData["Message"] = "Đã xóa nhóm tự chọn."; }
            catch (DbUpdateException) { TempData["Error"] = "Nhóm đang có học phần, gỡ học phần trước."; }
            return RedirectToAction(nameof(Details), new { id = g.ProgramId });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var p = id == null ? new TrainingProgram { CohortYear = DateTime.Today.Year, TotalCredits = 130 } : await Db.Programs.FindAsync(id);
            if (p == null) return NotFound();
            await LoadListsAsync(p.MajorId);
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("ProgramId,ProgramCode,ProgramName,MajorId,CohortYear,TotalCredits")] TrainingProgram model)
        {
            if (ModelState.IsValid)
            {
                model.ProgramCode = model.ProgramCode.Trim().ToUpper();
                if (id == null) Db.Programs.Add(model); else Db.Programs.Update(model);
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Programs", () => model.ProgramId,
                        new { model.ProgramCode, model.ProgramName, model.MajorId, model.CohortYear, model.TotalCredits }))
                    return RedirectToAction(nameof(Index));
            }
            await LoadListsAsync(model.MajorId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await Db.Programs.FindAsync(id);
            return await DeleteAsync(p, "Programs", id, new { p?.ProgramCode, p?.ProgramName });
        }

        // Dropdown Khoa -> Ngành: chọn khoa thì tải ngành bằng fetch (Majors/ByFaculty)
        private async Task LoadListsAsync(int majorId)
        {
            var facultyId = await Db.Majors.Where(m => m.MajorId == majorId).Select(m => (int?)m.FacultyId).FirstOrDefaultAsync();
            ViewBag.Faculties = new SelectList(await Db.Faculties.AsNoTracking().OrderBy(f => f.FacultyName).ToListAsync(), "FacultyId", "FacultyName", facultyId);
            ViewBag.Majors = new SelectList(await Db.Majors.AsNoTracking().Where(m => facultyId == null || m.FacultyId == facultyId)
                .OrderBy(m => m.MajorName).ToListAsync(), "MajorId", "MajorName", majorId);
        }
    }
}
