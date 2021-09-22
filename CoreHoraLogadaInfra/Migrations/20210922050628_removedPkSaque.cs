using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreHoraLogadaInfra.Migrations
{
    public partial class removedPkSaque : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Saque_Role_RoleId",
                table: "Saque");

            migrationBuilder.DropIndex(
                name: "IX_Saque_RoleId",
                table: "Saque");

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "Saque",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "Saque");

            migrationBuilder.CreateIndex(
                name: "IX_Saque_RoleId",
                table: "Saque",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Saque_Role_RoleId",
                table: "Saque",
                column: "RoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
