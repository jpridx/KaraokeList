using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSingerLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SingerLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SingerId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingerLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SingerLists_Singers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "Singers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SingerListSongs",
                columns: table => new
                {
                    ListId = table.Column<int>(type: "int", nullable: false),
                    SongId = table.Column<int>(type: "int", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingerListSongs", x => new { x.ListId, x.SongId });
                    table.ForeignKey(
                        name: "FK_SingerListSongs_SingerLists_ListId",
                        column: x => x.ListId,
                        principalTable: "SingerLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SingerListSongs_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SingerLists_SingerId_Kind",
                table: "SingerLists",
                columns: new[] { "SingerId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SingerListSongs_SongId",
                table: "SingerListSongs",
                column: "SongId");

            migrationBuilder.Sql(
                """
                INSERT INTO SingerLists (SingerId, Kind, CreatedUtc, IsSystem)
                SELECT s.Id, kinds.Kind, GETUTCDATE(), 1
                FROM Singers s
                CROSS APPLY (VALUES (0), (1), (2)) AS kinds(Kind)
                WHERE NOT EXISTS (
                    SELECT 1 FROM SingerLists sl
                    WHERE sl.SingerId = s.Id AND sl.Kind = kinds.Kind);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO SingerListSongs (ListId, SongId, AddedUtc)
                SELECT sl.Id, performed.Song, GETUTCDATE()
                FROM (
                    SELECT DISTINCT Singer, Song FROM Performances
                ) performed
                INNER JOIN SingerLists sl
                    ON sl.SingerId = performed.Singer AND sl.Kind = 0
                WHERE NOT EXISTS (
                    SELECT 1 FROM SingerListSongs sls
                    WHERE sls.ListId = sl.Id AND sls.SongId = performed.Song);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SingerListSongs");

            migrationBuilder.DropTable(
                name: "SingerLists");
        }
    }
}
