using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniKnowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddShareQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalQuestionId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_OriginalQuestionId",
                table: "Questions",
                column: "OriginalQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Questions_OriginalQuestionId",
                table: "Questions",
                column: "OriginalQuestionId",
                principalTable: "Questions",
                principalColumn: "QuestionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Questions_OriginalQuestionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_OriginalQuestionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OriginalQuestionId",
                table: "Questions");
        }
    }
}
