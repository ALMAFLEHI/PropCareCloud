using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropCareCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenTenantUnitAssignmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_unit_assignments_RentalUnitId",
                table: "tenant_unit_assignments");

            migrationBuilder.DropIndex(
                name: "IX_tenant_unit_assignments_TenantProfileId_RentalUnitId_IsActi~",
                table: "tenant_unit_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_RentalUnitId",
                table: "tenant_unit_assignments",
                column: "RentalUnitId",
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"LeaseEndDateUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_unit_assignments_RentalUnitId",
                table: "tenant_unit_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_RentalUnitId",
                table: "tenant_unit_assignments",
                column: "RentalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_TenantProfileId_RentalUnitId_IsActi~",
                table: "tenant_unit_assignments",
                columns: new[] { "TenantProfileId", "RentalUnitId", "IsActive" },
                unique: true);
        }
    }
}
