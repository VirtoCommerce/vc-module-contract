using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.Contracts.Data.SqlServer.Migrations
{
    public partial class AddPricelistAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BasePricelistAssignmentId",
                table: "Contract",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityPricelistAssignmentId",
                table: "Contract",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePricelistAssignmentId",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "PriorityPricelistAssignmentId",
                table: "Contract");
        }
    }
}
