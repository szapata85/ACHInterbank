using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class BatchControlValidator : AbstractValidator<BatchControl>
{
    public BatchControlValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().Length(10);
        RuleFor(x => x.OdfiIdentification).NotEmpty().Length(8);
    }
}
