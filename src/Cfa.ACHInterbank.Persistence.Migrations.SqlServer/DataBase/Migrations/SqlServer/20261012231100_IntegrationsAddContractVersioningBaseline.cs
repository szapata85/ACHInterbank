using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class IntegrationsAddContractVersioningBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "FunctionalCategory", table: "IntegrationMethods", type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "FunctionalDescription", table: "IntegrationMethods", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "RiskLevel", table: "IntegrationMethods", type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<Guid>(name: "ContractVersionId", table: "IntegrationMethodParameters", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "MaxOccurs", table: "IntegrationMethodParameters", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "MinOccurs", table: "IntegrationMethodParameters", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(name: "Nillable", table: "IntegrationMethodParameters", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "SupportStatus", table: "IntegrationMethodParameters", type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "Supported");
            migrationBuilder.AddColumn<string>(name: "XmlElementName", table: "IntegrationMethodParameters", type: "nvarchar(250)", maxLength: 250, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "XsdType", table: "IntegrationMethodParameters", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "string");

            migrationBuilder.AddColumn<Guid>(name: "ContractVersionId", table: "IntegrationMappingSets", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<long>(name: "Revision", table: "IntegrationMappingSets", type: "bigint", nullable: false, defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "IntegrationContractVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    VersionLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SoapAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceDocumentName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceDocumentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationContractVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationContractVersions_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_IntegrationContractVersions_SourceDocumentHash", table: "IntegrationContractVersions", column: "SourceDocumentHash");
            migrationBuilder.CreateIndex(name: "IX_IntegrationContractVersions_Method_Status", table: "IntegrationContractVersions", columns: new[] { "MethodId", "Status" });
            migrationBuilder.CreateIndex(name: "UX_IntegrationContractVersions_Method_VersionLabel", table: "IntegrationContractVersions", columns: new[] { "MethodId", "VersionLabel" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath", table: "IntegrationMethodParameters", columns: new[] { "ContractVersionId", "ParameterPath" });
            migrationBuilder.CreateIndex(name: "IX_IntegrationMappingSets_ContractVersionId", table: "IntegrationMappingSets", column: "ContractVersionId");
            migrationBuilder.CreateIndex(name: "IX_IntegrationMappingSets_MethodId_ContractVersionId_Version", table: "IntegrationMappingSets", columns: new[] { "MethodId", "ContractVersionId", "Version" });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [IntegrationMethods] WHERE [Code] = N'WSCFAACH.Proc_Contrapartidas')
                BEGIN
                    INSERT INTO [IntegrationMethods]
                        ([Code], [DisplayName], [FunctionalDescription], [FunctionalCategory], [RiskLevel], [SoapClientCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES
                        (N'WSCFAACH.Proc_Contrapartidas', N'Proc_Contrapartidas', N'', N'', N'', N'WscfaachSoapClient', 1,
                         '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00');
                END;

                IF NOT EXISTS (SELECT 1 FROM [IntegrationMethods] WHERE [Code] = N'WSCFAACH.Proc_Transacciones')
                BEGIN
                    INSERT INTO [IntegrationMethods]
                        ([Code], [DisplayName], [FunctionalDescription], [FunctionalCategory], [RiskLevel], [SoapClientCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES
                        (N'WSCFAACH.Proc_Transacciones', N'Proc_Transacciones', N'', N'', N'', N'WscfaachSoapClient', 1,
                         '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00');
                END;

                IF NOT EXISTS (SELECT 1 FROM [IntegrationMethods] WHERE [Code] = N'WSAXON.RegistrarRespuestaTransaccion')
                BEGIN
                    INSERT INTO [IntegrationMethods]
                        ([Code], [DisplayName], [FunctionalDescription], [FunctionalCategory], [RiskLevel], [SoapClientCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES
                        (N'WSAXON.RegistrarRespuestaTransaccion', N'RegistrarRespuestaTransaccion', N'', N'', N'', N'WsAxonRespuestaTransaccionesSoapClient', 1,
                         '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00');
                END;

                UPDATE [IntegrationMethods]
                SET [FunctionalDescription] = CASE [Code]
                        WHEN N'WSCFAACH.Proc_Contrapartidas' THEN N'Débitos originados en CFA enviados mediante Proc_Contrapartidas.'
                        WHEN N'WSCFAACH.Proc_Transacciones' THEN N'Créditos recibidos en CFA procesados mediante Proc_Transacciones.'
                        WHEN N'WSAXON.RegistrarRespuestaTransaccion' THEN N'Respuestas funcionales registradas para transacciones mediante WSAXON.'
                        ELSE [FunctionalDescription]
                    END,
                    [FunctionalCategory] = CASE [Code]
                        WHEN N'WSCFAACH.Proc_Contrapartidas' THEN N'Débitos'
                        WHEN N'WSCFAACH.Proc_Transacciones' THEN N'Créditos'
                        WHEN N'WSAXON.RegistrarRespuestaTransaccion' THEN N'Respuestas'
                        ELSE [FunctionalCategory]
                    END,
                    [RiskLevel] = N'No clasificado',
                    [UpdatedAt] = CASE
                        WHEN [UpdatedAt] < '2026-10-12T23:10:00+00:00' THEN '2026-10-12T23:10:00+00:00'
                        ELSE [UpdatedAt]
                    END
                WHERE [Code] IN (N'WSCFAACH.Proc_Contrapartidas', N'WSCFAACH.Proc_Transacciones', N'WSAXON.RegistrarRespuestaTransaccion');

                INSERT INTO [IntegrationContractVersions]
                    ([Id], [MethodId], [VersionLabel], [Status], [OperationName], [TargetNamespace], [SoapAction], [SourceType],
                     [SourceDocumentName], [SourceDocumentHash], [SourceReference], [Notes], [Revision], [ImportedAtUtc], [ImportedBy],
                     [ActivatedAtUtc], [ActivatedBy], [CreatedAt], [UpdatedAt])
                SELECT CAST('39b5693c-407b-505c-8e8a-e49e8558a82e' AS uniqueidentifier), m.[Id], N'baseline-v1', 3,
                       N'Proc_Contrapartidas', N'http://tempuri.org/', N'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas', N'BaselineMigration',
                       NULL, NULL, N'CFA-SOAP-UX-MAP-001/P0',
                       N'Baseline derivado del comportamiento SOAP caracterizado en P0; no implica importación WSDL.', 1, NULL, NULL,
                       '2026-10-12T23:10:00', N'CFA-SOAP-UX-MAP-001/P1.2',
                       '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00'
                FROM [IntegrationMethods] m
                WHERE m.[Code] = N'WSCFAACH.Proc_Contrapartidas'
                  AND NOT EXISTS (SELECT 1 FROM [IntegrationContractVersions] cv WHERE cv.[MethodId] = m.[Id] AND cv.[VersionLabel] = N'baseline-v1');

                INSERT INTO [IntegrationContractVersions]
                    ([Id], [MethodId], [VersionLabel], [Status], [OperationName], [TargetNamespace], [SoapAction], [SourceType],
                     [SourceDocumentName], [SourceDocumentHash], [SourceReference], [Notes], [Revision], [ImportedAtUtc], [ImportedBy],
                     [ActivatedAtUtc], [ActivatedBy], [CreatedAt], [UpdatedAt])
                SELECT CAST('9f3a13b6-eb79-5e43-803b-4c97b71a0f5d' AS uniqueidentifier), m.[Id], N'baseline-v1', 3,
                       N'Proc_Transacciones', N'http://tempuri.org/', N'http://tempuri.org/IWSCFAACH/Proc_Transacciones', N'BaselineMigration',
                       NULL, NULL, N'CFA-SOAP-UX-MAP-001/P0',
                       N'Baseline derivado del comportamiento SOAP caracterizado en P0; no implica importación WSDL.', 1, NULL, NULL,
                       '2026-10-12T23:10:00', N'CFA-SOAP-UX-MAP-001/P1.2',
                       '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00'
                FROM [IntegrationMethods] m
                WHERE m.[Code] = N'WSCFAACH.Proc_Transacciones'
                  AND NOT EXISTS (SELECT 1 FROM [IntegrationContractVersions] cv WHERE cv.[MethodId] = m.[Id] AND cv.[VersionLabel] = N'baseline-v1');

                INSERT INTO [IntegrationContractVersions]
                    ([Id], [MethodId], [VersionLabel], [Status], [OperationName], [TargetNamespace], [SoapAction], [SourceType],
                     [SourceDocumentName], [SourceDocumentHash], [SourceReference], [Notes], [Revision], [ImportedAtUtc], [ImportedBy],
                     [ActivatedAtUtc], [ActivatedBy], [CreatedAt], [UpdatedAt])
                SELECT CAST('4a093eb5-beed-5b15-bc7c-5768c0e0547e' AS uniqueidentifier), m.[Id], N'baseline-v1', 3,
                       N'RegistrarRespuestaTransaccion', N'http://tempuri.org/', N'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion', N'BaselineMigration',
                       NULL, NULL, N'CFA-SOAP-UX-MAP-001/P0',
                       N'Baseline derivado del comportamiento SOAP caracterizado en P0; conserva los siete parámetros actuales.', 1, NULL, NULL,
                       '2026-10-12T23:10:00', N'CFA-SOAP-UX-MAP-001/P1.2',
                       '2026-10-12T23:10:00+00:00', '2026-10-12T23:10:00+00:00'
                FROM [IntegrationMethods] m
                WHERE m.[Code] = N'WSAXON.RegistrarRespuestaTransaccion'
                  AND NOT EXISTS (SELECT 1 FROM [IntegrationContractVersions] cv WHERE cv.[MethodId] = m.[Id] AND cv.[VersionLabel] = N'baseline-v1');

                UPDATE p
                SET p.[ContractVersionId] = cv.[Id],
                    p.[XmlElementName] = p.[ParameterPath],
                    p.[XsdType] = CASE WHEN NULLIF(LTRIM(RTRIM(p.[DataType])), N'') IS NULL THEN N'string' ELSE p.[DataType] END,
                    p.[Nillable] = 0,
                    p.[MinOccurs] = CASE WHEN p.[Required] = 1 THEN 1 ELSE 0 END,
                    p.[MaxOccurs] = 1,
                    p.[SupportStatus] = N'Supported'
                FROM [IntegrationMethodParameters] p
                INNER JOIN [IntegrationMethods] m ON m.[Id] = p.[MethodId]
                INNER JOIN [IntegrationContractVersions] cv ON cv.[MethodId] = m.[Id] AND cv.[VersionLabel] = N'baseline-v1'
                WHERE m.[Code] IN (N'WSCFAACH.Proc_Contrapartidas', N'WSCFAACH.Proc_Transacciones', N'WSAXON.RegistrarRespuestaTransaccion');

                UPDATE s
                SET s.[ContractVersionId] = cv.[Id],
                    s.[Revision] = CASE WHEN s.[Revision] < 1 THEN 1 ELSE s.[Revision] END
                FROM [IntegrationMappingSets] s
                INNER JOIN [IntegrationMethods] m ON m.[Id] = s.[MethodId]
                INNER JOIN [IntegrationContractVersions] cv ON cv.[MethodId] = m.[Id] AND cv.[VersionLabel] = N'baseline-v1'
                WHERE m.[Code] IN (N'WSCFAACH.Proc_Contrapartidas', N'WSCFAACH.Proc_Transacciones', N'WSAXON.RegistrarRespuestaTransaccion');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationMethodParameters_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMethodParameters",
                column: "ContractVersionId",
                principalTable: "IntegrationContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationMappingSets_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMappingSets",
                column: "ContractVersionId",
                principalTable: "IntegrationContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [IntegrationContractVersions]
                    WHERE NOT ([SourceType] = N'BaselineMigration' AND [VersionLabel] = N'baseline-v1')
                )
                    THROW 51000, 'P1.2 downgrade refused: non-baseline contract version data exists.', 1;
                """);

            migrationBuilder.DropForeignKey(name: "FK_IntegrationMethodParameters_IntegrationContractVersions_ContractVersionId", table: "IntegrationMethodParameters");
            migrationBuilder.DropForeignKey(name: "FK_IntegrationMappingSets_IntegrationContractVersions_ContractVersionId", table: "IntegrationMappingSets");

            migrationBuilder.DropTable(name: "IntegrationContractVersions");

            migrationBuilder.DropIndex(name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath", table: "IntegrationMethodParameters");
            migrationBuilder.DropIndex(name: "IX_IntegrationMappingSets_ContractVersionId", table: "IntegrationMappingSets");
            migrationBuilder.DropIndex(name: "IX_IntegrationMappingSets_MethodId_ContractVersionId_Version", table: "IntegrationMappingSets");

            migrationBuilder.DropColumn(name: "FunctionalCategory", table: "IntegrationMethods");
            migrationBuilder.DropColumn(name: "FunctionalDescription", table: "IntegrationMethods");
            migrationBuilder.DropColumn(name: "RiskLevel", table: "IntegrationMethods");

            migrationBuilder.DropColumn(name: "ContractVersionId", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "MaxOccurs", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "MinOccurs", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "Nillable", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "SupportStatus", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "XmlElementName", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "XsdType", table: "IntegrationMethodParameters");

            migrationBuilder.DropColumn(name: "ContractVersionId", table: "IntegrationMappingSets");
            migrationBuilder.DropColumn(name: "Revision", table: "IntegrationMappingSets");
        }
    }
}
