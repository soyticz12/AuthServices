using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Infrastructure.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AuthDbContext _db;
    public CompanyRepository(AuthDbContext db) => _db = db;

    public Task<Company?> FindByCodeAsync(string companyCode, CancellationToken ct) =>
        _db.Companies.FirstOrDefaultAsync(c => c.Code == companyCode, ct);
}
