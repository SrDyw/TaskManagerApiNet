using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreTaskStateAsInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Translate existing enum names into their integer values before changing the column type.
            migrationBuilder.Sql(
                """
                UPDATE [Tasks] SET [State] = CASE [State]
                    WHEN N'Pending' THEN N'0'
                    WHEN N'InProgress' THEN N'1'
                    WHEN N'Done' THEN N'2'
                    ELSE [State]
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "State",
                table: "Tasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_State",
                table: "Tasks",
                sql: "[State] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_State",
                table: "Tasks");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Tasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(
                """
                UPDATE [Tasks] SET [State] = CASE [State]
                    WHEN N'0' THEN N'Pending'
                    WHEN N'1' THEN N'InProgress'
                    WHEN N'2' THEN N'Done'
                    ELSE [State]
                END;
                """);
        }
    }
}
