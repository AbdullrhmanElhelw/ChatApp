using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupleSpaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoupleSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Partner1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Partner2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InviteCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PairedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Partner1Mood = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Partner2Mood = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpecialDatesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemoriesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoveNotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyAnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoupleSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoupleSpaces_AspNetUsers_Partner1Id",
                        column: x => x.Partner1Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoupleSpaces_AspNetUsers_Partner2Id",
                        column: x => x.Partner2Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoupleSpaces_InviteCode",
                table: "CoupleSpaces",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoupleSpaces_Partner1Id",
                table: "CoupleSpaces",
                column: "Partner1Id");

            migrationBuilder.CreateIndex(
                name: "IX_CoupleSpaces_Partner2Id",
                table: "CoupleSpaces",
                column: "Partner2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoupleSpaces");
        }
    }
}
