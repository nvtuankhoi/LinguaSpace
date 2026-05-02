using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMutedToRoomParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "RoomParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "RoomParticipants");
        }
    }
}
