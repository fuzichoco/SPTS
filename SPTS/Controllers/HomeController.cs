using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SPTS.Models;

namespace SPTS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

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
