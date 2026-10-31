using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SPTS.Data;
using SPTS.Models;
using SPTS.Models.ViewModels;

namespace SPTS.Repositories;

// Repository chứa truy vấn EF Core; Service quyết định nghiệp vụ và giao dịch.
public class CatalogRepository(ApplicationDbContext db)
{
    public IQueryable<CatalogRowViewModel> FacultyRows() => db.Faculties.AsNoTracking()
        .Select(f => new CatalogRowViewModel {
            Id = f.FacultyId, Code = f.FacultyCode, Name = f.FacultyName,
            MajorCount = f.Majors.Count, StaffCount = f.Staffs.Count
        });

    public IQueryable<CatalogRowViewModel> MajorRows() => db.Majors.AsNoTracking()
        .Select(m => new CatalogRowViewModel {
            Id = m.MajorId, Code = m.MajorCode, Name = m.MajorName,
            FacultyId = m.FacultyId, FacultyName = m.Faculty.FacultyName,
            ProgramCount = m.Programs.Count
        });

    public Task<Faculty?> FindFacultyAsync(int id) => db.Faculties.FirstOrDefaultAsync(x => x.FacultyId == id);
    public Task<Major?> FindMajorAsync(int id) => db.Majors.FirstOrDefaultAsync(x => x.MajorId == id);
    public Task<bool> FacultyExistsAsync(int id) => db.Faculties.AnyAsync(x => x.FacultyId == id);
    public Task<bool> FacultyCodeExistsAsync(string code, int? id) =>
        db.Faculties.AnyAsync(x => x.FacultyCode == code && (!id.HasValue || x.FacultyId != id));
    public Task<bool> MajorCodeExistsAsync(string code, int? id) =>
        db.Majors.AnyAsync(x => x.MajorCode == code && (!id.HasValue || x.MajorId != id));
    public async Task<bool> FacultyReferencedAsync(int id) =>
        await db.Majors.AnyAsync(x => x.FacultyId == id) || await db.Staffs.AnyAsync(x => x.FacultyId == id);
    public Task<bool> MajorReferencedAsync(int id) => db.Programs.AnyAsync(x => x.MajorId == id);
    public void Add(Faculty faculty) => db.Faculties.Add(faculty);
    public void Add(Major major) => db.Majors.Add(major);
    public void Remove(Faculty faculty) => db.Faculties.Remove(faculty);
    public void Remove(Major major) => db.Majors.Remove(major);
    public Task<int> SaveAsync() => db.SaveChangesAsync();
    public Task<IDbContextTransaction> BeginTransactionAsync() => db.Database.BeginTransactionAsync();
    public void ClearTracking() => db.ChangeTracker.Clear();
}
