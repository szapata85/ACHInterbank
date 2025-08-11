using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class BatchControlValidator : AbstractValidator<BatchControl>
{
    public BatchControlValidator()
    {
        RuleFor(x => x.IdUserOrig).NotEmpty().Length(10);
        RuleFor(x => x.CodAutMessage).NotEmpty().Length(8);
    }
}
