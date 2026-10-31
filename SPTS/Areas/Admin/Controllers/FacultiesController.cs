using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Role.Admin)]
public class FacultiesController(CatalogService service) : Controller
{
    public async Task<IActionResult> Index(CatalogFilterViewModel filter) =>
        View(await service.ListAsync(filter, false));

    public async Task<IActionResult> Details(int id)
    {
        var row = await service.FacultyDetailAsync(id);
        return row == null ? NotFound() : View(row);
    }

    public IActionResult Create()
    {
        var model = new FacultyEditModel();

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyEditModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await service.SaveFacultyAsync(null, model);
            if (result.Success)
            {
                TempData["Message"] = "Đã thêm khoa.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field, result.Error!);
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var row = await service.FacultyDetailAsync(id);
        if (row == null) return NotFound();
        var model = new FacultyEditModel {
            FacultyCode = row.Code, FacultyName = row.Name
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacultyEditModel model)
    {
        if (await service.FacultyDetailAsync(id) == null) return NotFound();
        if (ModelState.IsValid)
        {
            var result = await service.SaveFacultyAsync(id, model);
            if (result.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["Message"] = "Đã cập nhật khoa.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field, result.Error!);
        }

        return View(model);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var row = await service.FacultyDetailAsync(id);
        return row == null ? NotFound() : View(row);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await service.DeleteFacultyAsync(id);
        if (result.NotFound) return NotFound();
        if (result.Success)
        {
            TempData["Message"] = "Đã xóa khoa.";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError("", result.Error!);
        var row = await service.FacultyDetailAsync(id);
        return row == null ? NotFound() : View("Delete", row);
    }
}
