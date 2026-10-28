using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    // Lớp sinh hoạt
    public class Class
    {
        public int ClassId { get; set; }

        [Required, StringLength(20), Unicode(false)]
        public string ClassCode { get; set; } = string.Empty;

        public int ProgramId { get; set; }

        // Cố vấn học tập (Staff)
        public int? AdvisorId { get; set; }

        public TrainingProgram Program { get; set; } = null!;
        public Staff? Advisor { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
