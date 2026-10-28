using System.ComponentModel.DataAnnotations;

namespace SPTS.Models
{
    public class ElectiveGroup
    {
        public int ElectiveGroupId { get; set; }
        public int ProgramId { get; set; }

        [Required, StringLength(150)]
        public string GroupName { get; set; } = string.Empty;

        [Range(0, 100)]
        public int MinCredits { get; set; }

        public TrainingProgram Program { get; set; } = null!;
        public ICollection<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
    }
}
