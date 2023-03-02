using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.Contracts.Data.SqlServer.Migrations
{
    public partial class RenameIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_ObjectType_ObjectId",
                table: "ContractDynamicPropertyObjectValue",
                newName: "IX_ContractDynamicPropertyObjectValueEntity_ObjectType_ObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_ContractDynamicPropertyObjectValueEntity_ObjectType_ObjectId",
                table: "ContractDynamicPropertyObjectValue",
                newName: "IX_ObjectType_ObjectId");
        }
    }
}
