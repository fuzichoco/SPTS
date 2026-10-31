using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SPTS.Models.ViewModels;

// Chỉ nhận các trường người quản trị được phép sửa, không bind entity.
public class FacultyEditModel
{
    [Required(ErrorMessage = "Nhập mã khoa"), StringLength(10)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang hoặc gạch dưới")]
    [Display(Name = "Mã khoa")]
    public string FacultyCode { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên khoa"), StringLength(150), Display(Name = "Tên khoa")]
    public string FacultyName { get; set; } = "";
}

public class MajorEditModel
{
    [Required(ErrorMessage = "Nhập mã ngành"), StringLength(10)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang hoặc gạch dưới")]
    [Display(Name = "Mã ngành")]
    public string MajorCode { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên ngành"), StringLength(150), Display(Name = "Tên ngành")]
    public string MajorName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Chọn khoa"), Display(Name = "Khoa")]
    public int FacultyId { get; set; }

    [BindNever] public List<SelectListItem> Faculties { get; set; } = new();
}

public class CatalogRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int MajorCount { get; set; }
    public int StaffCount { get; set; }
    public int ProgramCount { get; set; }
}

public class CatalogFilterViewModel
{
    public string? Keyword { get; set; }
    public string SortOrder { get; set; } = "code";
    public int? FacultyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    [BindNever] public PagedResult<CatalogRowViewModel> Result { get; set; } = new();
    [BindNever] public List<SelectListItem> Faculties { get; set; } = new();
}

public record CatalogResult(bool Success, string? Error = null, string Field = "", bool NotFound = false);
