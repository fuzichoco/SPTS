using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPTS.Data;
using SPTS.Models;
using SPTS.Services;

var builder = WebApplication.CreateBuilder(args);

// Định dạng ngày kiểu Việt Nam nhưng số thập phân dùng dấu chấm (khớp <input type="number">)
var culture = new CultureInfo("vi-VN") { NumberFormat = CultureInfo.InvariantCulture.NumberFormat };
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;

// Add services to the container.
// Filter toàn cục ghi Persistent Cookie (số lần truy cập, chức năng hay dùng)
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add<VisitTrackingFilter>();
    // Navigation property (Faculty, Class...) không bị coi là trường bắt buộc khi bind form
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Không mã hóa chữ tiếng Việt thành &#x...; trong HTML
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();
// TV2: tính điểm, GPA, tiến độ có cache trong bộ nhớ
builder.Services.AddMemoryCache();
builder.Services.AddScoped<GradeService>();

// ASP.NET Core Identity trên bảng Users/Roles có sẵn (UserStore tự viết)
builder.Services.AddIdentityCore<User>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;                      // sai 5 lần
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // khóa 15 phút
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddUserStore<UserStore>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

// Session Cookie xác thực: cookie đăng nhập do Identity cấp
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.Name = "SPTS.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // Always khi chạy HTTPS thật
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
});

// Kiểm tra SecurityStamp mỗi request: khóa tài khoản / đổi mật khẩu thì cookie cũ mất hiệu lực ngay
// ponytail: truy vấn DB mỗi request, tăng ValidationInterval nếu tải lớn
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.AddAuthorization();

// Session: hết hạn sau 30 phút không hoạt động
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

// Tạo dữ liệu mẫu (chỉ chạy khi DB chưa có dữ liệu)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await SeedData.InitializeAsync(db, hasher);
    await SeedData.SeedMoreAsync(db, hasher);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
