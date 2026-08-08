using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchReturnsControllerConflictTests
{
    [Fact]
    public async Task GenerateFile_WhenDatabaseClaimWasLost_ReturnsCanonicalConflict()
    {
        var service = new Mock<IAchReturnsService>(MockBehavior.Strict);
        var request = new GenerateReturnsFileRequest(
            "ACH-RETURN-CONFLICT",
            [new ReturnSelectionItemDto(701, "R01")]);
        service.Setup(x => x.GenerateReturnsFileAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AchReturnAlreadyGeneratedException([701]));

        var result = await new AchReturnsController(service.Object)
            .GenerateFile(request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(AchReturnAlreadyGeneratedException.ErrorCode, problem.Extensions["errorCode"]);
        Assert.Equal(new[] { 701 }, Assert.IsAssignableFrom<IReadOnlyList<int>>(problem.Extensions["transactionIds"]));
    }
}
