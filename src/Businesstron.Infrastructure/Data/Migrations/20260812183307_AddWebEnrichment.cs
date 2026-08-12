using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Businesstron.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableWebEnrichment",
                table: "SearchRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "BusinessNameRecords",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmails",
                table: "BusinessNameRecords",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "BusinessNameRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactSocials",
                table: "BusinessNameRecords",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebEnrichmentError",
                table: "BusinessNameRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WebEnrichmentStatus",
                table: "BusinessNameRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Websites",
                table: "BusinessNameRecords",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessNameRecords_WebEnrichmentStatus",
                table: "BusinessNameRecords",
                column: "WebEnrichmentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessNameRecords_WebEnrichmentStatus",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "EnableWebEnrichment",
                table: "SearchRuns");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "ContactEmails",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "ContactSocials",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "WebEnrichmentError",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "WebEnrichmentStatus",
                table: "BusinessNameRecords");

            migrationBuilder.DropColumn(
                name: "Websites",
                table: "BusinessNameRecords");
        }
    }
}
