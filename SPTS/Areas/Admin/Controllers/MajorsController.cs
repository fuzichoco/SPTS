using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Services;

namespace SPTS.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Role.Admin)]
public class MajorsController(CatalogService service) : Controller
{
    public async Task<IActionResult> Index(CatalogFilterViewModel filter) =>
        View(await service.ListAsync(filter, true));

    public async Task<IActionResult> Details(int id)
    {
        var row = await service.MajorDetailAsync(id);
        return row == null ? NotFound() : View(row);
    }

    public async Task<IActionResult> Create()
    {
        var model = new MajorEditModel();
        model.Faculties = await service.FacultyOptionsAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MajorEditModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await service.SaveMajorAsync(null, model);
            if (result.Success)
            {
                TempData["Message"] = "Đã thêm ngành.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field, result.Error!);
        }
        model.Faculties = await service.FacultyOptionsAsync();
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var row = await service.MajorDetailAsync(id);
        if (row == null) return NotFound();
        var model = new MajorEditModel {
            MajorCode = row.Code, MajorName = row.Name , FacultyId = row.FacultyId!.Value
        };
        model.Faculties = await service.FacultyOptionsAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MajorEditModel model)
    {
        if (await service.MajorDetailAsync(id) == null) return NotFound();
        if (ModelState.IsValid)
        {
            var result = await service.SaveMajorAsync(id, model);
            if (result.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["Message"] = "Đã cập nhật ngành.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field, result.Error!);
        }
        model.Faculties = await service.FacultyOptionsAsync();
        return View(model);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var row = await service.MajorDetailAsync(id);
        return row == null ? NotFound() : View(row);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await service.DeleteMajorAsync(id);
        if (result.NotFound) return NotFound();
        if (result.Success)
        {
            TempData["Message"] = "Đã xóa ngành.";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError("", result.Error!);
        var row = await service.MajorDetailAsync(id);
        return row == null ? NotFound() : View("Delete", row);
    }
}
