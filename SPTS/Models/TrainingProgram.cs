using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    // Chương trình đào tạo (CTĐT). Không đặt tên "Program" để tránh trùng lớp Program của Program.cs
    [Table("Programs")]
    public class TrainingProgram
    {
        [Key]
        public int ProgramId { get; set; }

        [Required, StringLength(20), Unicode(false)]
        public string ProgramCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string ProgramName { get; set; } = string.Empty;

        public int MajorId { get; set; }

        [Range(2000, 2100)]
        public int CohortYear { get; set; }

        [Range(1, 300)]
        public int TotalCredits { get; set; }

        public Major Major { get; set; } = null!;
        public ICollection<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
        public ICollection<ElectiveGroup> ElectiveGroups { get; set; } = new List<ElectiveGroup>();
        public ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}
