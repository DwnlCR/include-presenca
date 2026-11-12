using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncludePresenca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipationAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaParticipacoesEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PreviousCheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewCheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NewJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviousJustifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewJustifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaParticipacoesEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditoriaParticipacoesEventos_Eventos_EventId",
                        column: x => x.EventId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaParticipacoesEventos_Membros_ChangedByMemberId",
                        column: x => x.ChangedByMemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaParticipacoesEventos_Membros_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaParticipacoesEventos_ParticipacoesEventos_Particip~",
                        column: x => x.ParticipationId,
                        principalTable: "ParticipacoesEventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaParticipacoesEventos_ChangedAtUtc",
                table: "AuditoriaParticipacoesEventos",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaParticipacoesEventos_ChangedByMemberId",
                table: "AuditoriaParticipacoesEventos",
                column: "ChangedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaParticipacoesEventos_EventId",
                table: "AuditoriaParticipacoesEventos",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaParticipacoesEventos_MemberId",
                table: "AuditoriaParticipacoesEventos",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaParticipacoesEventos_ParticipationId_ChangedAtUtc",
                table: "AuditoriaParticipacoesEventos",
                columns: new[] { "ParticipationId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaParticipacoesEventos");
        }
    }
}
