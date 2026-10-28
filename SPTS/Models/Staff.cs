using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Staff")]
    public class Staff
    {
        public int StaffId { get; set; }

        [Required, StringLength(20), Unicode(false)]
        public string StaffCode { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Position { get; set; }

        public int FacultyId { get; set; }

        public User? User { get; set; }
        public Faculty Faculty { get; set; } = null!;
        public ICollection<Class> AdvisedClasses { get; set; } = new List<Class>();
    }
}
