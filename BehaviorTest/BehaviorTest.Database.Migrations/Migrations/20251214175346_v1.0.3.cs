using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BehaviorTest.Database.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class v103 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluations_videos_video_id",
                table: "evaluations");

            migrationBuilder.AlterColumn<ulong>(
                name: "video_id",
                table: "evaluations",
                type: "bigint unsigned",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.CreateTable(
                name: "qa_records",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    session_id = table.Column<int>(type: "int", nullable: true),
                    question = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    answers_json = table.Column<string>(type: "longtext", nullable: false),
                    asked_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qa_records", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 14, 17, 53, 46, 487, DateTimeKind.Utc).AddTicks(7346), new DateTime(2025, 12, 14, 17, 53, 46, 487, DateTimeKind.Utc).AddTicks(7350) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 14, 17, 53, 46, 491, DateTimeKind.Utc).AddTicks(297), new DateTime(2025, 12, 14, 17, 53, 46, 491, DateTimeKind.Utc).AddTicks(299) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 14, 17, 53, 46, 491, DateTimeKind.Utc).AddTicks(563), new DateTime(2025, 12, 14, 17, 53, 46, 491, DateTimeKind.Utc).AddTicks(563) });

            migrationBuilder.AddForeignKey(
                name: "FK_evaluations_videos_video_id",
                table: "evaluations",
                column: "video_id",
                principalTable: "videos",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluations_videos_video_id",
                table: "evaluations");

            migrationBuilder.DropTable(
                name: "qa_records");

            migrationBuilder.AlterColumn<ulong>(
                name: "video_id",
                table: "evaluations",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 14, 11, 16, 59, 694, DateTimeKind.Utc).AddTicks(3590), new DateTime(2025, 11, 14, 11, 16, 59, 694, DateTimeKind.Utc).AddTicks(3593) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 14, 11, 16, 59, 697, DateTimeKind.Utc).AddTicks(4127), new DateTime(2025, 11, 14, 11, 16, 59, 697, DateTimeKind.Utc).AddTicks(4128) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 14, 11, 16, 59, 697, DateTimeKind.Utc).AddTicks(4353), new DateTime(2025, 11, 14, 11, 16, 59, 697, DateTimeKind.Utc).AddTicks(4353) });

            migrationBuilder.AddForeignKey(
                name: "FK_evaluations_videos_video_id",
                table: "evaluations",
                column: "video_id",
                principalTable: "videos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
