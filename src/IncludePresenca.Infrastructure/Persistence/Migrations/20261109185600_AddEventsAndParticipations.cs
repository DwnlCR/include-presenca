using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncludePresenca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsAndParticipations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticipacoesEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacoesEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipacoesEventos_Eventos_EventId",
                        column: x => x.EventId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipacoesEventos_Membros_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_StartsAt",
                table: "Eventos",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesEventos_EventId_MemberId",
                table: "ParticipacoesEventos",
                columns: new[] { "EventId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesEventos_MemberId",
                table: "ParticipacoesEventos",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipacoesEventos");

            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
