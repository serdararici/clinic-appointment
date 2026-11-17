using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationsAndStaffModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAvailabilities_Users_DoctorId",
                table: "DoctorAvailabilities");

            migrationBuilder.DropTable(
                name: "DoctorService");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "DoctorId",
                table: "DoctorAvailabilities",
                newName: "StaffMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_DoctorAvailabilities_DoctorId",
                table: "DoctorAvailabilities",
                newName: "IX_DoctorAvailabilities_StaffMemberId");

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "Services",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ClinicFacilities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StaffMemberId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HealthOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthOrganizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStaffMember",
                columns: table => new
                {
                    ServicesProvidedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStaffMember", x => new { x.ServicesProvidedId, x.StaffMemberId });
                    table.ForeignKey(
                        name: "FK_ServiceStaffMember_Services_ServicesProvidedId",
                        column: x => x.ServicesProvidedId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceStaffMember_Users_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClinicFacilityId",
                table: "Users",
                column: "ClinicFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFacilities_OrganizationId",
                table: "ClinicFacilities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStaffMember_StaffMemberId",
                table: "ServiceStaffMember",
                column: "StaffMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicFacilities_HealthOrganizations_OrganizationId",
                table: "ClinicFacilities",
                column: "OrganizationId",
                principalTable: "HealthOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAvailabilities_Users_StaffMemberId",
                table: "DoctorAvailabilities",
                column: "StaffMemberId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ClinicFacilities_ClinicFacilityId",
                table: "Users",
                column: "ClinicFacilityId",
                principalTable: "ClinicFacilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicFacilities_HealthOrganizations_OrganizationId",
                table: "ClinicFacilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAvailabilities_Users_StaffMemberId",
                table: "DoctorAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ClinicFacilities_ClinicFacilityId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "HealthOrganizations");

            migrationBuilder.DropTable(
                name: "ServiceStaffMember");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClinicFacilityId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ClinicFacilities_OrganizationId",
                table: "ClinicFacilities");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ClinicFacilities");

            migrationBuilder.DropColumn(
                name: "StaffMemberId",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "StaffMemberId",
                table: "DoctorAvailabilities",
                newName: "DoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_DoctorAvailabilities_StaffMemberId",
                table: "DoctorAvailabilities",
                newName: "IX_DoctorAvailabilities_DoctorId");

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DoctorService",
                columns: table => new
                {
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicesProvidedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorService", x => new { x.DoctorId, x.ServicesProvidedId });
                    table.ForeignKey(
                        name: "FK_DoctorService_Services_ServicesProvidedId",
                        column: x => x.ServicesProvidedId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorService_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorService_ServicesProvidedId",
                table: "DoctorService",
                column: "ServicesProvidedId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAvailabilities_Users_DoctorId",
                table: "DoctorAvailabilities",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
