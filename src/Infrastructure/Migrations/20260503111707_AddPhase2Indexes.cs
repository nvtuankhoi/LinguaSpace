using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_Status_LanguageCode",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_RoomMediaSessions_RoomId_UserId",
                table: "RoomMediaSessions");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Type_Status_LanguageCode",
                table: "Rooms",
                columns: new[] { "RoomType", "Status", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomMediaSessions_RoomId_JoinedAt",
                table: "RoomMediaSessions",
                columns: new[] { "RoomId", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomMediaSessions_UserId_JoinedAt",
                table: "RoomMediaSessions",
                columns: new[] { "UserId", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                column: "LastMessageAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_Type_Status_LanguageCode",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_RoomMediaSessions_RoomId_JoinedAt",
                table: "RoomMediaSessions");

            migrationBuilder.DropIndex(
                name: "IX_RoomMediaSessions_UserId_JoinedAt",
                table: "RoomMediaSessions");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Status_LanguageCode",
                table: "Rooms",
                columns: new[] { "Status", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomMediaSessions_RoomId_UserId",
                table: "RoomMediaSessions",
                columns: new[] { "RoomId", "UserId" });
        }
    }
}
