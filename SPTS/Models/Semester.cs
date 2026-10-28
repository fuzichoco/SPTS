using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPTS.Models
{
    public class Semester
    {
        public int SemesterId { get; set; }

        [Required, StringLength(50)]
        public string SemesterName { get; set; } = string.Empty;

        [DataType(DataType.Date), Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date), Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        // Học kỳ đã khóa thì không cho sửa điểm
        public bool IsLocked { get; set; }

        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
