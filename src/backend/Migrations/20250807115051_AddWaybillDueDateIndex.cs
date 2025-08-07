using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ea_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWaybillDueDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Waybill_DueDate",
                table: "Waybills",
                column: "DueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Waybill_DueDate",
                table: "Waybills");
        }
    }
}
