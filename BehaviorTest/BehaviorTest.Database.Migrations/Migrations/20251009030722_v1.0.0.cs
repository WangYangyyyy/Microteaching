using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BehaviorTest.Database.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class v100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "longtext", nullable: true),
                    Role = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "user",
                columns: new[] { "Id", "CreatedAt", "Email", "IsDeleted", "Name", "Password", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 9, 3, 7, 22, 99, DateTimeKind.Utc).AddTicks(1030), "363108445@qq.com", false, "张三", "fEqNCco3Yq9h5ZUglD3CZJT4lBs=", "Admin", new DateTime(2025, 10, 9, 3, 7, 22, 99, DateTimeKind.Utc).AddTicks(1034) },
                    { 2, new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(8865), "555108445@qq.com", false, "李四", "fEqNCco3Yq9h5ZUglD3CZJT4lBs=", "Teacher", new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(8870) },
                    { 3, new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(9136), "551238445@qq.com", false, "王五", "fEqNCco3Yq9h5ZUglD3CZJT4lBs=", "Student", new DateTime(2025, 10, 9, 3, 7, 22, 283, DateTimeKind.Utc).AddTicks(9136) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_Email",
                table: "user",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_Name",
                table: "user",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
