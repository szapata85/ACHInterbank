using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBulkFileStructuralValidator
{
    StructuralValidationOutcome Validate(ParsedRawItem item);
}
