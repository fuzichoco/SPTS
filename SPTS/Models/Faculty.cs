using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    public class Faculty
    {
        public int FacultyId { get; set; }

        [Required, StringLength(10), Unicode(false)]
        public string FacultyCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string FacultyName { get; set; } = string.Empty;

        public ICollection<Major> Majors { get; set; } = new List<Major>();
        public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }
}
