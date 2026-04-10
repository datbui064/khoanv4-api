using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaNVCB_API.Migrations
{
    public partial class MakeSessionIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ yêu cầu EF sửa đúng cột SessionId trong bảng QuizAttempts thành nullable
            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "QuizAttempts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Code đảo ngược nếu muốn quay lại trạng thái cũ
            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "QuizAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}