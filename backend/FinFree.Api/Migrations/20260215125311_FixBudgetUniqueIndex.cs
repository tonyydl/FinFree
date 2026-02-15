using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinFree.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixBudgetUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 移除舊的不含 NULL 處理的唯一索引
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_Year_Month_CategoryId",
                table: "Budgets");

            // 分類預算唯一約束（CategoryId 有值時）
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Budgets_UserId_Year_Month_CategoryId""
                  ON ""Budgets""(""UserId"", ""Year"", ""Month"", ""CategoryId"")
                  WHERE ""CategoryId"" IS NOT NULL;");

            // 整體預算唯一約束（CategoryId 為 NULL 時）
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Budgets_UserId_Year_Month_Overall""
                  ON ""Budgets""(""UserId"", ""Year"", ""Month"")
                  WHERE ""CategoryId"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Budgets_UserId_Year_Month_CategoryId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Budgets_UserId_Year_Month_Overall"";");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_Year_Month_CategoryId",
                table: "Budgets",
                columns: new[] { "UserId", "Year", "Month", "CategoryId" },
                unique: true);
        }
    }
}
