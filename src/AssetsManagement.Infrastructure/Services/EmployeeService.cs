using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class EmployeeService(AssetsDbContext db, ICurrentUser currentUser) : IEmployeeService
{
    public async Task<PagedResult<EmployeeListItemDto>> ListAsync(
        EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var source = db.Employees.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.Department))
            source = source.Where(x => x.Department == query.Department.Trim());
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.Department != null && x.Department.Contains(search)));
        }
        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("code", "desc") => source.OrderByDescending(x => x.Code),
            ("code", _) => source.OrderBy(x => x.Code),
            ("department", "desc") => source.OrderByDescending(x => x.Department),
            ("department", _) => source.OrderBy(x => x.Department),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new EmployeeListItemDto(x.Id, x.Name, x.Code, x.Department, x.IsActive))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<EmployeeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<EmployeeDetailDto> CreateAsync(EmployeeRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueAsync(request.Code, request.Name, null, cancellationToken);
        var entity = new Employee();
        Apply(entity, request);
        db.Employees.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EmployeeDetailDto> UpdateAsync(
        Guid id, EmployeeRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueAsync(request.Code, request.Name, id, cancellationToken);
        Apply(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive) return;
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Name.Contains(term) || x.Code.Contains(term) ||
                (x.Department != null && x.Department.Contains(term)));
        }
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    private async Task<Employee> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Employees.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Employee was not found.");

    private async Task EnsureUniqueAsync(string? code, string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();
        if (await db.Employees.AnyAsync(x => x.Name == trimmedName && (!excludingId.HasValue || x.Id != excludingId),
                cancellationToken))
            throw new ConflictException("Employee name is already in use.");
        if (string.IsNullOrWhiteSpace(code)) return;
        var trimmed = code.Trim();
        if (await db.Employees.AnyAsync(x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId),
                cancellationToken))
            throw new ConflictException("Employee code is already in use.");
    }

    private static void Apply(Employee entity, EmployeeRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? "" : request.Code.Trim();
        entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        entity.Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
        entity.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        entity.IsActive = request.Active != false;
    }

    private static EmployeeDetailDto Map(Employee entity) =>
        new(entity.Id, entity.Name, entity.Code, entity.Email, entity.Department, entity.JobTitle, entity.IsActive);
}
