using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Application.Customers
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
