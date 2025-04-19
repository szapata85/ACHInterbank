namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaParserServiceScoped
{
    Task ParseAndSaveAsync(Stream nachaStream);
}

