using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM Performances WHERE Song IS NULL OR Singer IS NULL;
                DELETE FROM Performances WHERE Song NOT IN (SELECT Id FROM Songs);
                DELETE FROM Performances WHERE Singer NOT IN (SELECT Id FROM Singers);
                UPDATE Performances SET Venue = NULL
                WHERE Venue IS NOT NULL AND Venue NOT IN (SELECT Id FROM Venues);
                UPDATE Songs SET SecondaryArtist = NULL
                WHERE SecondaryArtist IS NOT NULL AND SecondaryArtist NOT IN (SELECT Id FROM Artists);
                UPDATE Songs SET Artist = NULL
                WHERE Artist IS NOT NULL AND Artist NOT IN (SELECT Id FROM Artists);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "Song",
                table: "Performances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Singer",
                table: "Performances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Artist",
                table: "Songs",
                column: "Artist");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SecondaryArtist",
                table: "Songs",
                column: "SecondaryArtist");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_Singer",
                table: "Performances",
                column: "Singer");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_Song",
                table: "Performances",
                column: "Song");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_Venue",
                table: "Performances",
                column: "Venue");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers",
                column: "SingerId",
                principalTable: "Singers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Performances_Singers_Singer",
                table: "Performances",
                column: "Singer",
                principalTable: "Singers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Performances_Songs_Song",
                table: "Performances",
                column: "Song",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Performances_Venues_Venue",
                table: "Performances",
                column: "Venue",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Performances_Singers_Singer",
                table: "Performances");

            migrationBuilder.DropForeignKey(
                name: "FK_Performances_Songs_Song",
                table: "Performances");

            migrationBuilder.DropForeignKey(
                name: "FK_Performances_Venues_Venue",
                table: "Performances");

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

            migrationBuilder.DropIndex(
                name: "IX_Performances_Singer",
                table: "Performances");

            migrationBuilder.DropIndex(
                name: "IX_Performances_Song",
                table: "Performances");

            migrationBuilder.DropIndex(
                name: "IX_Performances_Venue",
                table: "Performances");

            migrationBuilder.AlterColumn<int>(
                name: "Song",
                table: "Performances",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Singer",
                table: "Performances",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Singers_SingerId",
                table: "AspNetUsers",
                column: "SingerId",
                principalTable: "Singers",
                principalColumn: "Id");
        }
    }
}
