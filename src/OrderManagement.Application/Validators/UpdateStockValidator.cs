using FluentValidation;
using OrderManagement.Application.Products.Requests;

namespace OrderManagement.Application.Validators
{
    public class UpdateStockValidator : AbstractValidator<UpdateStockRequest>
    {
        public UpdateStockValidator()
        {
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        }
    }
}
