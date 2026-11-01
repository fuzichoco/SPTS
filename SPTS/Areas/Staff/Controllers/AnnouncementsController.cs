using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Staff.Controllers
{
    // Gửi thông báo tới sinh viên một lớp (hoặc toàn bộ sinh viên)
    [Area("Staff"), Authorize(Roles = Role.Admin + "," + Role.Staff)]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AuditLogService _audit;

        public AnnouncementsController(ApplicationDbContext db, AuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Classes = new SelectList(await _db.Classes.AsNoTracking().OrderBy(c => c.ClassCode).ToListAsync(), "ClassId", "ClassCode");
            return View(new NotificationCreateModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(NotificationCreateModel m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Classes = new SelectList(await _db.Classes.AsNoTracking().OrderBy(c => c.ClassCode).ToListAsync(), "ClassId", "ClassCode");
                return View(m);
            }
            var userIds = await _db.Students.AsNoTracking()
                .Where(s => !s.IsDeleted && s.UserId != null && (m.ClassId == null || s.ClassId == m.ClassId))
                .Select(s => s.UserId!.Value).ToListAsync();
            _db.Notifications.AddRange(userIds.Select(id => new Notification { UserId = id, Title = m.Title, Content = m.Content }));
            await _db.SaveChangesAsync();
            await _audit.LogAsync("SEND_NOTIFICATION", "Notifications", m.ClassId, null, new { m.Title, Recipients = userIds.Count });
            TempData["Message"] = $"Đã gửi thông báo tới {userIds.Count} sinh viên.";
            return RedirectToAction(nameof(Index));
        }
    }
}
