using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkingEmbeddingBillingUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentLog_Documents_DocumentId",
                table: "DocumentLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Subjects_SubjectId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Subjects_SubjectId",
                table: "LecturerSubject");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Users_LecturerId",
                table: "LecturerSubject");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId",
                table: "StudentSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Users_StudentId",
                table: "StudentSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LecturerSubject",
                table: "LecturerSubject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentLog",
                table: "DocumentLog");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "DocumentChunks");

            migrationBuilder.RenameTable(
                name: "LecturerSubject",
                newName: "LecturerSubjects");

            migrationBuilder.RenameTable(
                name: "DocumentLog",
                newName: "DocumentLogs");

            migrationBuilder.RenameColumn(
                name: "DocumentId",
                table: "DocumentChunks",
                newName: "EmbeddingSetId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_DocumentId",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_EmbeddingSetId");

            migrationBuilder.RenameIndex(
                name: "IX_LecturerSubject_SubjectId",
                table: "LecturerSubjects",
                newName: "IX_LecturerSubjects_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentLog_DocumentId",
                table: "DocumentLogs",
                newName: "IX_DocumentLogs_DocumentId");

            migrationBuilder.Sql(@"
    UPDATE Users SET Role = 
        CASE Role
            WHEN 'Admin' THEN '1'
            WHEN 'Lecturer' THEN '2'
            WHEN 'Student' THEN '3'
            ELSE Role
        END
");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "EmbeddingSetId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Documents",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParentDocumentId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LecturerSubjects",
                table: "LecturerSubjects",
                columns: new[] { "LecturerId", "SubjectId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentLogs",
                table: "DocumentLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ChunkingConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChunkSize = table.Column<int>(type: "int", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false),
                    Strategy = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkingConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChunkingConfigs_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChunkingConfigs_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelComparisonLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Feature = table.Column<int>(type: "int", nullable: false),
                    TestInput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LatencyAMs = table.Column<int>(type: "int", nullable: false),
                    TokensA = table.Column<int>(type: "int", nullable: false),
                    ModelB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LatencyBMs = table.Column<int>(type: "int", nullable: false),
                    TokensB = table.Column<int>(type: "int", nullable: false),
                    WinnerModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluationNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelComparisonLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    TokenQuota = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Feature = table.Column<int>(type: "int", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    ChatSessionId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmbeddingSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChunkingConfigId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbeddingSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbeddingSets_ChunkingConfigs_ChunkingConfigId",
                        column: x => x.ChunkingConfigId,
                        principalTable: "ChunkingConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmbeddingSets_Documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GatewayResponseRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemainingTokenQuota = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EmbeddingSetId",
                table: "Documents",
                column: "EmbeddingSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileHash",
                table: "Documents",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ParentDocumentId",
                table: "Documents",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkingConfigs_SubjectId",
                table: "ChunkingConfigs",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkingConfigs_UpdatedByUserId",
                table: "ChunkingConfigs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbeddingSets_ChunkingConfigId",
                table: "EmbeddingSets",
                column: "ChunkingConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbeddingSets_FileHash_ChunkingConfigId",
                table: "EmbeddingSets",
                columns: new[] { "FileHash", "ChunkingConfigId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmbeddingSets_SourceDocumentId",
                table: "EmbeddingSets",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PackageId",
                table: "Orders",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TransactionCode",
                table: "Orders",
                column: "TransactionCode",
                unique: true,
                filter: "[TransactionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_UserId_CreatedAt",
                table: "UsageLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_OrderId",
                table: "UserSubscriptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PackageId",
                table: "UserSubscriptions",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.Sql("DELETE FROM [DocumentLogs];");
            migrationBuilder.Sql("DELETE FROM [DocumentChunks];");
            migrationBuilder.Sql("DELETE FROM [Documents];");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_EmbeddingSets_EmbeddingSetId",
                table: "DocumentChunks",
                column: "EmbeddingSetId",
                principalTable: "EmbeddingSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentLogs_Documents_DocumentId",
                table: "DocumentLogs",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Documents_ParentDocumentId",
                table: "Documents",
                column: "ParentDocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_EmbeddingSets_EmbeddingSetId",
                table: "Documents",
                column: "EmbeddingSetId",
                principalTable: "EmbeddingSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Subjects_SubjectId",
                table: "Documents",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubjects_Subjects_SubjectId",
                table: "LecturerSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubjects_Users_LecturerId",
                table: "LecturerSubjects",
                column: "LecturerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId",
                table: "StudentSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Users_StudentId",
                table: "StudentSubjects",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_EmbeddingSets_EmbeddingSetId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentLogs_Documents_DocumentId",
                table: "DocumentLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Documents_ParentDocumentId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_EmbeddingSets_EmbeddingSetId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Subjects_SubjectId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubjects_Subjects_SubjectId",
                table: "LecturerSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubjects_Users_LecturerId",
                table: "LecturerSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId",
                table: "StudentSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Users_StudentId",
                table: "StudentSubjects");

            migrationBuilder.DropTable(
                name: "EmbeddingSets");

            migrationBuilder.DropTable(
                name: "ModelComparisonLogs");

            migrationBuilder.DropTable(
                name: "UsageLogs");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "ChunkingConfigs");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Documents_EmbeddingSetId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_FileHash",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ParentDocumentId",
                table: "Documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LecturerSubjects",
                table: "LecturerSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentLogs",
                table: "DocumentLogs");

            migrationBuilder.DropColumn(
                name: "EmbeddingSetId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ParentDocumentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Documents");

            migrationBuilder.RenameTable(
                name: "LecturerSubjects",
                newName: "LecturerSubject");

            migrationBuilder.RenameTable(
                name: "DocumentLogs",
                newName: "DocumentLog");

            migrationBuilder.RenameColumn(
                name: "EmbeddingSetId",
                table: "DocumentChunks",
                newName: "DocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_EmbeddingSetId",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_DocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_LecturerSubjects_SubjectId",
                table: "LecturerSubject",
                newName: "IX_LecturerSubject_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentLogs_DocumentId",
                table: "DocumentLog",
                newName: "IX_DocumentLog_DocumentId");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LecturerSubject",
                table: "LecturerSubject",
                columns: new[] { "LecturerId", "SubjectId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentLog",
                table: "DocumentLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentLog_Documents_DocumentId",
                table: "DocumentLog",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Subjects_SubjectId",
                table: "Documents",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Subjects_SubjectId",
                table: "LecturerSubject",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Users_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId",
                table: "StudentSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Users_StudentId",
                table: "StudentSubjects",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
