using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A_U_ClimateScout.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquivalencyInfographicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "EquivalencyFactors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "EquivalencyFactors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitPlural",
                table: "EquivalencyFactors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitSingular",
                table: "EquivalencyFactors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "EquivalencyFactors");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "EquivalencyFactors");

            migrationBuilder.DropColumn(
                name: "UnitPlural",
                table: "EquivalencyFactors");

            migrationBuilder.DropColumn(
                name: "UnitSingular",
                table: "EquivalencyFactors");
        }
    }
}
