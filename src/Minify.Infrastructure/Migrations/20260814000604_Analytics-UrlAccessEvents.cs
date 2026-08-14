using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsUrlAccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrlAccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortCode = table.Column<string>(type: "text", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "text", nullable: false),
                    Browser = table.Column<string>(type: "text", nullable: false),
                    OperatingSystem = table.Column<string>(type: "text", nullable: false),
                    TrafficSource = table.Column<string>(type: "text", nullable: false),
                    UtmSource = table.Column<string>(type: "text", nullable: true),
                    UtmMedium = table.Column<string>(type: "text", nullable: true),
                    UtmCampaign = table.Column<string>(type: "text", nullable: true),
                    Referer = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlAccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlAccessEvents_ShortUrls_ShortCode",
                        column: x => x.ShortCode,
                        principalTable: "ShortUrls",
                        principalColumn: "ShortenCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrlAccessEvents_ShortCode_AccessedAt",
                table: "UrlAccessEvents",
                columns: new[] { "ShortCode", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UrlAccessEvents_ShortCode_Country",
                table: "UrlAccessEvents",
                columns: new[] { "ShortCode", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_UrlAccessEvents_ShortCode_TrafficSource",
                table: "UrlAccessEvents",
                columns: new[] { "ShortCode", "TrafficSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrlAccessEvents");
        }
    }
}
