using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models.ViewModels;

namespace SPTS.Areas.Admin.Controllers
{
    // Chỉ đọc: không có action sửa/xóa nhật ký
    [Area("Admin"), Microsoft.AspNetCore.Authorization.Authorize(Roles = SPTS.Models.Role.Admin)]
    public class AuditLogsController : Controller
    {
        private const int MaxRangeDays = 90;
        private readonly ApplicationDbContext _db;

        public AuditLogsController(ApplicationDbContext db) => _db = db;

        // GET: Admin/AuditLogs?Username=&Action=&TableName=&FromDate=&ToDate=&Page=
        public async Task<IActionResult> Index(AuditLogFilterViewModel f)
        {
            f.Page = Math.Max(1, f.Page);
            f.PageSize = Math.Clamp(f.PageSize, 10, 100);
            f.ToDate = (f.ToDate ?? DateTime.Today).Date;
            f.FromDate = (f.FromDate ?? f.ToDate.Value.AddDays(-30)).Date;
            if (f.FromDate > f.ToDate)
                (f.FromDate, f.ToDate) = (f.ToDate, f.FromDate);
            if ((f.ToDate.Value - f.FromDate.Value).TotalDays > MaxRangeDays)
            {
                f.FromDate = f.ToDate.Value.AddDays(-MaxRangeDays);
                TempData["Error"] = $"Khoảng thời gian tối đa {MaxRangeDays} ngày, đã tự điều chỉnh Từ ngày.";
            }

            var to = f.ToDate.Value.AddDays(1);
            var query = _db.AuditLogs.AsNoTracking()
                .Where(l => l.CreatedAt >= f.FromDate && l.CreatedAt < to);

            if (!string.IsNullOrWhiteSpace(f.Username))
                query = query.Where(l => l.User != null && l.User.Username.Contains(f.Username.Trim()));
            if (!string.IsNullOrWhiteSpace(f.Action))
                query = query.Where(l => l.Action == f.Action);
            if (!string.IsNullOrWhiteSpace(f.TableName))
                query = query.Where(l => l.TableName == f.TableName);

            f.Items = new PagedResult<AuditLogDetailViewModel>
            {
                Page = f.Page,
                PageSize = f.PageSize,
                TotalItems = await query.CountAsync(),
                Items = await query
                    .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
                    .Skip((f.Page - 1) * f.PageSize)
                    .Take(f.PageSize)
                    .Select(l => new AuditLogDetailViewModel
                    {
                        LogId = l.LogId,
                        Username = l.User != null ? l.User.Username : null,
                        Action = l.Action,
                        TableName = l.TableName,
                        RecordId = l.RecordId,
                        IpAddress = l.IpAddress,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync()
            };

            ViewBag.Actions = await _db.AuditLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();
            ViewBag.Tables = await _db.AuditLogs.Where(l => l.TableName != null)
                .Select(l => l.TableName!).Distinct().OrderBy(t => t).ToListAsync();
            return View(f);
        }

        // GET: Admin/AuditLogs/Detail/5
        public async Task<IActionResult> Detail(long id)
        {
            var log = await _db.AuditLogs.AsNoTracking()
                .Where(l => l.LogId == id)
                .Select(l => new AuditLogDetailViewModel
                {
                    LogId = l.LogId,
                    Username = l.User != null ? l.User.Username : null,
                    Action = l.Action,
                    TableName = l.TableName,
                    RecordId = l.RecordId,
                    OldValue = l.OldValue,
                    NewValue = l.NewValue,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                })
                .FirstOrDefaultAsync();
            return log == null ? NotFound() : View(log);
        }
    }
}
