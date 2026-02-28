using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Sessions");
        }
    }
}
