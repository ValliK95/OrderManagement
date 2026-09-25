using FluentValidation;
using OrderManagement.Application.Customers.Requests;

namespace OrderManagement.Application.Validators
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        }
    }
}
