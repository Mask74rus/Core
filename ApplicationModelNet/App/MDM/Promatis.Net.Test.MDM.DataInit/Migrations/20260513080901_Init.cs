using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promatis.Net.Test.MDM.DataInit.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mdm");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangesJson = table.Column<string>(type: "jsonb", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnologicalOperation",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalOperation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentUnits",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionUnits",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionUnits",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorageUnits",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologicalOperationUnit",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalOperationUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationUnit_TechnologicalOperation_Operation~",
                        column: x => x.OperationId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalOperation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationUnit_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportUnits",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportUnits_Units_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperation_DeletedAt",
                schema: "mdm",
                table: "TechnologicalOperation",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnit_DeletedAt",
                schema: "mdm",
                table: "TechnologicalOperationUnit",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnit_OperationId",
                schema: "mdm",
                table: "TechnologicalOperationUnit",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnit_UnitId",
                schema: "mdm",
                table: "TechnologicalOperationUnit",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_DeletedAt",
                schema: "mdm",
                table: "Units",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "DepartmentUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "PositionUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "ProductionUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "StorageUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TechnologicalOperationUnit",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TransportUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TechnologicalOperation",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "Units",
                schema: "mdm");
        }
    }
}
