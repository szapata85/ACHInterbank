using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ACHConfig;

public class CatClearingHouseConfiguration : IEntityTypeConfiguration<CatClearingHouse>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatClearingHouse> builder)
    {
        builder.ToTable("CatClearingHouse");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatClearingHouse { Id = 1, Code = "ACH", Name = "ACH Colombia", IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatDirectionConfiguration : IEntityTypeConfiguration<CatDirection>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatDirection> builder)
    {
        builder.ToTable("CatDirection");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatDirection { Id = 1, Code = "ENTRADA", NameEs = "Entrada", IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatDirection { Id = 2, Code = "SALIDA", NameEs = "Salida", IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatFlowTypeConfiguration : IEntityTypeConfiguration<CatFlowType>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatFlowType> builder)
    {
        builder.ToTable("CatFlowType");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne(x => x.DirectionDefault)
            .WithMany(x => x.FlowTypesDefault)
            .HasForeignKey(x => x.DirectionDefaultId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new CatFlowType { Id = 1, Code = "ORIGINAL", NameEs = "Original", DirectionDefaultId = 2, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 2, Code = "PRENOTIFICACION", NameEs = "Prenotificación", DirectionDefaultId = 2, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 3, Code = "DEVOLUCION", NameEs = "Devolución", DirectionDefaultId = 1, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 4, Code = "RETORNO", NameEs = "Retorno", DirectionDefaultId = 1, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 5, Code = "REVERSO", NameEs = "Reverso", DirectionDefaultId = 1, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 6, Code = "RECHAZO", NameEs = "Rechazo", DirectionDefaultId = 1, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatFlowType { Id = 7, Code = "OTRO", NameEs = "Otro", DirectionDefaultId = null, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatServiceClassConfiguration : IEntityTypeConfiguration<CatServiceClass>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatServiceClass> builder)
    {
        builder.ToTable("CatServiceClass");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.Code, x.ClearingHouseId }).IsUnique();

        builder.HasOne(x => x.ClearingHouse)
            .WithMany(x => x.ServiceClasses)
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new CatServiceClass { Id = 1, Code = "PPD", NameEs = "Pago prearreglado y depósito", ClearingHouseId = null, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatServiceClass { Id = 2, Code = "CCD", NameEs = "Concentración y desembolso corporativo", ClearingHouseId = null, IsActive = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatRecordCodeConfiguration : IEntityTypeConfiguration<CatRecordCode>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatRecordCode> builder)
    {
        builder.ToTable("CatRecordCode");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(5).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatRecordCode { Id = 1, Code = "1", NameEs = "Encabezado de archivo", IsMandatoryBase = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRecordCode { Id = 2, Code = "5", NameEs = "Encabezado de lote", IsMandatoryBase = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRecordCode { Id = 3, Code = "6", NameEs = "Detalle de transacción", IsMandatoryBase = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRecordCode { Id = 4, Code = "7", NameEs = "Addenda", IsMandatoryBase = false, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRecordCode { Id = 5, Code = "8", NameEs = "Control de lote", IsMandatoryBase = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRecordCode { Id = 6, Code = "9", NameEs = "Control de archivo", IsMandatoryBase = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatConfigStatusConfiguration : IEntityTypeConfiguration<CatConfigStatus>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatConfigStatus> builder)
    {
        builder.ToTable("CatConfigStatus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatConfigStatus { Id = 1, Code = "BORRADOR", IsEditable = true, IsPublishable = true, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatConfigStatus { Id = 2, Code = "PUBLICADO", IsEditable = false, IsPublishable = false, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatConfigStatus { Id = 3, Code = "INACTIVO", IsEditable = false, IsPublishable = false, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatConfigStatus { Id = 4, Code = "ARCHIVADO", IsEditable = false, IsPublishable = false, CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatDataSourceTypeConfiguration : IEntityTypeConfiguration<CatDataSourceType>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatDataSourceType> builder)
    {
        builder.ToTable("CatDataSourceType");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatDataSourceType { Id = 1, Code = "CONSTANTE", NameEs = "Valor constante", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatDataSourceType { Id = 2, Code = "ENTIDAD", NameEs = "Entidad del dominio", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatDataSourceType { Id = 3, Code = "SQL_VIEW", NameEs = "Vista SQL", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatDataSourceType { Id = 4, Code = "SQL_PROCEDURE", NameEs = "Procedimiento SQL", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatDataSourceType { Id = 5, Code = "EXPRESION", NameEs = "Expresión declarativa", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}

public class CatRuleTypeConfiguration : IEntityTypeConfiguration<CatRuleType>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<CatRuleType> builder)
    {
        builder.ToTable("CatRuleType");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new CatRuleType { Id = 1, Code = "REQUIRED", NameEs = "Obligatorio", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 2, Code = "REGEX", NameEs = "Expresión regular", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 3, Code = "RANGE", NameEs = "Rango", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 4, Code = "ENUM", NameEs = "Lista de valores", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 5, Code = "DATE_FORMAT", NameEs = "Formato de fecha", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 6, Code = "CHECKSUM", NameEs = "Dígito de chequeo", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 7, Code = "CONDITIONAL", NameEs = "Condicional", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp },
            new CatRuleType { Id = 8, Code = "CROSS_FIELD", NameEs = "Validación entre campos", CreatedAt = SeedTimestamp, UpdatedAt = SeedTimestamp });
    }
}
