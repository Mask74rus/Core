using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promatis.Net.Test.MDM.DataInit.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitOfMeasurement",
                schema: "mdm",
                table: "TechnologicalParameters");

            migrationBuilder.AddColumn<int>(
                name: "AllowedMethods",
                schema: "mdm",
                table: "TechnologicalParameters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasurementId",
                schema: "mdm",
                table: "TechnologicalParameters",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitOfMeasurements",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasurements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameters_UnitOfMeasurementId",
                schema: "mdm",
                table: "TechnologicalParameters",
                column: "UnitOfMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasurements_DeletedAt",
                schema: "mdm",
                table: "UnitOfMeasurements",
                column: "DeletedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_TechnologicalParameters_UnitOfMeasurements_UnitOfMeasuremen~",
                schema: "mdm",
                table: "TechnologicalParameters",
                column: "UnitOfMeasurementId",
                principalSchema: "mdm",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TechnologicalParameters_UnitOfMeasurements_UnitOfMeasuremen~",
                schema: "mdm",
                table: "TechnologicalParameters");

            migrationBuilder.DropTable(
                name: "UnitOfMeasurements",
                schema: "mdm");

            migrationBuilder.DropIndex(
                name: "IX_TechnologicalParameters_UnitOfMeasurementId",
                schema: "mdm",
                table: "TechnologicalParameters");

            migrationBuilder.DropColumn(
                name: "AllowedMethods",
                schema: "mdm",
                table: "TechnologicalParameters");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasurementId",
                schema: "mdm",
                table: "TechnologicalParameters");

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasurement",
                schema: "mdm",
                table: "TechnologicalParameters",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
