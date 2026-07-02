using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMusicServicePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredMusicService",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredMusicService",
                table: "AspNetUsers");
        }
    }
}
