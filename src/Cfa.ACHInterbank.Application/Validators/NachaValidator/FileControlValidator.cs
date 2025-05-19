using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class FileControlValidator : AbstractValidator<FileControl>
{
    public FileControlValidator()
    {
        RuleFor(x => x.NachaHeaderId).GreaterThan(0);
    }
}
