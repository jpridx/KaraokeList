using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSingerSongTicklerExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SingerSongTicklerExclusions",
                columns: table => new
                {
                    SingerId = table.Column<int>(type: "int", nullable: false),
                    SongId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingerSongTicklerExclusions", x => new { x.SingerId, x.SongId });
                    table.ForeignKey(
                        name: "FK_SingerSongTicklerExclusions_Singers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "Singers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SingerSongTicklerExclusions_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SingerSongTicklerExclusions_SongId",
                table: "SingerSongTicklerExclusions",
                column: "SongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SingerSongTicklerExclusions");
        }
    }
}
