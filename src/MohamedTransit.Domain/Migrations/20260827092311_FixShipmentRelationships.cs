using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MohamedTransit.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixShipmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AssignedAssessorId",
                table: "Shipments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignedCaseExecutorId",
                table: "Shipments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentNotes",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredValue",
                table: "Shipments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RiskLevel",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteCategory",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCategory",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ServiceStageExecution",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: true),
                    StageSpot = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresCustomerAction = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStageExecution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStageExecution_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceStageExecution_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenceDocument = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductAmount = table.Column<int>(type: "int", nullable: false),
                    ServiceStageId = table.Column<long>(type: "bigint", nullable: true),
                    ShipmentStage = table.Column<int>(type: "int", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StageComment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceStageId = table.Column<long>(type: "bigint", nullable: true),
                    CommentedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageComment_ServiceStageExecution_ServiceStageId",
                        column: x => x.ServiceStageId,
                        principalTable: "ServiceStageExecution",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StageComment_Users_CommentedByUserId",
                        column: x => x.CommentedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StageDocuments",
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
                    ServiceStageId = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceStageExecutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageDocuments_ServiceStageExecution_ServiceStageExecutionId",
                        column: x => x.ServiceStageExecutionId,
                        principalTable: "ServiceStageExecution",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StageDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StageDocuments_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedAssessorId",
                table: "Shipments",
                column: "AssignedAssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedCaseExecutorId",
                table: "Shipments",
                column: "AssignedCaseExecutorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStageExecution_ShipmentId",
                table: "ServiceStageExecution",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStageExecution_UpdatedByUserId",
                table: "ServiceStageExecution",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StageComment_CommentedByUserId",
                table: "StageComment",
                column: "CommentedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StageComment_ServiceStageId",
                table: "StageComment",
                column: "ServiceStageId");

            migrationBuilder.CreateIndex(
                name: "IX_StageDocuments_ServiceStageExecutionId",
                table: "StageDocuments",
                column: "ServiceStageExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StageDocuments_UploadedByUserId",
                table: "StageDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StageDocuments_VerifiedByUserId",
                table: "StageDocuments",
                column: "VerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Users_AssignedAssessorId",
                table: "Shipments",
                column: "AssignedAssessorId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Users_AssignedCaseExecutorId",
                table: "Shipments",
                column: "AssignedCaseExecutorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Users_AssignedAssessorId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Users_AssignedCaseExecutorId",
                table: "Shipments");

            migrationBuilder.DropTable(
                name: "StageComment");

            migrationBuilder.DropTable(
                name: "StageDocuments");

            migrationBuilder.DropTable(
                name: "Transports");

            migrationBuilder.DropTable(
                name: "ServiceStageExecution");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_AssignedAssessorId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_AssignedCaseExecutorId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "AssignedAssessorId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "AssignedCaseExecutorId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "AssignmentNotes",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeclaredValue",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RouteCategory",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TaxCategory",
                table: "Shipments");
        }
    }
}
