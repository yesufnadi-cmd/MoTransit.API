using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MohamedTransit.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceDocumentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceStageId = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    recordStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_Shipments_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Shipments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_ServiceId",
                table: "ServiceDocuments",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_UploadedByUserId",
                table: "ServiceDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_VerifiedByUserId",
                table: "ServiceDocuments",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDocuments");
        }
    }
}
