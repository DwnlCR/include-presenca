using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncludePresenca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipationAttendanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAtUtc",
                table: "ParticipacoesEventos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Justification",
                table: "ParticipacoesEventos",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JustifiedAtUtc",
                table: "ParticipacoesEventos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ParticipacoesEventos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedInAtUtc",
                table: "ParticipacoesEventos");

            migrationBuilder.DropColumn(
                name: "Justification",
                table: "ParticipacoesEventos");

            migrationBuilder.DropColumn(
                name: "JustifiedAtUtc",
                table: "ParticipacoesEventos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ParticipacoesEventos");
        }
    }
}
