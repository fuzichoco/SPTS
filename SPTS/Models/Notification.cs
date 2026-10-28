using System.ComponentModel.DataAnnotations;

namespace SPTS.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; } = null!;
    }
}
