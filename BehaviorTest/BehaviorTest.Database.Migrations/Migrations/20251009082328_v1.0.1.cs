using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BehaviorTest.Database.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class v101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "videos",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    source_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    original_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    duration_seconds = table.Column<uint>(type: "int unsigned", nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_videos", x => x.id);
                    table.ForeignKey(
                        name: "FK_videos_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    video_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    run_uuid = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    evaluation_model = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    input_source = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    total_score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    philosophy_score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    content_score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    process_score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    effect_score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    philosophy_rationale_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    content_rationale_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    process_rationale_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    effect_rationale_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    report_store_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    raw_response_store_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_evaluations_videos_video_id",
                        column: x => x.video_id,
                        principalTable: "videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pipeline_runs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    video_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    run_uuid = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    stage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    error_message = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_pipeline_runs_videos_video_id",
                        column: x => x.video_id,
                        principalTable: "videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "transcripts",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    video_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    run_uuid = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    segment_index = table.Column<uint>(type: "int unsigned", nullable: false),
                    transcriber_model = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    language_code = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    text_store_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    characters_count = table.Column<uint>(type: "int unsigned", nullable: true),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transcripts", x => x.id);
                    table.ForeignKey(
                        name: "FK_transcripts_videos_video_id",
                        column: x => x.video_id,
                        principalTable: "videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "summaries",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    video_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    run_uuid = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    related_transcript_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    summary_model = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    summary_store_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    characters_count = table.Column<uint>(type: "int unsigned", nullable: true),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_summaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_summaries_transcripts_related_transcript_id",
                        column: x => x.related_transcript_id,
                        principalTable: "transcripts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_summaries_videos_video_id",
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
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 202, DateTimeKind.Utc).AddTicks(7155), "admin", new DateTime(2025, 10, 9, 8, 23, 28, 202, DateTimeKind.Utc).AddTicks(7160) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8441), "operator", new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8445) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8682), "viewer", new DateTime(2025, 10, 9, 8, 23, 28, 205, DateTimeKind.Utc).AddTicks(8682) });

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_video_id",
                table: "evaluations",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_runs_video_id",
                table: "pipeline_runs",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_summaries_related_transcript_id",
                table: "summaries",
                column: "related_transcript_id");

            migrationBuilder.CreateIndex(
                name: "IX_summaries_video_id",
                table: "summaries",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_transcripts_video_id",
                table: "transcripts",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_videos_user_id",
                table: "videos",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "pipeline_runs");

            migrationBuilder.DropTable(
                name: "summaries");

            migrationBuilder.DropTable(
                name: "transcripts");

            migrationBuilder.DropTable(
                name: "videos");

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 3, 7, 22, 99, DateTimeKind.Utc).AddTicks(1030), "Admin", new DateTime(2025, 10, 9, 3, 7, 22, 99, DateTimeKind.Utc).AddTicks(1034) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(8865), "Teacher", new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(8870) });

            migrationBuilder.UpdateData(
                table: "user",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(9136), "Student", new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(9136) });
        }
    }
}
