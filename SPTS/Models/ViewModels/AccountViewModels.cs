using System.ComponentModel.DataAnnotations;

namespace SPTS.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Nhập tên đăng nhập"), StringLength(50), Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu"), DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Display(Name = "Ghi nhớ đăng nhập (7 ngày)")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int LoginsToday { get; set; }
        public int FailedLoginsToday { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public List<AuditLogDetailViewModel> RecentLogs { get; set; } = new();
    }
}
