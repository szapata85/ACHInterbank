using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Moq;

namespace Cfa.ACHInterbank.Tests;

internal static class ReturnOutExternalFileNamePolicyFactory
{
    public static IExternalFileNamePolicy Create(string externalFileName = "0101006.001.RET")
    {
        var policy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);
        policy.Setup(x => x.GenerateExternalNameAsync(
                It.Is<ExternalFileNameContext>(c =>
                    c.ExternalFileType == ExternalFileType.ReturnOut
                    && c.Direction == ExternalFileDirection.Outbound),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = externalFileName,
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Warning,
                    Issues = []
                }
            });

        return policy.Object;
    }
}
