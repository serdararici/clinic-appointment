using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicManagerIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClinicManager_IsActive",
                table: "Users",
                type: "bit",
                nullable: true);

            // Existing clinic managers stay active (the column is shared by the whole Users table: NULL for others).
            migrationBuilder.Sql("UPDATE [Users] SET [ClinicManager_IsActive] = 1 WHERE [UserType] = 'ClinicManager'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClinicManager_IsActive",
                table: "Users");
        }
    }
}
