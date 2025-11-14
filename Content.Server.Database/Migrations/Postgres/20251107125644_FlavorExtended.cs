using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FlavorExtended : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "oocflavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "character_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "green_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "yellow_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "red_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tags_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "links_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nsfwflavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "oocflavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "character_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "green_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "yellow_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "red_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "tags_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "links_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "nsfwflavor_text",
                table: "profile");
        }
    }
}
