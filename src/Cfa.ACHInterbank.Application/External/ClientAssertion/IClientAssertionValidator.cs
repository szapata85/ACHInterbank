namespace Cfa.ACHInterbank.Application.External.ClientAssertion;

public interface IClientAssertionValidator
{
    bool ValidateAssertionAsync(string assertion);
}
