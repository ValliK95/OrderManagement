using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Application.Customers
{
    public interface ICustomerService
    {
        Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct);
        Task<CustomerResponse> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken ct);
    }
}
