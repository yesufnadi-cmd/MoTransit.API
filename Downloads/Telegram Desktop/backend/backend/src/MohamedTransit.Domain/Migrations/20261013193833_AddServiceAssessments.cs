using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MohamedTransit.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false),
                    AssessorId = table.Column<long>(type: "INTEGER", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DutyAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAssessments_Shipments_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceAssessments_Users_AssessorId",
                        column: x => x.AssessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAssessments_AssessorId",
                table: "ServiceAssessments",
                column: "AssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAssessments_ServiceId",
                table: "ServiceAssessments",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceAssessments");
        }
    }
}
