using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Services;

namespace SPTS.Controllers
{
    // Thông báo của người dùng đang đăng nhập (mọi vai trò)
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public NotificationsController(ApplicationDbContext db) => _db = db;

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index(int page = 1) =>
            View(await _db.Notifications.AsNoTracking().Where(n => n.UserId == UserId)
                .OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt).ToPagedAsync(page, 10));

        // POST: Notifications/MarkRead/5 - gọi bằng fetch, trả số thông báo chưa đọc
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _db.Notifications.Where(n => n.NotificationId == id && n.UserId == UserId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return Json(new { unread = await _db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            await _db.Notifications.Where(n => n.UserId == UserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return RedirectToAction(nameof(Index));
        }
    }
}
