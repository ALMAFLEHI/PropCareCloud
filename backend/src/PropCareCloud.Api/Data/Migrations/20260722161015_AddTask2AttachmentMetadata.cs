using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropCareCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTask2AttachmentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "maintenance_request_attachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_StorageKey",
                table: "maintenance_request_attachments",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maintenance_request_attachments_StorageKey",
                table: "maintenance_request_attachments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "maintenance_request_attachments");
        }
    }
}
