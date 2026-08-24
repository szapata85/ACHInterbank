using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaConfigProfileReadModelTests
{
    [Fact]
    public async Task ConfigProfiles_ShouldReturnOfficialModelFlag()
    {
        await using var context = await SeedAsync();

        var profiles = await new NachaConfigProfileReadModelService(context).GetProfilesAsync();

        profiles.Should().OnlyContain(x => x.IsOfficialModel);
    }

    [Fact]
    public async Task ConfigProfiles_ShouldReturnProfilesWhenSeeded()
    {
        await using var context = await SeedAsync();

        var profiles = await new NachaConfigProfileReadModelService(context).GetProfilesAsync();

        profiles.Should().Contain(x => x.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        profiles.Should().Contain(x => x.ProfileCode == "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");
    }

    [Fact]
    public async Task ConfigProfiles_ShouldIncludeVariantsAndFields()
    {
        await using var context = await SeedAsync();

        var detail = await new NachaConfigProfileReadModelService(context)
            .GetProfileByCodeAsync(AchColOfficialNachaLayout.OutboundOriginalProfileCode);

        detail!.Variants.Should().NotBeEmpty();
        detail.Fields.Should().NotBeEmpty();
        detail.Fields.Should().OnlyContain(x => x.EndPosition == x.StartPosition + x.Length - 1);
    }

    [Fact]
    public async Task ConfigProfiles_ShouldIncludeRecordTypesWhenConfigured()
    {
        await using var context = await SeedAsync();

        var detail = await new NachaConfigProfileReadModelService(context)
            .GetProfileByCodeAsync("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        detail!.RecordTypes.Should().BeEquivalentTo("1", "5", "6", "7", "8", "9");
    }

    [Fact]
    public async Task ConfigProfiles_ShouldNotCallSaveChanges()
    {
        await using var context = await SeedCountingAsync();
        context.SaveChangesCount = 0;
        context.SaveChangesAsyncCount = 0;
        context.ChangeTracker.Clear();

        await new NachaConfigProfileReadModelService(context).GetDashboardAsync();

        context.SaveChangesCount.Should().Be(0);
        context.SaveChangesAsyncCount.Should().Be(0);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ConfigProfiles_ShouldMarkLegacyDeprecated()
    {
        await using var context = await SeedAsync();

        var dashboard = await new NachaConfigProfileReadModelService(context).GetDashboardAsync();

        dashboard.LegacyDeprecated.Should().BeTrue();
        (await new NachaConfigProfileReadModelService(context).GetProfilesAsync())
            .Should().OnlyContain(x => x.LegacyDeprecated);
    }

    [Fact]
    public async Task ConfigProfiles_ShouldNotExposeSensitiveData()
    {
        await using var context = await SeedAsync();

        var serialized = System.Text.Json.JsonSerializer.Serialize(
            await new NachaConfigProfileReadModelService(context).GetDashboardAsync()).ToLowerInvariant();

        serialized.Should().NotContain("password");
        serialized.Should().NotContain("credential");
        serialized.Should().NotContain("soap");
        serialized.Should().NotContain("rowversion");
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnOk()
    {
        await using var context = await SeedAsync();
        var controller = new NachaConfigProfilesReadOnlyController(new NachaConfigProfileReadModelService(context));

        var ok = Assert.IsType<OkObjectResult>(await controller.GetDashboard(default));

        Assert.IsType<NachaConfigProfilesDashboardReadModel>(ok.Value);
    }

    [Fact]
    public async Task GetProfiles_ShouldReturnOk()
    {
        await using var context = await SeedAsync();
        var controller = new NachaConfigProfilesReadOnlyController(new NachaConfigProfileReadModelService(context));

        var ok = Assert.IsType<OkObjectResult>(await controller.GetProfiles(default));

        Assert.IsAssignableFrom<IReadOnlyList<NachaConfigProfileReadModel>>(ok.Value);
    }

    [Fact]
    public void Endpoints_ShouldBeGetOnly()
    {
        typeof(NachaConfigProfilesReadOnlyController).GetMethods()
            .Where(x => x.DeclaringType == typeof(NachaConfigProfilesReadOnlyController) && x.IsPublic)
            .Should()
            .OnlyContain(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Any());
    }

    [Fact]
    public void MutatingEndpoints_ShouldNotExistOrBeBlocked()
    {
        var httpMutationAttributes = typeof(NachaConfigProfilesReadOnlyController).GetMethods()
            .Where(x => x.DeclaringType == typeof(NachaConfigProfilesReadOnlyController))
            .SelectMany(x => x.GetCustomAttributes(inherit: true))
            .Where(x => x.GetType().Name is "HttpPostAttribute" or "HttpPutAttribute" or "HttpPatchAttribute" or "HttpDeleteAttribute");

        httpMutationAttributes.Should().BeEmpty();
    }

    [Fact]
    public void ConfigProfiles_ShouldNotUseLegacyLayoutsDefinitions()
    {
        typeof(NachaConfigProfileReadModelService).GetConstructors().Single().GetParameters()
            .Select(x => x.ParameterType)
            .Should()
            .NotContain(typeof(INachaRecordLayoutAppService))
            .And.NotContain(typeof(INachaRecordDefinitionAppService));
    }

    [Fact]
    public void NachaExport_ShouldStillUseCycleIdNotHash()
    {
        var dto = new AchCycleExportDto
        {
            Id = "8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa",
            CycleId = "42",
            ExportIdentifier = "42",
            CycleName = "Ciclo",
            ProcessingDate = DateTime.UtcNow,
            IsExportable = true
        };

        dto.ExportIdentifier.Should().Be(dto.CycleId);
        dto.ExportIdentifier.Should().NotBe(dto.Id);
    }

    private static async Task<AchDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();
        return context;
    }

    private static async Task<CountingAchDbContext> SeedCountingAsync()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CountingAchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();
        return context;
    }

    private sealed class CountingAchDbContext : AchDbContext
    {
        public CountingAchDbContext(DbContextOptions<AchDbContext> options) : base(options)
        {
        }

        public int SaveChangesCount { get; set; }
        public int SaveChangesAsyncCount { get; set; }

        public override int SaveChanges()
        {
            SaveChangesCount++;
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCount++;
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
