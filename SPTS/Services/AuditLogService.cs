using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using SPTS.Data;
using SPTS.Models;

namespace SPTS.Services
{
    // Ghi nhật ký hệ thống, dùng chung cho mọi module
    public class AuditLogService
    {
        private static readonly JsonSerializerOptions JsonOpt = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AuditLogService(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task LogAsync(string action, string? tableName, object? recordId, object? oldValue = null, object? newValue = null, int? userId = null)
        {
            var ctx = _http.HttpContext;
            userId ??= int.TryParse(ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                TableName = tableName,
                RecordId = recordId?.ToString(),
                OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue, JsonOpt),
                NewValue = newValue == null ? null : JsonSerializer.Serialize(newValue, JsonOpt),
                IpAddress = ctx?.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }
}
