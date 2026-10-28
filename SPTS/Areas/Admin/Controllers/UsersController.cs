using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers
{
    [Area("Admin"), Microsoft.AspNetCore.Authorization.Authorize(Roles = Role.Admin)]
    public class UsersController : Controller
    {
        private const string Table = "Users";
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly AuditLogService _audit;

        public UsersController(ApplicationDbContext db, IPasswordHasher<User> hasher, AuditLogService audit)
        {
            _db = db;
            _hasher = hasher;
            _audit = audit;
        }

        // GET: Admin/Users?Keyword=&RoleId=&IsActive=&SortOrder=&Page=
        public async Task<IActionResult> Index(UserFilterViewModel filter)
        {
            filter.Result = await QueryUsersAsync(filter);
            ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
            return View(filter);
        }

        // GET: Admin/Users/Ajax - trang quản lý tài khoản bằng AJAX
        public async Task<IActionResult> Ajax()
        {
            ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
            return View();
        }

        // GET: Admin/Users/List?keyword=&page=&pageSize= - trả JSON cho XHR/fetch
        public async Task<IActionResult> List(UserFilterViewModel filter) =>
            Json(await QueryUsersAsync(filter));

        // Search + Sort + Paging dùng chung cho Index (HTML) và List (JSON)
        private async Task<PagedResult<UserListItemViewModel>> QueryUsersAsync(UserFilterViewModel filter)
        {
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = Math.Clamp(filter.PageSize, 5, 50);

            var query = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var k = filter.Keyword.Trim();
                query = query.Where(u => u.Username.Contains(k) || u.FullName.Contains(k) || u.Email.Contains(k));
            }
            if (filter.RoleId.HasValue)
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == filter.RoleId));
            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive);

            query = filter.SortOrder switch
            {
                "name" => query.OrderBy(u => u.FullName),
                "name_desc" => query.OrderByDescending(u => u.FullName),
                "date" => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var now = DateTime.Now;
            return new PagedResult<UserListItemViewModel>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalItems = await query.CountAsync(),
                Items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(u => new UserListItemViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                        IsActive = u.IsActive,
                        IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
                        LastLoginAt = u.LastLoginAt,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync()
            };
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _db.Users.AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
            return user == null ? NotFound() : View(user);
        }

        // GET: Admin/Users/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
            return View(new UserEditModel());
        }

        // POST: Admin/Users/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserEditModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Nhập mật khẩu");
            await ValidateAsync(model, null);
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
                return View(model);
            }

            var user = await CreateUserAsync(model);
            TempData["Message"] = $"Đã tạo tài khoản {user.Username}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/CreateAjax - gọi bằng jQuery AJAX, trả JSON
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(UserEditModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Nhập mật khẩu");
            await ValidateAsync(model, null);
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            var user = await CreateUserAsync(model);
            return Json(new { success = true, message = $"Đã tạo tài khoản {user.Username}." });
        }

        private async Task<User> CreateUserAsync(UserEditModel model)
        {
            var user = new User
            {
                Username = model.Username.Trim(),
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim()
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password!);
            foreach (var roleId in model.SelectedRoleIds)
                user.UserRoles.Add(new UserRole { RoleId = roleId });

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await _audit.LogAsync("CREATE", Table, user.UserId, null, await SnapshotAsync(user.UserId));
            return user;
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.Users.AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => new UserEditModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    SelectedRoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
                })
                .FirstOrDefaultAsync();
            if (model == null) return NotFound();

            ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
            return View(model);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserEditModel model)
        {
            if (id != model.UserId) return BadRequest();
            var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            await ValidateAsync(model, id);
            var adminRoleId = await AdminRoleIdAsync();
            if (user.IsActive && user.UserRoles.Any(r => r.RoleId == adminRoleId)
                && !model.SelectedRoleIds.Contains(adminRoleId) && await IsLastActiveAdminAsync(id))
                ModelState.AddModelError(nameof(model.SelectedRoleIds), "Phải giữ lại ít nhất 1 Admin đang hoạt động.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _db.Roles.AsNoTracking().ToListAsync();
                return View(model);
            }

            var old = await SnapshotAsync(id);

            user.Username = model.Username.Trim();
            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(model.Password))   // Admin đặt lại mật khẩu
                user.PasswordHash = _hasher.HashPassword(user, model.Password);

            // Đồng bộ UserRoles: xóa vai trò bỏ chọn, thêm vai trò mới chọn
            var current = user.UserRoles.Select(r => r.RoleId).ToList();
            foreach (var ur in user.UserRoles.Where(r => !model.SelectedRoleIds.Contains(r.RoleId)).ToList())
                user.UserRoles.Remove(ur);
            foreach (var roleId in model.SelectedRoleIds.Except(current))
                user.UserRoles.Add(new UserRole { RoleId = roleId });

            user.SecurityStamp = Guid.NewGuid().ToString();   // buộc đăng nhập lại
            await _db.SaveChangesAsync();
            await _audit.LogAsync("UPDATE", Table, id, old, await SnapshotAsync(id));

            TempData["Message"] = $"Đã cập nhật tài khoản {user.Username}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Delete/5 - xác nhận khóa tài khoản
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
            return user == null ? NotFound() : View(user);
        }

        // POST: Admin/Users/Delete/5 - xóa mềm: IsActive = false
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var error = await LockUserAsync(id);
            if (error == null) TempData["Message"] = "Đã khóa tài khoản.";
            else TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/DeleteAjax/5 - gọi bằng jQuery AJAX, xóa mềm, trả JSON
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var error = await LockUserAsync(id);
            return error == null
                ? Json(new { success = true, message = "Đã khóa tài khoản." })
                : BadRequest(new { success = false, message = error });
        }

        // Khóa tài khoản (IsActive = false). Trả về thông báo lỗi, null nếu thành công
        private async Task<string?> LockUserAsync(int id)
        {
            var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return "Không tìm thấy tài khoản.";

            var adminRoleId = await AdminRoleIdAsync();
            if (user.UserRoles.Any(r => r.RoleId == adminRoleId) && await IsLastActiveAdminAsync(id))
                return "Không thể khóa Admin cuối cùng.";

            user.IsActive = false;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _audit.LogAsync("LOCK", Table, id, new { IsActive = true }, new { IsActive = false });
            return null;
        }

        // POST: Admin/Users/Unlock/5 - mở khóa, xóa trạng thái khóa do đăng nhập sai
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _audit.LogAsync("UNLOCK", Table, id, null, new { IsActive = true });

            TempData["Message"] = $"Đã mở khóa tài khoản {user.Username}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateAsync(UserEditModel model, int? id)
        {
            if (await _db.Users.AnyAsync(u => u.Username == model.Username.Trim() && u.UserId != id))
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại");
            if (await _db.Users.AnyAsync(u => u.Email == model.Email.Trim() && u.UserId != id))
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại");

            model.SelectedRoleIds = await _db.Roles
                .Where(r => model.SelectedRoleIds.Contains(r.RoleId))
                .Select(r => r.RoleId).ToListAsync();
            if (model.SelectedRoleIds.Count == 0)
                ModelState.AddModelError(nameof(model.SelectedRoleIds), "Chọn ít nhất 1 vai trò");
        }

        private Task<int> AdminRoleIdAsync() =>
            _db.Roles.Where(r => r.RoleName == Role.Admin).Select(r => r.RoleId).FirstAsync();

        private async Task<bool> IsLastActiveAdminAsync(int userId) =>
            !await _db.Users.AnyAsync(u => u.UserId != userId && u.IsActive
                && u.UserRoles.Any(ur => ur.Role.RoleName == Role.Admin));

        // Dữ liệu ghi nhật ký, không chứa PasswordHash
        private Task<object?> SnapshotAsync(int id) =>
            _db.Users.AsNoTracking().Where(u => u.UserId == id)
                .Select(u => (object?)new
                {
                    u.Username, u.FullName, u.Email, u.IsActive,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                })
                .FirstOrDefaultAsync();
    }
}
