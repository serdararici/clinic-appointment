using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: true);

            // The column only applies to staff rows (shared Users table), so it is nullable. Staff that
            // already exist must read back as active, otherwise loading them would fail on a NULL.
            migrationBuilder.Sql("UPDATE [Users] SET [IsActive] = 1 WHERE [UserType] IN ('Doctor', 'HealthTechnician')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
