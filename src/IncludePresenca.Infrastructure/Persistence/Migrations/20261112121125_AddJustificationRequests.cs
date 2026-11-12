using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncludePresenca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJustificationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitacoesJustificativas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DecidedByMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttachmentStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentOriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttachmentLength = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesJustificativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesJustificativas_Membros_DecidedByMemberId",
                        column: x => x.DecidedByMemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacoesJustificativas_Membros_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacoesJustificativas_ParticipacoesEventos_Participati~",
                        column: x => x.ParticipationId,
                        principalTable: "ParticipacoesEventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditoriaSolicitacoesJustificativas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JustificationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PreviousParticipationStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NewParticipationStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaSolicitacoesJustificativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditoriaSolicitacoesJustificativas_Membros_ChangedByMember~",
                        column: x => x.ChangedByMemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaSolicitacoesJustificativas_Membros_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaSolicitacoesJustificativas_ParticipacoesEventos_Pa~",
                        column: x => x.ParticipationId,
                        principalTable: "ParticipacoesEventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaSolicitacoesJustificativas_SolicitacoesJustificati~",
                        column: x => x.JustificationRequestId,
                        principalTable: "SolicitacoesJustificativas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaSolicitacoesJustificativas_ChangedByMemberId",
                table: "AuditoriaSolicitacoesJustificativas",
                column: "ChangedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaSolicitacoesJustificativas_JustificationRequestId_~",
                table: "AuditoriaSolicitacoesJustificativas",
                columns: new[] { "JustificationRequestId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaSolicitacoesJustificativas_MemberId",
                table: "AuditoriaSolicitacoesJustificativas",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaSolicitacoesJustificativas_ParticipationId",
                table: "AuditoriaSolicitacoesJustificativas",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesJustificativas_DecidedByMemberId",
                table: "SolicitacoesJustificativas",
                column: "DecidedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesJustificativas_MemberId",
                table: "SolicitacoesJustificativas",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesJustificativas_ParticipationId_Status",
                table: "SolicitacoesJustificativas",
                columns: new[] { "ParticipationId", "Status" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesJustificativas_SubmittedAtUtc",
                table: "SolicitacoesJustificativas",
                column: "SubmittedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaSolicitacoesJustificativas");

            migrationBuilder.DropTable(
                name: "SolicitacoesJustificativas");
        }
    }
}
