using System.ComponentModel.DataAnnotations;

namespace SPTS.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // Đổi khi khóa/đổi mật khẩu để vô hiệu hóa cookie cũ
        [StringLength(50)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public Student? Student { get; set; }
        public Staff? Staff { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
