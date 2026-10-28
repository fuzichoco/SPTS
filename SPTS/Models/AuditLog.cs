using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    public class AuditLog
    {
        [Key]
        public long LogId { get; set; }

        // Null khi chưa đăng nhập (vd đăng nhập sai tên) hoặc do hệ thống ghi
        public int? UserId { get; set; }

        // LOGIN_SUCCESS, CREATE_USER, UPDATE_GRADE...
        [Required, StringLength(50), Unicode(false)]
        public string Action { get; set; } = string.Empty;

        [StringLength(50), Unicode(false)]
        public string? TableName { get; set; }

        [StringLength(50), Unicode(false)]
        public string? RecordId { get; set; }

        // Giá trị trước/sau dạng JSON, nvarchar(max)
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [StringLength(45), Unicode(false)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }
}
