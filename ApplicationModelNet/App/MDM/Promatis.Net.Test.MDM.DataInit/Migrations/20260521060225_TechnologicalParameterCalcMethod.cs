using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promatis.Net.Test.MDM.DataInit.Migrations
{
    /// <inheritdoc />
    public partial class TechnologicalParameterCalcMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Units_ParentId",
                schema: "mdm",
                table: "Units",
                newName: "IX_UnitBase_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnologicalOperations_ParentId",
                schema: "mdm",
                table: "TechnologicalOperations",
                newName: "IX_TechnologicalOperation_ParentId");

            migrationBuilder.CreateTable(
                name: "TechnologicalParameterCalcMethods",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologicalOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologicalParameterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationMethod = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalParameterCalcMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologicalParameterCalcMethods_TechnologicalOperations_T~",
                        column: x => x.TechnologicalOperationId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologicalParameterCalcMethods_TechnologicalParameters_T~",
                        column: x => x.TechnologicalParameterId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologicalParameterCalcMethods_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameterCalcMethods_DeletedAt",
                schema: "mdm",
                table: "TechnologicalParameterCalcMethods",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameterCalcMethods_TechnologicalOperationId",
                schema: "mdm",
                table: "TechnologicalParameterCalcMethods",
                column: "TechnologicalOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameterCalcMethods_TechnologicalParameterId",
                schema: "mdm",
                table: "TechnologicalParameterCalcMethods",
                column: "TechnologicalParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameterCalcMethods_UnitId",
                schema: "mdm",
                table: "TechnologicalParameterCalcMethods",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnologicalParameterCalcMethods",
                schema: "mdm");

            migrationBuilder.RenameIndex(
                name: "IX_UnitBase_ParentId",
                schema: "mdm",
                table: "Units",
                newName: "IX_Units_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnologicalOperation_ParentId",
                schema: "mdm",
                table: "TechnologicalOperations",
                newName: "IX_TechnologicalOperations_ParentId");
        }
    }
}
