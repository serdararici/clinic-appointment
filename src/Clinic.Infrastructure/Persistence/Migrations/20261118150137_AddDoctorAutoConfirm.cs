using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAutoConfirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoConfirmAppointments",
                table: "Users",
                type: "bit",
                nullable: true);

            // Existing doctors keep confirming appointments at once, like newly created ones. (The column is shared by the
            // whole Users table and stays NULL for everyone who is not a doctor.)
            migrationBuilder.Sql("UPDATE [Users] SET [AutoConfirmAppointments] = 1 WHERE [UserType] = 'Doctor'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoConfirmAppointments",
                table: "Users");
        }
    }
}
