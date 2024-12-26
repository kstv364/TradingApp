﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenFlagToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpen",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpen",
                table: "Orders");
        }
    }
}
