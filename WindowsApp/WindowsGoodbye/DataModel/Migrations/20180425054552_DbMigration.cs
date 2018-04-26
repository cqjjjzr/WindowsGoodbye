using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WindowsGoodbye.Migrations
{
    public partial class DbMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<Guid>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(nullable: false),
                    AuthKey = table.Column<byte[]>(nullable: true),
                    DeviceFriendlyName = table.Column<string>(nullable: true),
                    DeviceKey = table.Column<byte[]>(nullable: true),
                    DeviceMacAddress = table.Column<string>(nullable: true),
                    DeviceModelName = table.Column<string>(nullable: true),
                    Enabled = table.Column<bool>(nullable: false),
                    LastConnectedHost = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthRecords");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
