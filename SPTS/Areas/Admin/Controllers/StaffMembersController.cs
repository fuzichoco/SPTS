using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    // Area "Staff" trùng tên với lớp Models.Staff nên khai báo alias trong namespace
    using Staff = SPTS.Models.Staff;

    // Cán bộ (bảng Staff). Đặt tên StaffMembers để không trùng Area "Staff"
    public class StaffMembersController : CatalogControllerBase
    {
        public StaffMembersController(ApplicationDbContext db, AuditLogService audit) : base(db, audit) { }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            var query = Db.Staffs.AsNoTracking().Include(s => s.Faculty).Include(s => s.User).Include(s => s.AdvisedClasses).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(s => s.StaffCode.Contains(q) || s.FullName.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderBy(s => s.StaffCode).ToPagedAsync(page, PageSize));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var s = id == null ? new Staff() : await Db.Staffs.FindAsync(id);
            if (s == null) return NotFound();
            await LoadListsAsync(s);
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("StaffId,StaffCode,FullName,Position,FacultyId,UserId")] Staff model)
        {
            if (ModelState.IsValid)
            {
                model.StaffCode = model.StaffCode.Trim().ToUpper();
                if (id == null) Db.Staffs.Add(model); else Db.Staffs.Update(model);
                if (await TrySaveAsync(id == null ? "CREATE" : "UPDATE", "Staff", () => model.StaffId,
                        new { model.StaffCode, model.FullName, model.Position, model.FacultyId, model.UserId }))
                    return RedirectToAction(nameof(Index));
            }
            await LoadListsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await Db.Staffs.FindAsync(id);
            return await DeleteAsync(s, "Staff", id, new { s?.StaffCode, s?.FullName });
        }

        // Tài khoản liên kết: chỉ tài khoản vai trò Staff chưa gắn với cán bộ khác
        private async Task LoadListsAsync(Staff s)
        {
            ViewBag.Faculties = new SelectList(await Db.Faculties.AsNoTracking().OrderBy(f => f.FacultyName).ToListAsync(), "FacultyId", "FacultyName", s.FacultyId);
            ViewBag.Users = new SelectList(await Db.Users.AsNoTracking()
                .Where(u => u.UserRoles.Any(r => r.Role.RoleName == Role.Staff) && (u.Staff == null || u.UserId == s.UserId))
                .OrderBy(u => u.Username).Select(u => new { u.UserId, Name = u.Username + " - " + u.FullName }).ToListAsync(), "UserId", "Name", s.UserId);
        }
    }
}
