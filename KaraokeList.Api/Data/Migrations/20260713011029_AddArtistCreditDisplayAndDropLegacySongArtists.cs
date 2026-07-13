using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistCreditDisplayAndDropLegacySongArtists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Artists_Artist",
                table: "Songs");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Artists_SecondaryArtist",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_Artist",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_SecondaryArtist",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "SecondaryArtist",
                table: "Songs");

            migrationBuilder.AddColumn<string>(
                name: "ArtistCreditDisplay",
                table: "Songs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtistCreditDisplay",
                table: "Songs");

            migrationBuilder.AddColumn<int>(
                name: "Artist",
                table: "Songs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondaryArtist",
                table: "Songs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Artist",
                table: "Songs",
                column: "Artist");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SecondaryArtist",
                table: "Songs",
                column: "SecondaryArtist");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Artists_Artist",
                table: "Songs",
                column: "Artist",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Artists_SecondaryArtist",
                table: "Songs",
                column: "SecondaryArtist",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
