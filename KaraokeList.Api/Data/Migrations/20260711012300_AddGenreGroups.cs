using KaraokeList.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraokeList.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenreGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GenreGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenreGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenreGroupGenres",
                columns: table => new
                {
                    GenreGroupId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenreGroupGenres", x => new { x.GenreGroupId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_GenreGroupGenres_GenreGroups_GenreGroupId",
                        column: x => x.GenreGroupId,
                        principalTable: "GenreGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GenreGroupGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenreGroupGenres_GenreId",
                table: "GenreGroupGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_GenreGroups_GroupName",
                table: "GenreGroups",
                column: "GroupName",
                unique: true);

            migrationBuilder.Sql(GenreGroupSeedSql.GroupsSql);
            migrationBuilder.Sql(GenreGroupSeedSql.MappingsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenreGroupGenres");

            migrationBuilder.DropTable(
                name: "GenreGroups");
        }
    }
}
