using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectLifecycles",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsightElicitationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SolidificationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BlueprintingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeSynthesisStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Testing_Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Testing_Functional = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Testing_Integration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Doc_HLD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Doc_LLD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Doc_UserManual = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Doc_TraceabilityMatrix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeReview = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLifecycles", x => x.ProjectID);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFeedbacks",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    CodeCoverageScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CodeQualityScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ArtifactQuality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewerComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeedbacks", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_ProjectLifecycles_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "ProjectLifecycles",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_ProjectID",
                table: "ProjectFeedbacks",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectFeedbacks");

            migrationBuilder.DropTable(
                name: "ProjectLifecycles");
        }
    }
}
