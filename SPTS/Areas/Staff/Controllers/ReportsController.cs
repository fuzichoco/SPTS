using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Staff.Controllers
{
    // TV3 - báo cáo thống kê theo lớp, tra cứu tiến độ từng sinh viên
    [Area("Staff"), Authorize(Roles = Role.Admin + "," + Role.Staff)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly GradeService _grades;

        public ReportsController(ApplicationDbContext db, GradeService grades)
        {
            _db = db;
            _grades = grades;
        }

        public async Task<IActionResult> Index(int? classId)
        {
            var classes = await _db.Classes.AsNoTracking().OrderBy(c => c.ClassCode).ToListAsync();
            classId ??= classes.FirstOrDefault()?.ClassId;
            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassCode", classId);
            var m = new ClassReportViewModel { ClassId = classId, ClassCode = classes.FirstOrDefault(c => c.ClassId == classId)?.ClassCode };
            if (classId == null) return View(m);

            var ids = await _db.Students.AsNoTracking().Where(s => s.ClassId == classId && !s.IsDeleted)
                .OrderBy(s => s.StudentCode).Select(s => s.StudentId).ToListAsync();
            foreach (var id in ids)
                if (await _grades.GetProgressAsync(id) is { } p) m.Students.Add(p);

            m.RankDistribution = new[] { "Xuất sắc", "Giỏi", "Khá", "Trung bình", "Yếu" }
                .ToDictionary(r => r, r => m.Students.Count(s => s.AttemptedCredits > 0 && s.Rank == r));
            m.Courses = await _db.Grades.AsNoTracking()
                .Where(g => g.Student.ClassId == classId && !g.Student.IsDeleted && g.TotalScore != null)
                .GroupBy(g => new { g.Course.CourseCode, g.Course.CourseName })
                .Select(x => new CoursePassRateViewModel
                {
                    CourseCode = x.Key.CourseCode, CourseName = x.Key.CourseName,
                    Total = x.Count(), Passed = x.Count(g => g.IsPassed), Average = x.Average(g => g.TotalScore!.Value)
                })
                .OrderBy(x => x.CourseCode).ToListAsync();
            return View(m);
        }

        // Giáo vụ / cố vấn xem tiến độ của một sinh viên
        public async Task<IActionResult> Student(int id)
        {
            var p = await _grades.GetProgressAsync(id);
            return p == null ? NotFound() : View(p);
        }

        // Xuất CSV bảng tổng hợp lớp (mở được bằng Excel)
        public async Task<IActionResult> ExportCsv(int classId)
        {
            var ids = await _db.Students.AsNoTracking().Where(s => s.ClassId == classId && !s.IsDeleted)
                .OrderBy(s => s.StudentCode).Select(s => s.StudentId).ToListAsync();
            var sb = new System.Text.StringBuilder("MSSV,Họ tên,Tín chỉ đạt,GPA (4),GPA (10),Xếp loại,Cảnh báo\n");
            foreach (var id in ids)
                if (await _grades.GetProgressAsync(id) is { } p)
                    sb.AppendLine($"{p.StudentCode},\"{p.FullName}\",{p.PassedCredits},{p.Gpa4},{p.Gpa10},{p.Rank},{(p.IsWarned ? "Có" : "")}");
            // BOM để Excel đọc đúng tiếng Việt
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"BaoCao_Lop_{classId}.csv");
        }
    }
}
