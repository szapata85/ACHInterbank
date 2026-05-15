using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRecordFieldValidator
{
    NachaRecordValidationResult Validate(NachaRecordValidationContext context);
}
