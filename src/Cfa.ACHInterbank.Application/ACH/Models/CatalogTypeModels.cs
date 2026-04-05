namespace Cfa.ACHInterbank.Application.ACH.Models;

public class CatalogTypeItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CatalogTypeUpsertRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public enum CatalogTypeKey
{
    DocumentTypes = 1,
    GenderTypes = 2,
    PersonTypes = 3,
    PhoneTypes = 4,
    EmailTypes = 5,
    AddressTypes = 6,
    TransactionCodes = 7
}
