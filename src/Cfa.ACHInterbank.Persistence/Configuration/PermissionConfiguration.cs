using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public static readonly Guid ManageAchPermissionId = Guid.Parse("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a");
    public static readonly Guid ReadAchPermissionId = Guid.Parse("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7");
    public static readonly Guid ReadAliasesPermissionId = Guid.Parse("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7");
    public static readonly Guid ReadCatalogsPermissionId = Guid.Parse("dd0e54be-b6df-4ab3-8783-0f72b6e774a2");
    public static readonly Guid ManageUsersPermissionId = Guid.Parse("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf");
    public static readonly Guid ManageCertificatesPermissionId = Guid.Parse("13d4e160-8be4-43eb-b69b-c9c658c2dc74");
    public static readonly Guid SchedulerViewPermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd01");
    public static readonly Guid SchedulerHistoryViewPermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd02");
    public static readonly Guid SchedulerExecutePermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd03");
    public static readonly Guid SchedulerManageSchedulePermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd04");
    public static readonly Guid SchedulerPauseResumePermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd05");
    public static readonly Guid SchedulerViewInstancesPermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd06");
    public static readonly Guid SchedulerViewTechnicalPermissionId = Guid.Parse("d1445236-b093-4d6f-8b09-821599d4dd07");
    public static readonly Guid ClearingHousesViewPermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000001");
    public static readonly Guid ClearingHousesCreatePermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000002");
    public static readonly Guid ClearingHousesUpdatePermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000003");
    public static readonly Guid ClearingHousesChangeStatusPermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000004");
    public static readonly Guid ClearingHousesManageCyclesPermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000005");
    public static readonly Guid ClearingHousesManageSpecialDatesPermissionId = Guid.Parse("c1ea0001-5b98-4d95-a100-000000000006");

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(250);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Permission
            {
                Id = ManageAchPermissionId,
                Name = "CanManageAch",
                Description = "Gestión completa de operaciones ACH"
            },
            new Permission
            {
                Id = ReadAchPermissionId,
                Name = "CanReadAch",
                Description = "Consulta de operaciones ACH"
            },
            new Permission
            {
                Id = ReadAliasesPermissionId,
                Name = "CanReadAliases",
                Description = "Consulta de alias"
            },
            new Permission
            {
                Id = ReadCatalogsPermissionId,
                Name = "CanReadCatalogs",
                Description = "Consulta de catálogos"
            },
            new Permission
            {
                Id = ManageUsersPermissionId,
                Name = "CanManageUsers",
                Description = "Gestión de usuarios"
            },
            new Permission
            {
                Id = ManageCertificatesPermissionId,
                Name = "CanManageCertificates",
                Description = "Administración de certificados digitales"
            },
            new Permission { Id = SchedulerViewPermissionId, Name = "Scheduler.View", Description = "Consultar tareas programadas" },
            new Permission { Id = SchedulerHistoryViewPermissionId, Name = "Scheduler.History.View", Description = "Consultar historial del programador" },
            new Permission { Id = SchedulerExecutePermissionId, Name = "Scheduler.Execute", Description = "Ejecutar manualmente tareas autorizadas" },
            new Permission { Id = SchedulerManageSchedulePermissionId, Name = "Scheduler.ManageSchedule", Description = "Editar programaciones" },
            new Permission { Id = ClearingHousesViewPermissionId, Name = "ClearingHouses.View", Description = "Consultar cámaras compensadoras" },
            new Permission { Id = ClearingHousesCreatePermissionId, Name = "ClearingHouses.Create", Description = "Crear cámaras compensadoras" },
            new Permission { Id = ClearingHousesUpdatePermissionId, Name = "ClearingHouses.Update", Description = "Editar cámaras compensadoras" },
            new Permission { Id = ClearingHousesChangeStatusPermissionId, Name = "ClearingHouses.ChangeStatus", Description = "Activar o desactivar cámaras compensadoras" },
            new Permission { Id = ClearingHousesManageCyclesPermissionId, Name = "ClearingHouses.ManageCycles", Description = "Administrar ciclos por cámara" },
            new Permission { Id = ClearingHousesManageSpecialDatesPermissionId, Name = "ClearingHouses.ManageSpecialDates", Description = "Administrar fechas especiales por cámara" },
            new Permission { Id = SchedulerPauseResumePermissionId, Name = "Scheduler.PauseResume", Description = "Pausar y reanudar tareas" },
            new Permission { Id = SchedulerViewInstancesPermissionId, Name = "Scheduler.ViewInstances", Description = "Consultar instancias del clúster" },
            new Permission { Id = SchedulerViewTechnicalPermissionId, Name = "Scheduler.Technical.View", Description = "Consultar información técnica de tareas programadas" });
    }
}
