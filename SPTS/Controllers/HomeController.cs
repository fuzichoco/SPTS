using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;

namespace SPTS.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(new HomeStatsViewModel
    {
        Students = await _db.Students.CountAsync(s => !s.IsDeleted),
        Courses = await _db.Courses.CountAsync(c => !c.IsDeleted),
        Programs = await _db.Programs.CountAsync(),
        Grades = await _db.Grades.CountAsync()
    });

    public IActionResult Privacy()
    {
        return View();
    }

    // Trang lỗi 404/400... có layout chung (menu, nút Đăng xuất)
    public IActionResult StatusPage(int code)
    {
        ViewBag.Code = code;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
