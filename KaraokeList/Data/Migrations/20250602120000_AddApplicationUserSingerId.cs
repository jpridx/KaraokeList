using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserSingerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SingerId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SingerId",
                table: "AspNetUsers",
                column: "SingerId",
                unique: true,
                filter: "[SingerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers",
                column: "SingerId",
                principalTable: "Singers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SingerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SingerId",
                table: "AspNetUsers");
        }
    }
}
