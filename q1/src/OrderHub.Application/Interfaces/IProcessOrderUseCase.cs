using OrderHub.Core.Models;

namespace OrderHub.Application.Interfaces
{
    public interface IProcessOrderUseCase
    {
        Task<OrderResult> ExecuteAsync(int schoolId, List<OrderLine> lines, string parentEmail, CancellationToken ct = default);
    }
}
