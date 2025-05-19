using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class EntryDetailValidator : AbstractValidator<EntryDetail>
{
    public EntryDetailValidator()
    {
        RuleFor(x => x.TransactionCode).NotEmpty();
        RuleFor(x => x.DfiAccountNumber).NotEmpty().MaximumLength(17);
        RuleFor(x => x.IndividualName).MaximumLength(22);

        RuleForEach(x => x.AddendaRecords).SetValidator(new AddendaRecordValidator());
    }
}
