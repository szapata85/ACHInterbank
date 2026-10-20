using System;
using Xunit;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionCapabilitiesTests
{
    [Theory]
    [InlineData(IntegrationContractVersionStatusEnum.Draft, false, true)]
    [InlineData(IntegrationContractVersionStatusEnum.Validated, false, true)]
    [InlineData(IntegrationContractVersionStatusEnum.Active, true, false)]
    [InlineData(IntegrationContractVersionStatusEnum.Retired, true, false)]
    public void Capabilities_AreDerivedByBackendFromContractMutability(
        IntegrationContractVersionStatusEnum status,
        bool expectedReadOnly,
        bool expectedCanEditFunctionalMetadata)
    {
        var dto = new IntegrationContractVersionDto(
            Guid.NewGuid(),
            1,
            "v1",
            status,
            "Proc_Transacciones",
            "urn:test",
            "urn:test/action",
            IntegrationContractSourceTypeEnum.ManualApproved,
            null,
            null,
            null,
            null,
            3,
            DateTime.UtcNow,
            "operator",
            null,
            null);

        Assert.Equal(expectedReadOnly, dto.Capabilities.IsReadOnly);
        Assert.Equal(expectedCanEditFunctionalMetadata, dto.Capabilities.CanEditFunctionalMetadata);
    }
}
