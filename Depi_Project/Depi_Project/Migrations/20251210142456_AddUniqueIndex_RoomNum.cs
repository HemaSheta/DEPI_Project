using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depi_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex_RoomNum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Room_RoomNum_Unique",
                table: "Rooms",
                column: "RoomNum",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Room_RoomNum_Unique",
                table: "Rooms");
        }
    }
}
