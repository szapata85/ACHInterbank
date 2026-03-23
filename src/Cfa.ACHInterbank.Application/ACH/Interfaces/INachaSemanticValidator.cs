using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSemanticValidator
{
    void Validate(string fileContent, NachaBuildContext context);
}
