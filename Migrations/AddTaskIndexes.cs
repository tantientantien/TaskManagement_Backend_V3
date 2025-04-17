using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddTaskIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_TaskComments_TaskId",
            table: "TaskComments",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_Attachments_TaskId",
            table: "Attachments",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskLabels_TaskId",
            table: "TaskLabels",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_UserId",
            table: "Tasks",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_AssigneeId",
            table: "Tasks",
            column: "AssigneeId");

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_CategoryId",
            table: "Tasks",
            column: "CategoryId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TaskComments_TaskId",
            table: "TaskComments");

        migrationBuilder.DropIndex(
            name: "IX_Attachments_TaskId",
            table: "Attachments");

        migrationBuilder.DropIndex(
            name: "IX_TaskLabels_TaskId",
            table: "TaskLabels");

        migrationBuilder.DropIndex(
            name: "IX_Tasks_UserId",
            table: "Tasks");

        migrationBuilder.DropIndex(
            name: "IX_Tasks_AssigneeId",
            table: "Tasks");

        migrationBuilder.DropIndex(
            name: "IX_Tasks_CategoryId",
            table: "Tasks");
    }
}