using FluentValidation;
using OrderManagement.Application.Orders.Requests;

namespace OrderManagement.Application.Validators
{
    public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusValidator()
        {
            RuleFor(x => x.Status).IsInEnum();
        }
    }
}
