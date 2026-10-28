using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SPTS.Models.ViewModels
{
    public interface IPagedResult
    {
        int Page { get; }
        int TotalPages { get; }
        int TotalItems { get; }
    }

    public class PagedResult<T> : IPagedResult
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }

    public class UserFilterViewModel
    {
        public string? Keyword { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public string? SortOrder { get; set; }   // name, name_desc, date, date_desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        [BindNever]
        public PagedResult<UserListItemViewModel> Result { get; set; } = new();
    }

    public class UserListItemViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Dùng cho Create/Edit, không có PasswordHash
    public class UserEditModel
    {
        public int? UserId { get; set; }

        [Required(ErrorMessage = "Nhập tên đăng nhập"), StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu chấm, gạch dưới")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Nhập họ tên"), StringLength(100), Display(Name = "Họ tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
        public string Email { get; set; } = "";

        // Bắt buộc khi tạo mới; khi sửa để trống = giữ nguyên
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        [DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string? Password { get; set; }

        [Display(Name = "Vai trò")]
        public List<int> SelectedRoleIds { get; set; } = new();
    }

    public class AuditLogFilterViewModel
    {
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? TableName { get; set; }
        [DataType(DataType.Date)] public DateTime? FromDate { get; set; }
        [DataType(DataType.Date)] public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        [BindNever]
        public PagedResult<AuditLogDetailViewModel> Items { get; set; } = new();
    }

    public class AuditLogDetailViewModel
    {
        public long LogId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = "";
        public string? TableName { get; set; }
        public string? RecordId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
