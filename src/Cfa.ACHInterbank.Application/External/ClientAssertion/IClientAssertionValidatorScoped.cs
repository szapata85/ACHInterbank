namespace Cfa.ACHInterbank.Application.External.ClientAssertion;

public interface IClientAssertionValidatorScoped
{
    bool ValidateAssertionAsync(string assertion);
}
