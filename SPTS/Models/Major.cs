using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    public class Major
    {
        public int MajorId { get; set; }

        [Required, StringLength(10), Unicode(false)]
        public string MajorCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string MajorName { get; set; } = string.Empty;

        public int FacultyId { get; set; }

        public Faculty Faculty { get; set; } = null!;
        public ICollection<TrainingProgram> Programs { get; set; } = new List<TrainingProgram>();
    }
}
