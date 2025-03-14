using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class WhiteExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wd_humanoid_extensions",
                columns: table => new
                {
                    wd_humanoid_extensions_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    voice_id = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Nord"),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wd_humanoid_extensions", x => x.wd_humanoid_extensions_id);
                    table.ForeignKey(
                        name: "FK_wd_humanoid_extensions_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wd_humanoid_extensions_profile_id",
                table: "wd_humanoid_extensions",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wd_humanoid_extensions");
        }
    }
}
