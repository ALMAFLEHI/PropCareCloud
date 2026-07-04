using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropCareCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUnitAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_unit_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RentalUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseStartDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseEndDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_unit_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_unit_assignments_rental_units_RentalUnitId",
                        column: x => x.RentalUnitId,
                        principalTable: "rental_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_unit_assignments_user_profiles_TenantProfileId",
                        column: x => x.TenantProfileId,
                        principalTable: "user_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_RentalUnitId",
                table: "tenant_unit_assignments",
                column: "RentalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_TenantProfileId",
                table: "tenant_unit_assignments",
                column: "TenantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_unit_assignments_TenantProfileId_RentalUnitId_IsActi~",
                table: "tenant_unit_assignments",
                columns: new[] { "TenantProfileId", "RentalUnitId", "IsActive" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_unit_assignments");
        }
    }
}
