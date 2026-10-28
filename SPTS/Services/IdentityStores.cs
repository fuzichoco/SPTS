using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SPTS.Data;
using SPTS.Models;

namespace SPTS.Services
{
    // Cho ASP.NET Core Identity (UserManager, SignInManager) làm việc trên bảng Users/Roles/UserRoles có sẵn,
    // không cần các bảng AspNetUsers mặc định
    public class UserStore :
        IUserPasswordStore<User>,
        IUserSecurityStampStore<User>,
        IUserLockoutStore<User>,
        IUserRoleStore<User>
    {
        private readonly ApplicationDbContext _db;

        public UserStore(ApplicationDbContext db) => _db = db;

        public void Dispose() { }

        // ----- IUserStore -----
        public Task<string> GetUserIdAsync(User user, CancellationToken ct) => Task.FromResult(user.UserId.ToString());
        public Task<string?> GetUserNameAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Username);
        public Task SetUserNameAsync(User user, string? userName, CancellationToken ct) { user.Username = userName ?? ""; return Task.CompletedTask; }
        // Bảng không có cột NormalizedUserName: collation của SQL Server không phân biệt hoa thường nên so sánh trực tiếp Username
        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Username.ToUpperInvariant());
        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken ct) => Task.CompletedTask;

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken ct)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken ct)
        {
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return IdentityResult.Success;
        }

        // Hệ thống không xóa cứng tài khoản (xóa mềm bằng IsActive)
        public Task<IdentityResult> DeleteAsync(User user, CancellationToken ct) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Không xóa cứng tài khoản, hãy khóa tài khoản." }));

        public Task<User?> FindByIdAsync(string userId, CancellationToken ct) =>
            int.TryParse(userId, out var id) ? _db.Users.FirstOrDefaultAsync(u => u.UserId == id, ct) : Task.FromResult<User?>(null);

        public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
            _db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUserName, ct);

        // ----- Mật khẩu -----
        public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken ct) { user.PasswordHash = passwordHash ?? ""; return Task.CompletedTask; }
        public Task<string?> GetPasswordHashAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.PasswordHash);
        public Task<bool> HasPasswordAsync(User user, CancellationToken ct) => Task.FromResult(user.PasswordHash.Length > 0);

        // ----- SecurityStamp: đổi stamp thì cookie cũ mất hiệu lực -----
        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken ct) { user.SecurityStamp = stamp; return Task.CompletedTask; }
        public Task<string?> GetSecurityStampAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.SecurityStamp);

        // ----- Khóa khi đăng nhập sai (LockoutEnd lưu giờ địa phương) -----
        public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken ct) =>
            Task.FromResult(user.LockoutEnd.HasValue ? new DateTimeOffset(user.LockoutEnd.Value) : (DateTimeOffset?)null);
        public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken ct) { user.LockoutEnd = lockoutEnd?.LocalDateTime; return Task.CompletedTask; }
        public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken ct) => Task.FromResult(++user.FailedLoginCount);
        public Task ResetAccessFailedCountAsync(User user, CancellationToken ct) { user.FailedLoginCount = 0; return Task.CompletedTask; }
        public Task<int> GetAccessFailedCountAsync(User user, CancellationToken ct) => Task.FromResult(user.FailedLoginCount);
        public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken ct) => Task.FromResult(true);
        public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken ct) => Task.CompletedTask;

        // ----- Vai trò qua bảng UserRoles -----
        public async Task AddToRoleAsync(User user, string roleName, CancellationToken ct)
        {
            var role = await _db.Roles.FirstAsync(r => r.RoleName == roleName, ct);
            _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        }

        public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken ct)
        {
            var ur = await _db.UserRoles.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Role.RoleName == roleName, ct);
            if (ur != null) _db.UserRoles.Remove(ur);
        }

        public async Task<IList<string>> GetRolesAsync(User user, CancellationToken ct) =>
            await _db.UserRoles.Where(x => x.UserId == user.UserId).Select(x => x.Role.RoleName).ToListAsync(ct);

        public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken ct) =>
            _db.UserRoles.AnyAsync(x => x.UserId == user.UserId && x.Role.RoleName == roleName, ct);

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken ct) =>
            await _db.Users.Where(u => u.UserRoles.Any(x => x.Role.RoleName == roleName)).ToListAsync(ct);
    }

    // Tạo cookie đăng nhập: Identity tự thêm UserId, Username, SecurityStamp; thêm FullName và các Role
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        public AppClaimsPrincipalFactory(UserManager<User> userManager, IOptions<IdentityOptions> options)
            : base(userManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim("FullName", user.FullName));
            foreach (var role in await UserManager.GetRolesAsync(user))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            return identity;
        }
    }
}
