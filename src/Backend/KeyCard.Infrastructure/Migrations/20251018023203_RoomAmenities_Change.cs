using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyCard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomAmenities_Change : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueDecimal",
                table: "RoomTypeAmenities");

            migrationBuilder.DropColumn(
                name: "ValueInt",
                table: "RoomTypeAmenities");

            migrationBuilder.RenameColumn(
                name: "ValueText",
                table: "RoomTypeAmenities",
                newName: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "RoomTypeAmenities",
                newName: "ValueText");

            migrationBuilder.AddColumn<decimal>(
                name: "ValueDecimal",
                table: "RoomTypeAmenities",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueInt",
                table: "RoomTypeAmenities",
                type: "int",
                nullable: true);
        }
    }
}
