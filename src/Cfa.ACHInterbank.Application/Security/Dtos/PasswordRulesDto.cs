namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record PasswordRulesDto
{
    public int MinLength { get; init; }
    public int MinUppercase { get; init; }
    public int MinNumbers { get; init; }
    public int MinSpecial { get; init; }
    public int? MaxSpecial { get; init; }
}
