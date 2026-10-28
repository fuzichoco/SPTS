using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SPTS.Services
{
    // Persistent Cookie: lưu trạng thái người dùng trên trình duyệt (30 ngày), mỗi tài khoản một cookie
    public class VisitInfo
    {
        public int Count { get; set; }                     // số lần truy cập (mỗi phiên tính 1 lần)
        public DateTime? Last { get; set; }                // lần truy cập gần nhất
        public Dictionary<string, int> Features { get; set; } = new();   // chức năng -> số lần mở

        public IEnumerable<KeyValuePair<string, int>> TopFeatures(int n) =>
            Features.OrderByDescending(f => f.Value).Take(n);
    }

    public static class VisitCookie
    {
        private const string SessionKey = "VisitCounted";
        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

        public static string Name(string userId) => $"SPTS.Visit.{userId}";

        public static VisitInfo Read(HttpContext ctx, string userId)
        {
            try
            {
                return ctx.Request.Cookies.TryGetValue(Name(userId), out var raw)
                    ? JsonSerializer.Deserialize<VisitInfo>(raw) ?? new VisitInfo()
                    : new VisitInfo();
            }
            catch (JsonException)
            {
                return new VisitInfo();   // cookie bị sửa tay thì làm lại từ đầu
            }
        }

        public static void Write(HttpContext ctx, string userId, VisitInfo info) =>
            ctx.Response.Cookies.Append(Name(userId), JsonSerializer.Serialize(info), new CookieOptions
            {
                Expires = DateTimeOffset.Now.Add(Lifetime),   // có Expires => Persistent Cookie
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });

        // Đầu mỗi phiên làm việc: tăng số lần truy cập, nhớ lần truy cập trước vào Session
        public static VisitInfo CountVisit(HttpContext ctx, VisitInfo info)
        {
            if (ctx.Session.GetString(SessionKey) == null)
            {
                ctx.Session.SetString(SessionKey, "1");
                ctx.Session.SetString("PreviousVisit", info.Last?.ToString("dd/MM/yyyy HH:mm") ?? "Lần đầu truy cập");
                info.Count++;
                info.Last = DateTime.Now;
            }
            return info;
        }
    }

    // Filter toàn cục: ghi nhận lượt truy cập và chức năng người dùng mở.
    // Chỉ tính khi action trả về một trang HTML (ViewResult), bỏ qua các lời gọi AJAX trả JSON
    public class VisitTrackingFilter : IActionFilter
    {
        private static readonly Dictionary<string, string> FeatureNames = new()
        {
            ["Dashboard"] = "Bảng điều khiển",
            ["Users"] = "Quản lý tài khoản",
            ["AuditLogs"] = "Nhật ký hệ thống"
        };

        public void OnActionExecuting(ActionExecutingContext context) { }

        // Chạy trước khi trang được render nên vẫn kịp ghi cookie vào response
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var http = context.HttpContext;
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !HttpMethods.IsGet(http.Request.Method) || context.Result is not ViewResult)
                return;

            var info = VisitCookie.CountVisit(http, VisitCookie.Read(http, userId));
            var controller = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
            if (controller != null && FeatureNames.TryGetValue(controller, out var feature))
                info.Features[feature] = info.Features.GetValueOrDefault(feature) + 1;
            VisitCookie.Write(http, userId, info);
            http.Items[nameof(VisitInfo)] = info;   // view đọc giá trị vừa cập nhật
        }

        public static VisitInfo? Current(HttpContext http) => http.Items[nameof(VisitInfo)] as VisitInfo;
    }
}
