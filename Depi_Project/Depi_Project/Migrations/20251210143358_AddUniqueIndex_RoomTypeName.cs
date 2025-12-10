using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depi_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex_RoomTypeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RoomType_RoomTypeName_Unique",
                table: "RoomTypes",
                column: "RoomTypeName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomType_RoomTypeName_Unique",
                table: "RoomTypes");
        }
    }
}
