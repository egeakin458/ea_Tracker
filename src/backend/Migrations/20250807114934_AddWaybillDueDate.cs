using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ea_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWaybillDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Waybills",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Waybills");
        }
    }
}
