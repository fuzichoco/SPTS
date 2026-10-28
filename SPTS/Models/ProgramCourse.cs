using System.ComponentModel.DataAnnotations;

namespace SPTS.Models
{
    // Khóa kép (ProgramId, CourseId)
    public class ProgramCourse
    {
        public int ProgramId { get; set; }
        public int CourseId { get; set; }

        [Required, StringLength(50)]
        public string KnowledgeBlock { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;
        public int? ElectiveGroupId { get; set; }

        [Range(1, 12)]
        public int SuggestedSemester { get; set; }

        public TrainingProgram Program { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public ElectiveGroup? ElectiveGroup { get; set; }
    }
}
