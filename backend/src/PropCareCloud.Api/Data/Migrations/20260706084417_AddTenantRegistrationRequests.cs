using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropCareCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRegistrationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_registration_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RequestedPropertyOrUnit = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedUserProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedRentalUnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_registration_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_registration_requests_rental_units_ApprovedRentalUni~",
                        column: x => x.ApprovedRentalUnitId,
                        principalTable: "rental_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_registration_requests_user_profiles_ApprovedUserProf~",
                        column: x => x.ApprovedUserProfileId,
                        principalTable: "user_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_registration_requests_user_profiles_ReviewedByUserPr~",
                        column: x => x.ReviewedByUserProfileId,
                        principalTable: "user_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_ApprovedRentalUnitId",
                table: "tenant_registration_requests",
                column: "ApprovedRentalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_ApprovedUserProfileId",
                table: "tenant_registration_requests",
                column: "ApprovedUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_Email",
                table: "tenant_registration_requests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_Email_Status",
                table: "tenant_registration_requests",
                columns: new[] { "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_ReviewedByUserProfileId",
                table: "tenant_registration_requests",
                column: "ReviewedByUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_requests_Status",
                table: "tenant_registration_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_registration_requests");
        }
    }
}
