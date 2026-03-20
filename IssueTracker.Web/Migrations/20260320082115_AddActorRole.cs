using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddActorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Actors",
                type: "TEXT",
                nullable: false,
                defaultValue: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Actors");
        }
    }
}
