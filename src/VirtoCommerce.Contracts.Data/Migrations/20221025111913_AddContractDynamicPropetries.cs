using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.Contracts.Data.Migrations
{
    public partial class AddContractDynamicPropetries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractDynamicPropertyObjectValue",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ObjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Locale = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ShortTextValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LongTextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    IntegerValue = table.Column<int>(type: "int", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PropertyId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DictionaryItemId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PropertyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDynamicPropertyObjectValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDynamicPropertyObjectValue_Contract_ObjectId",
                        column: x => x.ObjectId,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractDynamicPropertyObjectValue_ObjectId",
                table: "ContractDynamicPropertyObjectValue",
                column: "ObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectType_ObjectId",
                table: "ContractDynamicPropertyObjectValue",
                columns: new[] { "ObjectType", "ObjectId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractDynamicPropertyObjectValue");
        }
    }
}
