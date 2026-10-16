using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveAttendanceRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_AttendanceRules_Active",
                table: "AttendanceRules",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1 AND [DeletedTime] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AttendanceRules_Active",
                table: "AttendanceRules");
        }
    }
}
