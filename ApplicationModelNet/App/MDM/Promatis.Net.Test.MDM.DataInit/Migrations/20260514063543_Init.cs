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
                name: "ReferenceTreeBase",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceTreeBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceTreeBase_ReferenceTreeBase_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "mdm",
                        principalTable: "ReferenceTreeBase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologicalOperations",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnologicalParameters",
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
                    UnitOfMeasurement = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_ReferenceTreeBase_Id",
                        column: x => x.Id,
                        principalSchema: "mdm",
                        principalTable: "ReferenceTreeBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologicalOperationParameters",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MinValue = table.Column<double>(type: "double precision", nullable: true),
                    MaxValue = table.Column<double>(type: "double precision", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologicalOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationParameters_TechnologicalOperations_Op~",
                        column: x => x.OperationId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationParameters_TechnologicalParameters_Pa~",
                        column: x => x.ParameterId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "TechnologicalOperationUnits",
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
                    table.PrimaryKey("PK_TechnologicalOperationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationUnits_TechnologicalOperations_Operati~",
                        column: x => x.OperationId,
                        principalSchema: "mdm",
                        principalTable: "TechnologicalOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologicalOperationUnits_Units_UnitId",
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
                name: "IX_ReferenceTreeBase_DeletedAt",
                schema: "mdm",
                table: "ReferenceTreeBase",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTreeBase_ParentId",
                schema: "mdm",
                table: "ReferenceTreeBase",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationParameters_DeletedAt",
                schema: "mdm",
                table: "TechnologicalOperationParameters",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationParameters_OperationId",
                schema: "mdm",
                table: "TechnologicalOperationParameters",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationParameters_ParameterId",
                schema: "mdm",
                table: "TechnologicalOperationParameters",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnits_DeletedAt",
                schema: "mdm",
                table: "TechnologicalOperationUnits",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnits_OperationId",
                schema: "mdm",
                table: "TechnologicalOperationUnits",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalOperationUnits_UnitId",
                schema: "mdm",
                table: "TechnologicalOperationUnits",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologicalParameters_DeletedAt",
                schema: "mdm",
                table: "TechnologicalParameters",
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
                name: "TechnologicalOperationParameters",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TechnologicalOperationUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TransportUnits",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TechnologicalParameters",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "TechnologicalOperations",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "Units",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "ReferenceTreeBase",
                schema: "mdm");
        }
    }
}
