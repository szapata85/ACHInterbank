namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaParserService
{
    Task ParseAndSaveAsync(Stream nachaStream, string FileName);
}

