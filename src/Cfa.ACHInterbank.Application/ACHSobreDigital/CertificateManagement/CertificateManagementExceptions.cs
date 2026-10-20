namespace Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

public sealed class CertificateValidationException : Exception
{
    public CertificateValidationException(string message) : base(message)
    {
    }
}

public sealed class CertificateConflictException : Exception
{
    public CertificateConflictException(string message) : base(message)
    {
    }
}
