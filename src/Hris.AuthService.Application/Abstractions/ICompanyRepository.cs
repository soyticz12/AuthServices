using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface ICompanyRepository
{
    Task<Company?> FindByCodeAsync(string companyCode, CancellationToken ct);
}
