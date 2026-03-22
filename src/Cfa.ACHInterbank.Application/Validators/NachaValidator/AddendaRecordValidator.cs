using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentValidation;

namespace Cfa.ACHInterbank.Application.Validators.NachaValidator;

public class AddendaRecordValidator : AbstractValidator<AddendaRecord>
{
    public AddendaRecordValidator()
    {
        RuleFor(x => x.AddendumSequence)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Length == 4);

        RuleFor(x => x.EntryDetailSequenceNumber)
            .NotEmpty()
            .Length(7);
    }
}
