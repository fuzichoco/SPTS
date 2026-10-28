using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    // Unique (StudentId, CourseId, SemesterId) cấu hình bằng Fluent API
    public class Grade
    {
        public int GradeId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }

        [Range(1, 10)]
        public int AttemptNo { get; set; } = 1;

        [Range(0, 10)] public decimal? AttendanceScore { get; set; }
        [Range(0, 10)] public decimal? MidtermScore { get; set; }
        [Range(0, 10)] public decimal? FinalScore { get; set; }
        [Range(0, 10)] public decimal? TotalScore { get; set; }

        [StringLength(2), Unicode(false)]
        public string? LetterGrade { get; set; }

        [Range(0, 4)]
        public decimal? GradePoint { get; set; }

        public bool IsPassed { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Student Student { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public Semester Semester { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}
