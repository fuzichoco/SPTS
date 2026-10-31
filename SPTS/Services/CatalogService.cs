using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SPTS.Models;
using SPTS.Models.ViewModels;
using SPTS.Repositories;

namespace SPTS.Services;

public class CatalogService(CatalogRepository repository, AuditLogService audit, IMemoryCache cache)
{
    private const string FacultyOptionsKey = "Catalog.FacultyOptions";

    public async Task<List<SelectListItem>> FacultyOptionsAsync()
    {
        var rows = await cache.GetOrCreateAsync(FacultyOptionsKey, async entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
            return await repository.FacultyRows().OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
        });
        // Không chia sẻ SelectListItem có trạng thái Selected giữa các request.
        return rows!.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    public async Task<CatalogFilterViewModel> ListAsync(CatalogFilterViewModel filter, bool majors)
    {
        filter.PageSize = Math.Clamp(filter.PageSize, 5, 50);
        filter.Keyword = filter.Keyword?.Trim();
        var query = majors ? repository.MajorRows() : repository.FacultyRows();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(x => x.Code.Contains(filter.Keyword) || x.Name.Contains(filter.Keyword));
        if (majors && filter.FacultyId.HasValue)
            query = query.Where(x => x.FacultyId == filter.FacultyId);
        query = filter.SortOrder switch {
            "code_desc" => query.OrderByDescending(x => x.Code).ThenBy(x => x.Id),
            "name" => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            "name_desc" => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Code).ThenBy(x => x.Id)
        };
        int total = await query.CountAsync();
        filter.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)filter.PageSize)));
        filter.Result = new PagedResult<CatalogRowViewModel> {
            TotalItems = total, Page = filter.Page, PageSize = filter.PageSize,
            Items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync()
        };
        if (majors) filter.Faculties = await FacultyOptionsAsync();
        return filter;
    }

    public Task<CatalogRowViewModel?> FacultyDetailAsync(int id) => repository.FacultyRows().FirstOrDefaultAsync(x => x.Id == id);
    public Task<CatalogRowViewModel?> MajorDetailAsync(int id) => repository.MajorRows().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CatalogResult> SaveFacultyAsync(int? id, FacultyEditModel input)
    {
        var entity = id.HasValue ? await repository.FindFacultyAsync(id.Value) : new Faculty();
        if (entity == null) return new(false, NotFound: true);
        string code = input.FacultyCode.Trim(), name = input.FacultyName.Trim();
        if (await repository.FacultyCodeExistsAsync(code, id))
            return new(false, "Mã khoa đã tồn tại", nameof(input.FacultyCode));
        var old = id.HasValue ? new { entity.FacultyCode, entity.FacultyName } : null;
        entity.FacultyCode = code;
        entity.FacultyName = name;
        if (!id.HasValue) repository.Add(entity);
        return await CommitAsync(id.HasValue ? "UPDATE" : "CREATE", "Faculties", () => entity.FacultyId,
            old, new { entity.FacultyCode, entity.FacultyName }, nameof(input.FacultyCode));
    }

    public async Task<CatalogResult> SaveMajorAsync(int? id, MajorEditModel input)
    {
        var entity = id.HasValue ? await repository.FindMajorAsync(id.Value) : new Major();
        if (entity == null) return new(false, NotFound: true);
        string code = input.MajorCode.Trim(), name = input.MajorName.Trim();
        if (await repository.MajorCodeExistsAsync(code, id))
            return new(false, "Mã ngành đã tồn tại", nameof(input.MajorCode));
        if (!await repository.FacultyExistsAsync(input.FacultyId))
            return new(false, "Khoa không tồn tại hoặc đã bị xóa", nameof(input.FacultyId));
        var old = id.HasValue ? new { entity.MajorCode, entity.MajorName, entity.FacultyId } : null;
        entity.MajorCode = code;
        entity.MajorName = name;
        entity.FacultyId = input.FacultyId;
        if (!id.HasValue) repository.Add(entity);
        return await CommitAsync(id.HasValue ? "UPDATE" : "CREATE", "Majors", () => entity.MajorId,
            old, new { entity.MajorCode, entity.MajorName, entity.FacultyId }, nameof(input.MajorCode));
    }

    public async Task<CatalogResult> DeleteFacultyAsync(int id)
    {
        var entity = await repository.FindFacultyAsync(id);
        if (entity == null) return new(false, NotFound: true);
        if (await repository.FacultyReferencedAsync(id))
            return new(false, "Không thể xóa khoa đang có ngành hoặc cán bộ tham chiếu.");
        repository.Remove(entity);
        return await CommitAsync("DELETE", "Faculties", () => id,
            new { entity.FacultyCode, entity.FacultyName }, null);
    }

    public async Task<CatalogResult> DeleteMajorAsync(int id)
    {
        var entity = await repository.FindMajorAsync(id);
        if (entity == null) return new(false, NotFound: true);
        if (await repository.MajorReferencedAsync(id))
            return new(false, "Không thể xóa ngành đang có chương trình đào tạo tham chiếu.");
        repository.Remove(entity);
        return await CommitAsync("DELETE", "Majors", () => id,
            new { entity.MajorCode, entity.MajorName, entity.FacultyId }, null);
    }

    private async Task<CatalogResult> CommitAsync(string action, string table, Func<int> id,
        object? old, object? value, string codeField = "")
    {
        // Lưu dữ liệu và nhật ký trong cùng giao dịch; chỉ xóa cache sau commit.
        await using var transaction = await repository.BeginTransactionAsync();
        try
        {
            await repository.SaveAsync();
            await audit.LogAsync(action, table, id(), old, value);
            await transaction.CommitAsync();
            cache.Remove(FacultyOptionsKey);
            return new(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            repository.ClearTracking();
            return new(false, "Dữ liệu đã bị thay đổi hoặc xóa. Hãy tải lại trang.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627 or 547)
        {
            await transaction.RollbackAsync();
            repository.ClearTracking();
            return sql.Number is 2601 or 2627
                ? new(false, "Mã đã tồn tại. Vui lòng chọn mã khác.", codeField)
                : new(false, "Dữ liệu liên quan đã thay đổi hoặc bản ghi đang được tham chiếu. Hãy tải lại trang.");
        }
    }
}
