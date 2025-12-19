using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BehaviorTest.Database.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class v102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Role = table.Column<string>(type: "longtext", nullable: false),
                    Auth = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "video_cuts",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    video_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    segment_index = table.Column<int>(type: "int", nullable: false),
                    segment_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    start_time = table.Column<double>(type: "double", nullable: false),
                    end_time = table.Column<double>(type: "double", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_cuts", x => x.id);
                    table.ForeignKey(
                        name: "FK_video_cuts_videos_video_id",
                        column: x => x.video_id,
                        principalTable: "videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 10, 1, 55, 38, 104, DateTimeKind.Utc).AddTicks(8943), new DateTime(2025, 10, 10, 1, 55, 38, 104, DateTimeKind.Utc).AddTicks(8944) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 10, 1, 55, 38, 108, DateTimeKind.Utc).AddTicks(165), new DateTime(2025, 10, 10, 1, 55, 38, 108, DateTimeKind.Utc).AddTicks(167) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 10, 1, 55, 38, 108, DateTimeKind.Utc).AddTicks(438), new DateTime(2025, 10, 10, 1, 55, 38, 108, DateTimeKind.Utc).AddTicks(439) });

            migrationBuilder.CreateIndex(
                name: "IX_video_cuts_video_id",
                table: "video_cuts",
                column: "video_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auth");

            migrationBuilder.DropTable(
                name: "video_cuts");

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 202, DateTimeKind.Utc).AddTicks(7155), new DateTime(2025, 10, 9, 8, 23, 28, 202, DateTimeKind.Utc).AddTicks(7160) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8441), new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8445) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8682), new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8682) });
        }
    }
}
