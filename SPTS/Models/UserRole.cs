namespace SPTS.Models
{
    // Khóa kép (UserId, RoleId) cấu hình bằng Fluent API
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
