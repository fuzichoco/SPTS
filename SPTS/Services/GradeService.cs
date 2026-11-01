using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;

namespace SPTS.Services
{
    // TV2: tính điểm tổng, quy đổi thang 4, GPA và tiến độ; có cache (IMemoryCache)
    public class GradeService
    {
        private const string ScalesKey = "GradeScales";
        private static readonly TimeSpan ProgressTtl = TimeSpan.FromMinutes(10);
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public GradeService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private static string ProgressKey(int studentId) => $"Progress:{studentId}";

        // Thang điểm ít thay đổi nên cache 1 giờ, xóa cache khi giáo vụ sửa thang điểm
        public async Task<List<GradeScale>> GetScalesAsync() =>
            await _cache.GetOrCreateAsync(ScalesKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return _db.GradeScales.AsNoTracking().OrderByDescending(s => s.MinScore).ToListAsync();
            }) ?? new();

        public void InvalidateScales() => _cache.Remove(ScalesKey);
        public void InvalidateProgress(int studentId) => _cache.Remove(ProgressKey(studentId));

        // Tổng = CC×10% + GK×30% + CK×60%, làm tròn 1 chữ số; đạt khi ≥ 4.0
        public async Task ApplyAsync(Grade g)
        {
            if (g.AttendanceScore is not { } cc || g.MidtermScore is not { } gk || g.FinalScore is not { } ck)
            {
                g.TotalScore = null; g.LetterGrade = null; g.GradePoint = null; g.IsPassed = false;
                return;
            }
            var total = Math.Round(cc * 0.1m + gk * 0.3m + ck * 0.6m, 1, MidpointRounding.AwayFromZero);
            var scale = (await GetScalesAsync()).FirstOrDefault(s => total >= s.MinScore);
            g.TotalScore = total;
            g.LetterGrade = scale?.LetterGrade;
            g.GradePoint = scale?.GradePoint;
            g.IsPassed = total >= 4.0m;
        }

        public static string Rank(decimal gpa4) => gpa4 switch
        {
            >= 3.6m => "Xuất sắc",
            >= 3.2m => "Giỏi",
            >= 2.5m => "Khá",
            >= 2.0m => "Trung bình",
            _ => "Yếu"
        };

        public async Task<ProgressViewModel?> GetProgressAsync(int studentId) =>
            await _cache.GetOrCreateAsync(ProgressKey(studentId), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = ProgressTtl;
                return await BuildProgressAsync(studentId);
            });

        private async Task<ProgressViewModel?> BuildProgressAsync(int studentId)
        {
            var s = await _db.Students.AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Select(x => new { x.StudentId, x.StudentCode, x.FullName, x.Status, x.Class.ClassCode, x.Class.ProgramId, x.Class.Program.ProgramName, x.Class.Program.TotalCredits })
                .FirstOrDefaultAsync();
            if (s == null) return null;

            var grades = await _db.Grades.AsNoTracking()
                .Where(g => g.StudentId == studentId)
                .Select(g => new GradeRowViewModel
                {
                    GradeId = g.GradeId, SemesterId = g.SemesterId, SemesterName = g.Semester.SemesterName, SemesterStart = g.Semester.StartDate,
                    CourseId = g.CourseId, CourseCode = g.Course.CourseCode, CourseName = g.Course.CourseName, Credits = g.Course.Credits,
                    AttemptNo = g.AttemptNo, AttendanceScore = g.AttendanceScore, MidtermScore = g.MidtermScore, FinalScore = g.FinalScore,
                    TotalScore = g.TotalScore, LetterGrade = g.LetterGrade, GradePoint = g.GradePoint, IsPassed = g.IsPassed
                })
                .ToListAsync();

            // Học lại: lấy lần có điểm cao nhất của mỗi học phần
            var best = grades.Where(g => g.TotalScore != null)
                .GroupBy(g => g.CourseId)
                .Select(x => x.OrderByDescending(g => g.TotalScore).First())
                .ToList();

            var programCourses = await _db.ProgramCourses.AsNoTracking()
                .Where(pc => pc.ProgramId == s.ProgramId && !pc.Course.IsDeleted)
                .OrderBy(pc => pc.SuggestedSemester).ThenBy(pc => pc.Course.CourseCode)
                .Select(pc => new ProgramCourseStatusViewModel
                {
                    CourseId = pc.CourseId, CourseCode = pc.Course.CourseCode, CourseName = pc.Course.CourseName, Credits = pc.Course.Credits,
                    KnowledgeBlock = pc.KnowledgeBlock, IsRequired = pc.IsRequired, ElectiveGroup = pc.ElectiveGroup != null ? pc.ElectiveGroup.GroupName : null,
                    SuggestedSemester = pc.SuggestedSemester
                })
                .ToListAsync();
            foreach (var pc in programCourses)
            {
                var g = best.FirstOrDefault(b => b.CourseId == pc.CourseId);
                pc.BestScore = g?.TotalScore;
                pc.LetterGrade = g?.LetterGrade;
                pc.Status = g == null ? CourseStatus.NotTaken : g.IsPassed ? CourseStatus.Passed : CourseStatus.Failed;
            }

            var m = new ProgressViewModel
            {
                StudentId = s.StudentId, StudentCode = s.StudentCode, FullName = s.FullName, Status = s.Status,
                ClassCode = s.ClassCode, ProgramName = s.ProgramName, TotalCredits = s.TotalCredits,
                PassedCredits = best.Where(g => g.IsPassed).Sum(g => g.Credits),
                AttemptedCredits = best.Sum(g => g.Credits),
                Gpa4 = WeightedAverage(best, g => g.GradePoint),
                Gpa10 = WeightedAverage(best, g => g.TotalScore),
                FailedCourses = best.Count(g => !g.IsPassed),
                ProgramCourses = programCourses,
                Semesters = grades.GroupBy(g => new { g.SemesterId, g.SemesterName, g.SemesterStart })
                    .OrderBy(x => x.Key.SemesterStart)
                    .Select(x => new SemesterSummaryViewModel
                    {
                        SemesterName = x.Key.SemesterName,
                        Credits = x.Sum(g => g.Credits),
                        Gpa4 = WeightedAverage(x.Where(g => g.TotalScore != null).ToList(), g => g.GradePoint),
                        Gpa10 = WeightedAverage(x.Where(g => g.TotalScore != null).ToList(), g => g.TotalScore),
                        Grades = x.OrderBy(g => g.CourseCode).ToList()
                    }).ToList()
            };
            m.Rank = Rank(m.Gpa4);
            m.IsWarned = m.AttemptedCredits > 0 && m.Gpa4 < 2.0m;
            return m;
        }

        private static decimal WeightedAverage(List<GradeRowViewModel> rows, Func<GradeRowViewModel, decimal?> value)
        {
            var credits = rows.Sum(r => r.Credits);
            return credits == 0 ? 0 : Math.Round(rows.Sum(r => (value(r) ?? 0) * r.Credits) / credits, 2);
        }
    }

    public static class QueryExtensions
    {
        // Phân trang dùng chung: Count + Skip/Take thực hiện trong SQL
        public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize = 10)
        {
            page = Math.Max(1, page);
            return new PagedResult<T>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = await query.CountAsync(),
                Items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
            };
        }
    }
}
