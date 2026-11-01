using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    // TV4: phần dùng chung của các trang danh mục (lưu, xóa, ghi nhật ký)
    [Area("Admin"), Authorize(Roles = Role.Admin)]
    public abstract class CatalogControllerBase : Controller
    {
        protected const int PageSize = 10;
        protected readonly ApplicationDbContext Db;
        protected readonly AuditLogService Audit;

        protected CatalogControllerBase(ApplicationDbContext db, AuditLogService audit)
        {
            Db = db;
            Audit = audit;
        }

        // Lưu thay đổi; trùng mã (unique index) thì báo lỗi lên form thay vì trang lỗi
        protected async Task<bool> TrySaveAsync(string action, string table, Func<object> recordId, object? newValue)
        {
            try
            {
                await Db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Mã đã tồn tại hoặc dữ liệu liên kết không hợp lệ.");
                return false;
            }
            await Audit.LogAsync(action, table, recordId(), null, newValue);
            TempData["Message"] = "Đã lưu thành công.";
            return true;
        }

        // Xóa cứng; nếu bản ghi đang được tham chiếu (FK Restrict) thì báo lỗi
        protected async Task<IActionResult> DeleteAsync(object? entity, string table, object id, object oldValue)
        {
            if (entity == null) return NotFound();
            Db.Remove(entity);
            try
            {
                await Db.SaveChangesAsync();
                await Audit.LogAsync("DELETE", table, id, oldValue);
                TempData["Message"] = "Đã xóa.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa: dữ liệu đang được sử dụng ở bảng khác.";
            }
            return RedirectToAction("Index");
        }
    }
}
