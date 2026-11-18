using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAutoSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoScheduleEnabled",
                table: "Users",
                type: "bit",
                nullable: true);

            // Existing doctors start with the automatic schedule on, like newly created ones. (The column is shared
            // by the whole Users table and stays NULL for everyone who is not a doctor.)
            migrationBuilder.Sql("UPDATE [Users] SET [AutoScheduleEnabled] = 1 WHERE [UserType] = 'Doctor'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoScheduleEnabled",
                table: "Users");
        }
    }
}
