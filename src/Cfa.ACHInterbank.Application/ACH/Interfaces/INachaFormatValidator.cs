namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFormatValidator
{
    void ValidateOrThrow(string nachaContent, NachaValidationContext context);
}
