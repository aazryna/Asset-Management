using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagementApi.Migrations
{
    public partial class EnsureFinalNotesColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Assets\" ADD COLUMN IF NOT EXISTS \"FinalNotes\" text;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Assets\" DROP COLUMN IF EXISTS \"FinalNotes\";");
        }
    }
}
