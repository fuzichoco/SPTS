using System.ComponentModel.DataAnnotations;

namespace SPTS.Models
{
    public class Role
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Student = "Student";

        public int RoleId { get; set; }

        [Required, StringLength(30)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
