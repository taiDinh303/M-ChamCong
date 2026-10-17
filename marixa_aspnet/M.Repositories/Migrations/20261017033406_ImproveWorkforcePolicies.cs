using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ImproveWorkforcePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaveLedger_LeaveRequestId",
                table: "LeaveLedger");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "LeaveRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledBy",
                table: "LeaveRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAt",
                table: "EmployeeHandovers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualHours",
                table: "Attendances",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "BreakStartTime",
                table: "AttendanceRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(12, 0, 0));

            migrationBuilder.CreateIndex(
                name: "UX_LeaveLedger_Request_Year_Type",
                table: "LeaveLedger",
                columns: new[] { "LeaveRequestId", "Year", "EntryType" },
                unique: true,
                filter: "[LeaveRequestId] IS NOT NULL AND [EntryType] IN (3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_LeaveLedger_Request_Year_Type",
                table: "LeaveLedger");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AppliedAt",
                table: "EmployeeHandovers");

            migrationBuilder.DropColumn(
                name: "BreakStartTime",
                table: "AttendanceRules");

            migrationBuilder.AlterColumn<int>(
                name: "ActualHours",
                table: "Attendances",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedger_LeaveRequestId",
                table: "LeaveLedger",
                column: "LeaveRequestId");
        }
    }
}
