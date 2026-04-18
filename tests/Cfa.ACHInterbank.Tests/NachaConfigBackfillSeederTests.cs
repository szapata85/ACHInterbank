using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public class NachaConfigBackfillSeederTests
{
    [Fact]
    public async Task SeedAsync_Should_Create_Default_Profile_And_Backfill_From_Legacy()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.NachaRecordDefinitions.Add(new NachaRecordDefinition
        {
            RecordCode = "1",
            Sequence = 10,
            SourceType = NachaRecordSourceType.Custom,
            IsEnabled = true
        });

        context.NachaRecordLayouts.Add(new NachaRecordLayout
        {
            RecordType = "FILE_HEADER",
            RecordCode = "1",
            TotalLength = 106,
            Description = "Layout legado",
            Fields =
            [
                new NachaRecordField
                {
                    FieldName = "PriorityCode",
                    StartPosition = 2,
                    Length = 2,
                    PadChar = '0',
                    Justification = 'R',
                    DbColumn = "CONST:01"
                }
            ]
        });

        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Term = "NOMINAS",
            Description = "Nóminas",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });

        await context.SaveChangesAsync();

        var seeder = new NachaConfigBackfillSeeder(context);
        await seeder.SeedAsync();

        var profile = await context.CfgProfiles
            .Include(x => x.Records)
            .FirstOrDefaultAsync();

        Assert.NotNull(profile);
        Assert.Equal("LEGACY_ACH_SALIDA_ORIGINAL_V1_0", profile!.ProfileCode);
        Assert.Single(profile.Records);
        Assert.True(await context.CfgLayoutVariants.AnyAsync());
        Assert.True(await context.CfgLayoutFields.AnyAsync());
        Assert.True(await context.HistConfigSnapshots.AnyAsync());
        Assert.True(await context.HistConfigChanges.AnyAsync());
    }

    [Fact]
    public async Task EnsureCreated_Should_Seed_New_Config_Catalogs()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var statuses = await context.CatConfigStatuses
            .OrderBy(x => x.Id)
            .Select(x => x.Code)
            .ToListAsync();

        Assert.Contains("BORRADOR", statuses);
        Assert.Contains("PUBLICADO", statuses);
        Assert.Contains("INACTIVO", statuses);
        Assert.Contains("ARCHIVADO", statuses);

        var recordCodes = await context.CatRecordCodes.Select(x => x.Code).ToListAsync();
        Assert.Contains("1", recordCodes);
        Assert.Contains("5", recordCodes);
        Assert.Contains("6", recordCodes);
        Assert.Contains("7", recordCodes);
        Assert.Contains("8", recordCodes);
        Assert.Contains("9", recordCodes);
    }

    [Fact]
    public async Task Model_Should_Define_Unique_Indexes_And_Check_Constraints_For_Core_Config()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var profileEntity = context.Model.FindEntityType(typeof(CfgProfile));
        Assert.NotNull(profileEntity);
        Assert.Contains(profileEntity!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(CfgProfile.ClearingHouseId),
                nameof(CfgProfile.FlowTypeId),
                nameof(CfgProfile.DirectionId),
                nameof(CfgProfile.ServiceClassId),
                nameof(CfgProfile.VersionMajor),
                nameof(CfgProfile.VersionMinor)
            }));

        Assert.Contains(profileEntity.GetCheckConstraints(), constraint => constraint.Name == "CK_CfgProfile_EffectiveRange");

        var layoutFieldEntity = context.Model.FindEntityType(typeof(CfgLayoutField));
        Assert.NotNull(layoutFieldEntity);
        Assert.Contains(layoutFieldEntity!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(CfgLayoutField.LayoutVariantId),
                nameof(CfgLayoutField.StartPosition)
            }));
        Assert.Contains(layoutFieldEntity.GetCheckConstraints(), constraint => constraint.Name == "CK_CfgLayoutField_Justification_Valid");
    }
}
