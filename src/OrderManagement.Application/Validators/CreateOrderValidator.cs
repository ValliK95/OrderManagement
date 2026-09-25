using FluentValidation;
using OrderManagement.Application.Orders.Requests;

namespace OrderManagement.Application.Validators
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0);

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("An order must contain at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).GreaterThan(0);
                item.RuleFor(i => i.Quantity).GreaterThan(0);
            });
        }
    }
}
