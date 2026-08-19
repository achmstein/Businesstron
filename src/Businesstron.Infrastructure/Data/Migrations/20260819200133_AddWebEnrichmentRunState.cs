using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Businesstron.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebEnrichmentRunState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WebEnrichmentCancellationRequested",
                table: "SearchRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WebEnrichmentError",
                table: "SearchRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WebEnrichmentHeartbeat",
                table: "SearchRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WebEnrichmentState",
                table: "SearchRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebEnrichmentCancellationRequested",
                table: "SearchRuns");

            migrationBuilder.DropColumn(
                name: "WebEnrichmentError",
                table: "SearchRuns");

            migrationBuilder.DropColumn(
                name: "WebEnrichmentHeartbeat",
                table: "SearchRuns");

            migrationBuilder.DropColumn(
                name: "WebEnrichmentState",
                table: "SearchRuns");
        }
    }
}
