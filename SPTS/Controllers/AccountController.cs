using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Controllers
{
    // UC-01: Đăng nhập / Đăng xuất bằng ASP.NET Core Identity (SignInManager, UserManager)
    public class AccountController : Controller
    {
        // Một thông báo chung cho mọi trường hợp sai, không tiết lộ tài khoản có tồn tại hay không
        private const string LoginError = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đang bị khóa.";

        private readonly SignInManager<User> _signIn;
        private readonly UserManager<User> _users;
        private readonly AuditLogService _audit;

        public AccountController(SignInManager<User> signIn, UserManager<User> users, AuditLogService audit)
        {
            _signIn = signIn;
            _users = users;
            _audit = audit;
        }

        // NoStore: bấm Back không hiện lại form cũ với token AntiForgery đã hết hiệu lực
        [HttpGet, AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectByRole(User);
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.FindByNameAsync(model.Username.Trim());
            if (user == null || !user.IsActive)
                return await LoginFailedAsync(model, user?.UserId, "Tài khoản không tồn tại hoặc đã bị khóa");

            // Identity kiểm tra mật khẩu, tự tăng số lần sai và khóa 15 phút sau 5 lần sai
            var result = await _signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return await LoginFailedAsync(model, user.UserId, result.IsLockedOut ? "Đang tạm khóa" : "Sai mật khẩu");

            // "Ghi nhớ đăng nhập": Persistent Cookie 7 ngày; không chọn: Session Cookie, mất khi đóng trình duyệt
            await _signIn.SignInAsync(user, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            });

            user.LastLoginAt = DateTime.Now;
            await _users.UpdateAsync(user);

            // Session: thông tin phiên làm việc
            var roles = await _users.GetRolesAsync(user);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Roles", string.Join(", ", roles));
            HttpContext.Session.SetString("LoginAt", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            await _audit.LogAsync("LOGIN_SUCCESS", "Users", user.UserId, userId: user.UserId);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectByRole(roles);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _audit.LogAsync("LOGOUT", "Users", User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _signIn.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        private async Task<IActionResult> LoginFailedAsync(LoginViewModel model, int? userId, string reason)
        {
            await _audit.LogAsync("LOGIN_FAILED", "Users", userId, null,
                new { Username = model.Username, Reason = reason }, userId);
            ModelState.AddModelError(string.Empty, LoginError);
            model.Password = string.Empty;
            return View(model);
        }

        private IActionResult RedirectByRole(ClaimsPrincipal user) =>
            RedirectByRole(user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList());

        // Admin → /Admin/Dashboard, Staff → /Staff/Grades, Student → /Student/Progress
        private IActionResult RedirectByRole(IList<string> roles)
        {
            if (roles.Contains(Role.Admin)) return Redirect("/Admin/Dashboard");
            if (roles.Contains(Role.Staff)) return Redirect("/Staff/Grades");
            if (roles.Contains(Role.Student)) return Redirect("/Student/Progress");
            return RedirectToAction("Index", "Home");
        }
    }
}
