using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    public partial class AddAchColombiaCycleOperationalSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchColombiaCycleOperationalSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseCycleConfigId = table.Column<int>(type: "int", nullable: false),
                    AutomaticOutboundEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutomaticOutboundStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    AutomaticOutboundLatestStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    OutboundExecutionStrategy = table.Column<int>(type: "int", nullable: false),
                    EffectiveTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchColombiaCycleOperationalSchedules", x => x.Id);
                    table.CheckConstraint(
                        "CK_AchColombiaCycleOperationalSchedules_ExactTimeRequiresStart",
                        "[OutboundExecutionStrategy] <> 1 OR [AutomaticOutboundStartTime] IS NOT NULL");
                    table.CheckConstraint(
                        "CK_AchColombiaCycleOperationalSchedules_OutboundTimeRange",
                        "[AutomaticOutboundStartTime] IS NULL OR [AutomaticOutboundLatestStartTime] IS NULL OR [AutomaticOutboundStartTime] <= [AutomaticOutboundLatestStartTime]");
                    table.CheckConstraint(
                        "CK_AchColombiaCycleOperationalSchedules_Strategy",
                        "[OutboundExecutionStrategy] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_AchColombiaCycleOperationalSchedules_ClearingHouseCycleConfigs_ClearingHouseCycleConfigId",
                        column: x => x.ClearingHouseCycleConfigId,
                        principalTable: "ClearingHouseCycleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchColombiaCycleOperationalSchedules_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchColombiaCycleOperationalSchedules_ClearingHouseCycleConfigId",
                table: "AchColombiaCycleOperationalSchedules",
                column: "ClearingHouseCycleConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_AchColombiaCycleOperationalSchedules_House_Active",
                table: "AchColombiaCycleOperationalSchedules",
                columns: new[] { "ClearingHouseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_AchColombiaCycleOperationalSchedules_House_Cycle",
                table: "AchColombiaCycleOperationalSchedules",
                columns: new[] { "ClearingHouseId", "ClearingHouseCycleConfigId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AchColombiaCycleOperationalSchedules");
        }
    }
}
