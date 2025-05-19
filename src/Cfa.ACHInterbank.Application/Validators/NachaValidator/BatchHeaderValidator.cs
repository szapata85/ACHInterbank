using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class BatchHeaderValidator : AbstractValidator<BatchHeader>
{
    public BatchHeaderValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().MaximumLength(16);

        RuleFor(x => x.CompanyId)
            .NotEmpty().Length(10);

        RuleFor(x => x.StandardEntryClassCode)
            .NotEmpty().Length(3);

        RuleForEach(x => x.Entries).SetValidator(new EntryDetailValidator());
        RuleFor(x => x.BatchControl).SetValidator(new BatchControlValidator());
    }
}
