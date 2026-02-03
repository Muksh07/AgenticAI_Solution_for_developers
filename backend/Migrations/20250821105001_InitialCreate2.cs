using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFeedbacks_ProjectLifecycles_ProjectID",
                table: "ProjectFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFeedbacks_ProjectID",
                table: "ProjectFeedbacks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_ProjectID",
                table: "ProjectFeedbacks",
                column: "ProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_ProjectLifecycles_ProjectID",
                table: "ProjectFeedbacks",
                column: "ProjectID",
                principalTable: "ProjectLifecycles",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
