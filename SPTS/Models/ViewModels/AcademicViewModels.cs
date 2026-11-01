using System.ComponentModel.DataAnnotations;

namespace SPTS.Models.ViewModels
{
    public enum CourseStatus { NotTaken, Passed, Failed }

    public class GradeRowViewModel
    {
        public int GradeId { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = "";
        public DateTime SemesterStart { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int Credits { get; set; }
        public int AttemptNo { get; set; }
        public decimal? AttendanceScore { get; set; }
        public decimal? MidtermScore { get; set; }
        public decimal? FinalScore { get; set; }
        public decimal? TotalScore { get; set; }
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
        public bool IsPassed { get; set; }
    }

    public class SemesterSummaryViewModel
    {
        public string SemesterName { get; set; } = "";
        public int Credits { get; set; }
        public decimal Gpa4 { get; set; }
        public decimal Gpa10 { get; set; }
        public List<GradeRowViewModel> Grades { get; set; } = new();
    }

    public class ProgramCourseStatusViewModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int Credits { get; set; }
        public string KnowledgeBlock { get; set; } = "";
        public bool IsRequired { get; set; }
        public string? ElectiveGroup { get; set; }
        public int SuggestedSemester { get; set; }
        public decimal? BestScore { get; set; }
        public string? LetterGrade { get; set; }
        public CourseStatus Status { get; set; }
    }

    // TV3: tiến độ học tập của một sinh viên
    public class ProgressViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Status { get; set; } = "";
        public string ClassCode { get; set; } = "";
        public string ProgramName { get; set; } = "";
        public int TotalCredits { get; set; }
        public int PassedCredits { get; set; }
        public int AttemptedCredits { get; set; }
        public int FailedCourses { get; set; }
        public decimal Gpa4 { get; set; }
        public decimal Gpa10 { get; set; }
        public string Rank { get; set; } = "";
        public bool IsWarned { get; set; }
        public int Percent => TotalCredits == 0 ? 0 : Math.Min(100, PassedCredits * 100 / TotalCredits);
        public List<SemesterSummaryViewModel> Semesters { get; set; } = new();
        public List<ProgramCourseStatusViewModel> ProgramCourses { get; set; } = new();
    }

    // TV2: danh sách điểm cho giáo vụ
    public class GradeFilterViewModel
    {
        public int? SemesterId { get; set; }
        public int? ClassId { get; set; }
        public int? CourseId { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public PagedResult<GradeListItemViewModel> Result { get; set; } = new();
    }

    public class GradeListItemViewModel
    {
        public int GradeId { get; set; }
        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string ClassCode { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string SemesterName { get; set; } = "";
        public bool SemesterLocked { get; set; }
        public int AttemptNo { get; set; }
        public decimal? AttendanceScore { get; set; }
        public decimal? MidtermScore { get; set; }
        public decimal? FinalScore { get; set; }
        public decimal? TotalScore { get; set; }
        public string? LetterGrade { get; set; }
        public bool IsPassed { get; set; }
    }

    public class GradeEditModel
    {
        public int? GradeId { get; set; }
        [Required(ErrorMessage = "Chọn sinh viên"), Display(Name = "Sinh viên")] public int? StudentId { get; set; }
        [Required(ErrorMessage = "Chọn học phần"), Display(Name = "Học phần")] public int? CourseId { get; set; }
        [Required(ErrorMessage = "Chọn học kỳ"), Display(Name = "Học kỳ")] public int? SemesterId { get; set; }
        [Range(1, 10), Display(Name = "Lần học")] public int AttemptNo { get; set; } = 1;
        [Range(0, 10, ErrorMessage = "Điểm từ 0 đến 10"), Display(Name = "Chuyên cần (10%)")] public decimal? AttendanceScore { get; set; }
        [Range(0, 10, ErrorMessage = "Điểm từ 0 đến 10"), Display(Name = "Giữa kỳ (30%)")] public decimal? MidtermScore { get; set; }
        [Range(0, 10, ErrorMessage = "Điểm từ 0 đến 10"), Display(Name = "Cuối kỳ (60%)")] public decimal? FinalScore { get; set; }
    }

    // Lưu điểm nhanh bằng AJAX trên bảng điểm
    public class GradeQuickSaveModel
    {
        public int GradeId { get; set; }
        [Range(0, 10)] public decimal? AttendanceScore { get; set; }
        [Range(0, 10)] public decimal? MidtermScore { get; set; }
        [Range(0, 10)] public decimal? FinalScore { get; set; }
    }

    // TV3: báo cáo thống kê theo lớp
    public class ClassReportViewModel
    {
        public int? ClassId { get; set; }
        public string? ClassCode { get; set; }
        public List<ProgressViewModel> Students { get; set; } = new();
        public Dictionary<string, int> RankDistribution { get; set; } = new();
        public List<CoursePassRateViewModel> Courses { get; set; } = new();
        public decimal AverageGpa => Students.Count == 0 ? 0 : Math.Round(Students.Average(s => s.Gpa4), 2);
    }

    public class CoursePassRateViewModel
    {
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int Total { get; set; }
        public int Passed { get; set; }
        public decimal Average { get; set; }
        public int Rate => Total == 0 ? 0 : Passed * 100 / Total;
    }

    public class CourseLookupItemViewModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int Credits { get; set; }
        public string? Description { get; set; }
        public List<string> Prerequisites { get; set; } = new();
    }

    public class NotificationCreateModel
    {
        [Required(ErrorMessage = "Nhập tiêu đề"), StringLength(200), Display(Name = "Tiêu đề")] public string Title { get; set; } = "";
        [Required(ErrorMessage = "Nhập nội dung"), StringLength(1000), Display(Name = "Nội dung")] public string Content { get; set; } = "";
        [Display(Name = "Gửi tới lớp")] public int? ClassId { get; set; }
    }

    public class HomeStatsViewModel
    {
        public int Students { get; set; }
        public int Courses { get; set; }
        public int Programs { get; set; }
        public int Grades { get; set; }
    }
}
