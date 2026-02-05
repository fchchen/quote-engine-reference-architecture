using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "quote");

            migrationBuilder.CreateTable(
                name: "Businesses",
                schema: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DbaName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    BusinessType = table.Column<int>(type: "int", nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateEstablished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeCount = table.Column<int>(type: "int", nullable: true),
                    AnnualRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AnnualPayroll = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationCodes",
                schema: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    HazardGroup = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateTables",
                schema: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    MinPremium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StateTaxRate = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                schema: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CoverageLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deductible = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BasePremium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAdjustments = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StateTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PolicyFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPremium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    RiskTier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuoteExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "quote",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                schema: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CoverageLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deductible = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualPremium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BoundDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "quote",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "quote",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_BusinessName",
                schema: "quote",
                table: "Businesses",
                column: "BusinessName");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_IsActive",
                schema: "quote",
                table: "Businesses",
                column: "IsActive",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_StateCode_BusinessType",
                schema: "quote",
                table: "Businesses",
                columns: new[] { "StateCode", "BusinessType" });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_TaxId",
                schema: "quote",
                table: "Businesses",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationCodes_Code_ProductType",
                schema: "quote",
                table: "ClassificationCodes",
                columns: new[] { "Code", "ProductType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationCodes_ProductType",
                schema: "quote",
                table: "ClassificationCodes",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_BusinessId",
                schema: "quote",
                table: "Policies",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_BusinessId_Status_ExpirationDate",
                schema: "quote",
                table: "Policies",
                columns: new[] { "BusinessId", "Status", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyNumber",
                schema: "quote",
                table: "Policies",
                column: "PolicyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_QuoteId",
                schema: "quote",
                table: "Policies",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_BusinessId",
                schema: "quote",
                table: "Quotes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_BusinessId_ProductType_Status",
                schema: "quote",
                table: "Quotes",
                columns: new[] { "BusinessId", "ProductType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteDate",
                schema: "quote",
                table: "Quotes",
                column: "QuoteDate");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteNumber",
                schema: "quote",
                table: "Quotes",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateTables_EffectiveDate_ExpirationDate",
                schema: "quote",
                table: "RateTables",
                columns: new[] { "EffectiveDate", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RateTables_StateCode_ClassCode_ProductType",
                schema: "quote",
                table: "RateTables",
                columns: new[] { "StateCode", "ClassificationCode", "ProductType" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassificationCodes",
                schema: "quote");

            migrationBuilder.DropTable(
                name: "Policies",
                schema: "quote");

            migrationBuilder.DropTable(
                name: "RateTables",
                schema: "quote");

            migrationBuilder.DropTable(
                name: "Quotes",
                schema: "quote");

            migrationBuilder.DropTable(
                name: "Businesses",
                schema: "quote");
        }
    }
}
