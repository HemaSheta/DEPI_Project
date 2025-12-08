using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depi_Project.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingIdentityUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_IdentityUserId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_IdentityUserId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IdentityUserId1",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId1",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_IdentityUserId1",
                table: "Bookings",
                column: "IdentityUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_IdentityUserId1",
                table: "Bookings",
                column: "IdentityUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
