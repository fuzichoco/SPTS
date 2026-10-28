using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required, StringLength(20), Unicode(false)]
        public string CourseCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Credits { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsDeleted { get; set; }

        // Các học phần tiên quyết của học phần này
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        // Các học phần lấy học phần này làm tiên quyết
        public ICollection<CoursePrerequisite> RequiredFor { get; set; } = new List<CoursePrerequisite>();
        public ICollection<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
