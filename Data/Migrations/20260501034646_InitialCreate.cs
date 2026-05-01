using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarAssemblyErp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SafetyStock = table.Column<int>(type: "integer", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                    table.CheckConstraint("CK_Part_SafetyStock", "\"SafetyStock\" >= 0");
                    table.CheckConstraint("CK_Part_StockQuantity", "\"StockQuantity\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Workstations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workstations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomNodes", x => x.Id);
                    table.CheckConstraint("CK_BomNode_NoSelfRef", "\"ParentPartId\" <> \"ChildPartId\"");
                    table.CheckConstraint("CK_BomNode_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_BomNodes_Parts_ChildPartId",
                        column: x => x.ChildPartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomNodes_Parts_ParentPartId",
                        column: x => x.ParentPartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RunningBalance = table.Column<int>(type: "integer", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.CheckConstraint("CK_InventoryTransaction_Quantity", "\"Quantity\" <> 0");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.CheckConstraint("CK_ProductionOrder_CompletedQuantity", "\"CompletedQuantity\" >= 0 AND \"CompletedQuantity\" <= \"Quantity\"");
                    table.CheckConstraint("CK_ProductionOrder_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Parts_TargetPartId",
                        column: x => x.TargetPartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Workstations_WorkstationId",
                        column: x => x.WorkstationId,
                        principalTable: "Workstations",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Parts",
                columns: new[] { "Id", "CreatedAt", "Name", "SafetyStock", "Sku", "Specification", "StockQuantity", "Unit" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "底盘", 5, "CHASSIS-001", "标准底盘", 10, "个" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "轮胎", 20, "TIRE-001", "标准轮胎", 100, "个" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "引擎", 2, "ENGINE-001", "V6引擎", 5, "个" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "活塞", 10, "PISTON-001", "标准活塞", 20, "个" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "螺丝", 100, "SCREW-001", "M8螺丝", 500, "个" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "整车", 1, "CAR-001", "标准轿车", 0, "辆" }
                });

            migrationBuilder.InsertData(
                table: "Workstations",
                columns: new[] { "Id", "IsActive", "Location", "Name" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), true, "车间1", "总装线A" });

            migrationBuilder.InsertData(
                table: "BomNodes",
                columns: new[] { "Id", "ChildPartId", "ParentPartId", "Quantity" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("66666666-6666-6666-6666-666666666666"), 1 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("66666666-6666-6666-6666-666666666666"), 4 },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("66666666-6666-6666-6666-666666666666"), 1 },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("33333333-3333-3333-3333-333333333333"), 4 },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new Guid("55555555-5555-5555-5555-555555555555"), new Guid("33333333-3333-3333-3333-333333333333"), 12 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomNodes_ChildPartId",
                table: "BomNodes",
                column: "ChildPartId");

            migrationBuilder.CreateIndex(
                name: "IX_BomNodes_ParentPartId_ChildPartId",
                table: "BomNodes",
                columns: new[] { "ParentPartId", "ChildPartId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PartId",
                table: "InventoryTransactions",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Part_Sku",
                table: "Parts",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                table: "ProductionOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_TargetPartId",
                table: "ProductionOrders",
                column: "TargetPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_WorkstationId",
                table: "ProductionOrders",
                column: "WorkstationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomNodes");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "Workstations");
        }
    }
}
