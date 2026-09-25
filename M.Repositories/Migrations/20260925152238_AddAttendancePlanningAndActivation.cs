using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendancePlanningAndActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PayDate",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActualHours",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInPhoto",
                table: "Attendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckOutPhoto",
                table: "Attendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedHours",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlannedShiftId",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdjustedAt",
                table: "AttendanceLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjustedBy",
                table: "AttendanceLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentNote",
                table: "AttendanceLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdjusted",
                table: "AttendanceLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "AttendanceLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivationCodes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardHours = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyLeaveThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    PhotoRequired = table.Column<bool>(type: "bit", nullable: false),
                    GpsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    StandardHours = table.Column<int>(type: "int", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    IsNight = table.Column<bool>(type: "bit", nullable: false),
                    WorkDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeShifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeShifts_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ApprovedBy",
                table: "Attendances",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_PlannedShiftId",
                table: "Attendances",
                column: "PlannedShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationCodes_EmployeeId",
                table: "ActivationCodes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationCodes_UserId",
                table: "ActivationCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShifts_EmployeeId",
                table: "EmployeeShifts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShifts_ShiftId",
                table: "EmployeeShifts",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_ApprovedBy",
                table: "Attendances",
                column: "ApprovedBy",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Shifts_PlannedShiftId",
                table: "Attendances",
                column: "PlannedShiftId",
                principalTable: "Shifts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_ApprovedBy",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Shifts_PlannedShiftId",
                table: "Attendances");

            migrationBuilder.DropTable(
                name: "ActivationCodes");

            migrationBuilder.DropTable(
                name: "AttendanceRules");

            migrationBuilder.DropTable(
                name: "EmployeeShifts");

            migrationBuilder.DropTable(
                name: "HolidayCalendars");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ApprovedBy",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_PlannedShiftId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PayDate",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ActualHours",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInPhoto",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutPhoto",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PlannedHours",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PlannedShiftId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "AdjustedAt",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "AdjustedBy",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentNote",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "IsAdjusted",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "AttendanceLogs");
        }
    }
}
