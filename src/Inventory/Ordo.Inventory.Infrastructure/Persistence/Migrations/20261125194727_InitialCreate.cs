using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordo.Inventory.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                rejection_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                total_minor = table.Column<long>(type: "bigint", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reservations", x => x.order_id);
            });

        migrationBuilder.CreateTable(
            name: "stock_items",
            columns: table => new
            {
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                available_quantity = table.Column<int>(type: "integer", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stock_items", x => x.product_id);
            });

        migrationBuilder.CreateTable(
            name: "reservation_lines",
            columns: table => new
            {
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reservation_lines", x => new { x.order_id, x.product_id });
                table.ForeignKey(
                    name: "FK_reservation_lines_reservations_order_id",
                    column: x => x.order_id,
                    principalTable: "reservations",
                    principalColumn: "order_id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "reservation_lines");

        migrationBuilder.DropTable(
            name: "stock_items");

        migrationBuilder.DropTable(
            name: "reservations");
    }
}
