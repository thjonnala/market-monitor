using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mm_AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mm_Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CashBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InitialCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mm_Symbols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Sector = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsCurated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_Symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mm_WatchlistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_WatchlistItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mm_AspNetRoleClaims_mm_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "mm_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mm_AspNetUserClaims_mm_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "mm_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_mm_AspNetUserLogins_mm_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "mm_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_mm_AspNetUserRoles_mm_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "mm_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mm_AspNetUserRoles_mm_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "mm_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_mm_AspNetUserTokens_mm_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "mm_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_Holdings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PortfolioId = table.Column<int>(type: "integer", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_Holdings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mm_Holdings_mm_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "mm_Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mm_Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PortfolioId = table.Column<int>(type: "integer", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mm_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mm_Trades_mm_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "mm_Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mm_AspNetRoleClaims_RoleId",
                table: "mm_AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "mm_AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mm_AspNetUserClaims_UserId",
                table: "mm_AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mm_AspNetUserLogins_UserId",
                table: "mm_AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mm_AspNetUserRoles_RoleId",
                table: "mm_AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "mm_AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "mm_AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mm_Holdings_PortfolioId_Ticker",
                table: "mm_Holdings",
                columns: new[] { "PortfolioId", "Ticker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mm_Portfolios_UserId",
                table: "mm_Portfolios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mm_Symbols_Ticker",
                table: "mm_Symbols",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mm_Trades_PortfolioId",
                table: "mm_Trades",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_mm_WatchlistItems_UserId_Ticker",
                table: "mm_WatchlistItems",
                columns: new[] { "UserId", "Ticker" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mm_AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "mm_AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "mm_AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "mm_AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "mm_AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "mm_Holdings");

            migrationBuilder.DropTable(
                name: "mm_Symbols");

            migrationBuilder.DropTable(
                name: "mm_Trades");

            migrationBuilder.DropTable(
                name: "mm_WatchlistItems");

            migrationBuilder.DropTable(
                name: "mm_AspNetRoles");

            migrationBuilder.DropTable(
                name: "mm_AspNetUsers");

            migrationBuilder.DropTable(
                name: "mm_Portfolios");
        }
    }
}
