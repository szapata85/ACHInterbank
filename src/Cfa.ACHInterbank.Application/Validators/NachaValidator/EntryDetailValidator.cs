using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class EntryDetailValidator : AbstractValidator<EntryDetail>
{
    public EntryDetailValidator()
    {
        RuleFor(x => x.TransactionCode).NotEmpty();
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(17);
        RuleFor(x => x.RecipUserName).MaximumLength(22);

        RuleForEach(x => x.AddendaRecords).SetValidator(new AddendaRecordValidator());
    }
}
