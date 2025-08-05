using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ea_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestigationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShippedItems",
                table: "Waybills",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientName",
                table: "Waybills",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Waybills",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "HasAnomalies",
                table: "Waybills",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInvestigatedAt",
                table: "Waybills",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Waybills",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTax",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientName",
                table: "Invoices",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Invoices",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "HasAnomalies",
                table: "Invoices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInvestigatedAt",
                table: "Invoices",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Invoices",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "InvestigatorTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultConfiguration = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigatorTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InvestigatorInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    CustomName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastExecutedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CustomConfiguration = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigatorInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestigatorInstances_InvestigatorTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "InvestigatorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InvestigationExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InvestigatorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestigationExecutions_InvestigatorInstances_InvestigatorId",
                        column: x => x.InvestigatorId,
                        principalTable: "InvestigatorInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InvestigationResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExecutionId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Severity = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Payload = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestigationResults_InvestigationExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "InvestigationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "InvestigatorTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "DefaultConfiguration", "Description", "DisplayName", "IsActive" },
                values: new object[,]
                {
                    { 1, "invoice", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"thresholds\":{\"maxTaxRatio\":0.5,\"minAmount\":0,\"maxFutureDays\":0}}", "Analyzes invoices for anomalies including negative amounts, excessive tax ratios, and future dates", "Invoice Investigator", true },
                    { 2, "waybill", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"thresholds\":{\"maxDaysLate\":7}}", "Monitors waybills for delivery delays and identifies shipments older than configured thresholds", "Waybill Investigator", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Waybill_Anomalies",
                table: "Waybills",
                column: "HasAnomalies");

            migrationBuilder.CreateIndex(
                name: "IX_Waybill_IssueDate",
                table: "Waybills",
                column: "GoodsIssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Waybill_LastInvestigated",
                table: "Waybills",
                column: "LastInvestigatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Anomalies",
                table: "Invoices",
                column: "HasAnomalies");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_IssueDate",
                table: "Invoices",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_LastInvestigated",
                table: "Invoices",
                column: "LastInvestigatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Execution_Investigator_Started",
                table: "InvestigationExecutions",
                columns: new[] { "InvestigatorId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Execution_Status",
                table: "InvestigationExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Entity",
                table: "InvestigationResults",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Result_Execution_Time",
                table: "InvestigationResults",
                columns: new[] { "ExecutionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Result_Severity",
                table: "InvestigationResults",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Timestamp",
                table: "InvestigationResults",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorInstance_LastExecuted",
                table: "InvestigatorInstances",
                column: "LastExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorInstance_Type_Active",
                table: "InvestigatorInstances",
                columns: new[] { "TypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorType_Code",
                table: "InvestigatorTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorType_IsActive",
                table: "InvestigatorTypes",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestigationResults");

            migrationBuilder.DropTable(
                name: "InvestigationExecutions");

            migrationBuilder.DropTable(
                name: "InvestigatorInstances");

            migrationBuilder.DropTable(
                name: "InvestigatorTypes");

            migrationBuilder.DropIndex(
                name: "IX_Waybill_Anomalies",
                table: "Waybills");

            migrationBuilder.DropIndex(
                name: "IX_Waybill_IssueDate",
                table: "Waybills");

            migrationBuilder.DropIndex(
                name: "IX_Waybill_LastInvestigated",
                table: "Waybills");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_Anomalies",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_IssueDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_LastInvestigated",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "HasAnomalies",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "LastInvestigatedAt",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Waybills");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "HasAnomalies",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastInvestigatedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "ShippedItems",
                table: "Waybills",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientName",
                table: "Waybills",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTax",
                table: "Invoices",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientName",
                table: "Invoices",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
