using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;

namespace SPTS.Areas.Admin.Controllers
{
    [Area("Admin"), Authorize(Roles = Role.Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db) => _db = db;

        // GET: Admin/Dashboard - trang chủ quản trị sau khi đăng nhập
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            var users = _db.Users.AsNoTracking();
            var todayLogs = _db.AuditLogs.AsNoTracking().Where(l => l.CreatedAt >= today);

            var model = new DashboardViewModel
            {
                TotalUsers = await users.CountAsync(),
                ActiveUsers = await users.CountAsync(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= now)),
                LockedUsers = await users.CountAsync(u => !u.IsActive || u.LockoutEnd > now),
                LoginsToday = await todayLogs.CountAsync(l => l.Action == "LOGIN_SUCCESS"),
                FailedLoginsToday = await todayLogs.CountAsync(l => l.Action == "LOGIN_FAILED"),
                // Đếm trong SQL (Select trước), không đếm trên navigation chưa được nạp
                UsersByRole = await _db.Roles.AsNoTracking()
                    .Select(r => new { r.RoleName, Count = r.UserRoles.Count })
                    .ToDictionaryAsync(x => x.RoleName, x => x.Count),
                RecentLogs = await _db.AuditLogs.AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
                    .Take(8)
                    .Select(l => new AuditLogDetailViewModel
                    {
                        LogId = l.LogId,
                        Username = l.User != null ? l.User.Username : null,
                        Action = l.Action,
                        TableName = l.TableName,
                        RecordId = l.RecordId,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync()
            };
            return View(model);
        }
    }
}
