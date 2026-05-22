using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectMessageEditDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EditedAt",
                table: "DirectMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DirectMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DirectMessages");
        }
    }
}
