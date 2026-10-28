using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    public class Student
    {
        public int StudentId { get; set; }

        [Required, StringLength(20), Unicode(false)]
        public string StudentCode { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date), Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(100), EmailAddress]
        public string? Email { get; set; }

        [StringLength(15), Phone, Unicode(false)]
        public string? Phone { get; set; }

        public int ClassId { get; set; }

        // Đang học / Bảo lưu / Thôi học / Tốt nghiệp
        [Required, StringLength(20)]
        public string Status { get; set; } = "Đang học";

        public bool IsDeleted { get; set; }

        public User? User { get; set; }
        public Class Class { get; set; } = null!;
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
