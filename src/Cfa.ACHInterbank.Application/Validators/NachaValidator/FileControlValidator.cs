using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class FileControlValidator : AbstractValidator<FileControl>
{
    public FileControlValidator()
    {
        RuleFor(x => x.FileControlID).GreaterThan(0);
        RuleFor(x => x.BlockCount).GreaterThan(0);
        RuleFor(x => x.EntryAddendaCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reserved).NotNull().Length(39);
    }
}
