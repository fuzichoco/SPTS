using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SPTS.Models
{
    // Bảng tra cứu độc lập, không có khóa ngoại
    public class GradeScale
    {
        public int GradeScaleId { get; set; }

        [Range(0, 10)] public decimal MinScore { get; set; }
        [Range(0, 10)] public decimal MaxScore { get; set; }

        [Required, StringLength(2), Unicode(false)]
        public string LetterGrade { get; set; } = string.Empty;

        [Range(0, 4)]
        public decimal GradePoint { get; set; }
    }
}
