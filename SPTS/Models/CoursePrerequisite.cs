namespace SPTS.Models
{
    // Khóa kép (CourseId, PrerequisiteId)
    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public int PrerequisiteId { get; set; }

        public Course Course { get; set; } = null!;
        public Course Prerequisite { get; set; } = null!;
    }
}
