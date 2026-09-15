using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BMWMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ModuleCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionID);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                columns: table => new
                {
                    ProductAttributeID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UnitLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.ProductAttributeID);
                });

            migrationBuilder.CreateTable(
                name: "ProductGroups",
                columns: table => new
                {
                    ProductGroupID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentGroupID = table.Column<long>(type: "bigint", nullable: true),
                    GroupCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseUnitOfMeasureID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGroups", x => x.ProductGroupID);
                    table.ForeignKey(
                        name: "FK_ProductGroups_Parent",
                        column: x => x.ParentGroupID,
                        principalTable: "ProductGroups",
                        principalColumn: "ProductGroupID");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    UnitOfMeasureID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuantityScale = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.UnitOfMeasureID);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseID);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeOptions",
                columns: table => new
                {
                    ProductAttributeOptionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAttributeID = table.Column<long>(type: "bigint", nullable: false),
                    OptionCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    OptionValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeOptions", x => x.ProductAttributeOptionID);
                    table.ForeignKey(
                        name: "FK_ProductAttributeOptions_Attribute",
                        column: x => x.ProductAttributeID,
                        principalTable: "ProductAttributes",
                        principalColumn: "ProductAttributeID");
                });

            migrationBuilder.CreateTable(
                name: "ProductGroupAttributes",
                columns: table => new
                {
                    ProductGroupID = table.Column<long>(type: "bigint", nullable: false),
                    ProductAttributeID = table.Column<long>(type: "bigint", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGroupAttributes", x => new { x.ProductGroupID, x.ProductAttributeID });
                    table.ForeignKey(
                        name: "FK_ProductGroupAttributes_Attribute",
                        column: x => x.ProductAttributeID,
                        principalTable: "ProductAttributes",
                        principalColumn: "ProductAttributeID");
                    table.ForeignKey(
                        name: "FK_ProductGroupAttributes_Group",
                        column: x => x.ProductGroupID,
                        principalTable: "ProductGroups",
                        principalColumn: "ProductGroupID");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    PermissionID = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleID, x.PermissionID });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permission",
                        column: x => x.PermissionID,
                        principalTable: "Permissions",
                        principalColumn: "PermissionID");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Role",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Role",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseZones",
                columns: table => new
                {
                    ZoneID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ZoneCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductGroupID = table.Column<long>(type: "bigint", nullable: true),
                    MaxCapacityQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AreaSquareMeter = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaxVolumeM3 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseZones", x => x.ZoneID);
                    table.UniqueConstraint("AK_WarehouseZones_ZoneID_WarehouseID", x => new { x.ZoneID, x.WarehouseID });
                    table.ForeignKey(
                        name: "FK_WarehouseZones_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogID);
                    table.ForeignKey(
                        name: "FK_AuditLogs_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customers_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ReferenceID = table.Column<long>(type: "bigint", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ReadAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    PasswordResetTokenID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.PasswordResetTokenID);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductGroupID = table.Column<long>(type: "bigint", nullable: false),
                    UnitOfMeasureID = table.Column<int>(type: "int", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Barcode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RotationMethod = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "FIFO"),
                    TrackLot = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TrackExpiry = table.Column<bool>(type: "bit", nullable: false),
                    DefaultShelfLifeDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Products_Group",
                        column: x => x.ProductGroupID,
                        principalTable: "ProductGroups",
                        principalColumn: "ProductGroupID");
                    table.ForeignKey(
                        name: "FK_Products_Unit",
                        column: x => x.UnitOfMeasureID,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "UnitOfMeasureID");
                    table.ForeignKey(
                        name: "FK_Products_UpdatedBy",
                        column: x => x.UpdatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "StocktakeSchedules",
                columns: table => new
                {
                    StocktakeScheduleID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FrequencyType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: true),
                    DayOfMonth = table.Column<byte>(type: "tinyint", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextRunDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSchedules", x => x.StocktakeScheduleID);
                    table.ForeignKey(
                        name: "FK_StocktakeSchedules_User",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeSchedules_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierID);
                    table.ForeignKey(
                        name: "FK_Suppliers_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Suppliers_UpdatedBy",
                        column: x => x.UpdatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TransferOrders",
                columns: table => new
                {
                    TransferOrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TransferType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SourceWarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    DestinationWarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    AssignedToUserID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrders", x => x.TransferOrderID);
                    table.ForeignKey(
                        name: "FK_TransferOrders_AssignedTo",
                        column: x => x.AssignedToUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransferOrders_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransferOrders_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransferOrders_DestinationWarehouse",
                        column: x => x.DestinationWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                    table.ForeignKey(
                        name: "FK_TransferOrders_SourceWarehouse",
                        column: x => x.SourceWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "UserPasswordHistories",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserPass__4D7B4ADD5A11F68C", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_UserPasswordHistories_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    RefreshTokenHash = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_UserSessions_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "StorageRacks",
                columns: table => new
                {
                    RackID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ZoneID = table.Column<long>(type: "bigint", nullable: false),
                    RackCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    RackName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxCapacityQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AreaSquareMeter = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaxVolumeM3 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageRacks", x => x.RackID);
                    table.UniqueConstraint("AK_StorageRacks_RackID_WarehouseID", x => new { x.RackID, x.WarehouseID });
                    table.ForeignKey(
                        name: "FK_StorageRacks_ZoneWarehouse",
                        columns: x => new { x.ZoneID, x.WarehouseID },
                        principalTable: "WarehouseZones",
                        principalColumns: new[] { "ZoneID", "WarehouseID" });
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    SalesOrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedIssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AllocationStrategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.SalesOrderID);
                    table.ForeignKey(
                        name: "FK_SalesOrders_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalesOrders_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customer",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    ProductAttributeID = table.Column<long>(type: "bigint", nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => new { x.ProductID, x.ProductAttributeID });
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Attribute",
                        column: x => x.ProductAttributeID,
                        principalTable: "ProductAttributes",
                        principalColumn: "ProductAttributeID");
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductLots",
                columns: table => new
                {
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    LotNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FirstReceivedDate = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(CONVERT([date],sysutcdatetime()))"),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLots", x => x.ProductLotID);
                    table.UniqueConstraint("AK_ProductLots_ProductLotID_ProductID", x => new { x.ProductLotID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_ProductLots_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductWarehousePolicies",
                columns: table => new
                {
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    MinimumStockQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpiryWarningDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWarehousePolicies", x => new { x.ProductID, x.WarehouseID });
                    table.ForeignKey(
                        name: "FK_ProductWarehousePolicies_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_ProductWarehousePolicies_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "StocktakeSessions",
                columns: table => new
                {
                    StocktakeSessionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StocktakeNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    StocktakeScheduleID = table.Column<long>(type: "bigint", nullable: true),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "SCHEDULED"),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    AssignedToUserID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    StartedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ApprovedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSessions", x => x.StocktakeSessionID);
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_ApprovedBy",
                        column: x => x.ApprovedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_AssignedTo",
                        column: x => x.AssignedToUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_Schedule",
                        column: x => x.StocktakeScheduleID,
                        principalTable: "StocktakeSchedules",
                        principalColumn: "StocktakeScheduleID");
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SupplierID = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.PurchaseOrderID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Supplier",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                columns: table => new
                {
                    SupplierID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    SupplierProductCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LastPurchasePrice = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProducts", x => new { x.SupplierID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Supplier",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    RackID = table.Column<long>(type: "bigint", nullable: true),
                    LocationCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocationType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "BIN"),
                    MaxCapacityQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AreaSquareMeter = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaxVolumeM3 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsPutawayAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPickable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.StorageLocationID);
                    table.ForeignKey(
                        name: "FK_StorageLocations_RackWarehouse",
                        columns: x => new { x.RackID, x.WarehouseID },
                        principalTable: "StorageRacks",
                        principalColumns: new[] { "RackID", "WarehouseID" });
                    table.ForeignKey(
                        name: "FK_StorageLocations_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderDetails",
                columns: table => new
                {
                    SalesOrderDetailID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FulfilledQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderDetails", x => x.SalesOrderDetailID);
                    table.UniqueConstraint("AK_SalesOrderDetails_SalesOrderDetailID_ProductID", x => new { x.SalesOrderDetailID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_Order",
                        column: x => x.SalesOrderID,
                        principalTable: "SalesOrders",
                        principalColumn: "SalesOrderID");
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "InboundOrders",
                columns: table => new
                {
                    InboundOrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PurchaseOrderID = table.Column<long>(type: "bigint", nullable: true),
                    SalesOrderID = table.Column<long>(type: "bigint", nullable: true),
                    TransferOrderID = table.Column<long>(type: "bigint", nullable: true),
                    ParentInboundOrderID = table.Column<long>(type: "bigint", nullable: true),
                    ExpectedReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    AssignedToUserID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancelledByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrders", x => x.InboundOrderID);
                    table.ForeignKey(
                        name: "FK_InboundOrders_AssignedTo",
                        column: x => x.AssignedToUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_CancelledBy",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_Parent",
                        column: x => x.ParentInboundOrderID,
                        principalTable: "InboundOrders",
                        principalColumn: "InboundOrderID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_PurchaseOrder",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_SalesOrder",
                        column: x => x.SalesOrderID,
                        principalTable: "SalesOrders",
                        principalColumn: "SalesOrderID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_TransferOrder",
                        column: x => x.TransferOrderID,
                        principalTable: "TransferOrders",
                        principalColumn: "TransferOrderID");
                    table.ForeignKey(
                        name: "FK_InboundOrders_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrders",
                columns: table => new
                {
                    OutboundOrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SalesOrderID = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseOrderID = table.Column<long>(type: "bigint", nullable: true),
                    TransferOrderID = table.Column<long>(type: "bigint", nullable: true),
                    ExpectedIssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    AssignedToUserID = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletionType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CompletionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancelledByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrders", x => x.OutboundOrderID);
                    table.ForeignKey(
                        name: "FK_OutboundOrders_ApprovedBy",
                        column: x => x.ApprovedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_AssignedTo",
                        column: x => x.AssignedToUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_CancelledBy",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_CreatedBy",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_PurchaseOrder",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_SalesOrder",
                        column: x => x.SalesOrderID,
                        principalTable: "SalesOrders",
                        principalColumn: "SalesOrderID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_TransferOrder",
                        column: x => x.TransferOrderID,
                        principalTable: "TransferOrders",
                        principalColumn: "TransferOrderID");
                    table.ForeignKey(
                        name: "FK_OutboundOrders_Warehouse",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetails",
                columns: table => new
                {
                    PurchaseOrderDetailID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderDetails", x => x.PurchaseOrderDetailID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_Order",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderID");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    InventoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(19,4)", nullable: true, computedColumnSql: "([OnHandQuantity]-[ReservedQuantity])", stored: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_Inventory_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_Inventory_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_Inventory_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductFixedLocations",
                columns: table => new
                {
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFixedLocations", x => new { x.ProductID, x.StorageLocationID });
                    table.ForeignKey(
                        name: "FK_ProductFixedLocations_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_ProductFixedLocations_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "StocktakeLocations",
                columns: table => new
                {
                    StocktakeSessionID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    CountStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    CountedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CountedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeLocations", x => new { x.StocktakeSessionID, x.StorageLocationID });
                    table.ForeignKey(
                        name: "FK_StocktakeLocations_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_StocktakeLocations_Session",
                        column: x => x.StocktakeSessionID,
                        principalTable: "StocktakeSessions",
                        principalColumn: "StocktakeSessionID");
                    table.ForeignKey(
                        name: "FK_StocktakeLocations_User",
                        column: x => x.CountedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderDetails",
                columns: table => new
                {
                    TransferOrderDetailID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: true),
                    SourceLocationID = table.Column<long>(type: "bigint", nullable: true),
                    DestinationLocationID = table.Column<long>(type: "bigint", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MovedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StaffNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfirmedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrderDetails", x => x.TransferOrderDetailID);
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_ConfirmedBy",
                        column: x => x.ConfirmedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_Destination",
                        column: x => x.DestinationLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_Order",
                        column: x => x.TransferOrderID,
                        principalTable: "TransferOrders",
                        principalColumn: "TransferOrderID");
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_TransferOrderDetails_Source",
                        column: x => x.SourceLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                });

            migrationBuilder.CreateTable(
                name: "InboundOrderItems",
                columns: table => new
                {
                    InboundOrderItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DamagedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrderItems", x => x.InboundOrderItemID);
                    table.UniqueConstraint("AK_InboundOrderItems_InboundOrderItemID_InboundOrderID_ProductID", x => new { x.InboundOrderItemID, x.InboundOrderID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_InboundOrderItems_Order",
                        column: x => x.InboundOrderID,
                        principalTable: "InboundOrders",
                        principalColumn: "InboundOrderID");
                    table.ForeignKey(
                        name: "FK_InboundOrderItems_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrderItems",
                columns: table => new
                {
                    OutboundOrderItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrderItems", x => x.OutboundOrderItemID);
                    table.UniqueConstraint("AK_OutboundOrderItems_OutboundOrderItemID_OutboundOrderID_ProductID", x => new { x.OutboundOrderItemID, x.OutboundOrderID, x.ProductID });
                    table.UniqueConstraint("AK_OutboundOrderItems_OutboundOrderItemID_ProductID", x => new { x.OutboundOrderItemID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_OutboundOrderItems_Order",
                        column: x => x.OutboundOrderID,
                        principalTable: "OutboundOrders",
                        principalColumn: "OutboundOrderID");
                    table.ForeignKey(
                        name: "FK_OutboundOrderItems_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "StocktakeItems",
                columns: table => new
                {
                    StocktakeItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StocktakeSessionID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    BookQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DifferenceQuantity = table.Column<decimal>(type: "decimal(19,4)", nullable: true, computedColumnSql: "(case when [CountedQuantity] IS NULL then NULL else [CountedQuantity]-[BookQuantity] end)", stored: true),
                    AdjustmentQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Resolution = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CountedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CountedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ApprovedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeItems", x => x.StocktakeItemID);
                    table.ForeignKey(
                        name: "FK_StocktakeItems_ApprovedBy",
                        column: x => x.ApprovedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeItems_CountedBy",
                        column: x => x.CountedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakeItems_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_StocktakeItems_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_StocktakeItems_Session",
                        column: x => x.StocktakeSessionID,
                        principalTable: "StocktakeSessions",
                        principalColumn: "StocktakeSessionID");
                    table.ForeignKey(
                        name: "FK_StocktakeItems_SessionLocation",
                        columns: x => new { x.StocktakeSessionID, x.StorageLocationID },
                        principalTable: "StocktakeLocations",
                        principalColumns: new[] { "StocktakeSessionID", "StorageLocationID" });
                });

            migrationBuilder.CreateTable(
                name: "InboundOrderDetails",
                columns: table => new
                {
                    InboundOrderDetailID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundOrderItemID = table.Column<long>(type: "bigint", nullable: false),
                    InboundOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConditionStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "GOOD"),
                    RecordedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrderDetails", x => x.InboundOrderDetailID);
                    table.ForeignKey(
                        name: "FK_InboundOrderDetails_Item",
                        columns: x => new { x.InboundOrderItemID, x.InboundOrderID, x.ProductID },
                        principalTable: "InboundOrderItems",
                        principalColumns: new[] { "InboundOrderItemID", "InboundOrderID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InboundOrderDetails_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_InboundOrderDetails_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InboundOrderDetails_User",
                        column: x => x.RecordedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    InventoryReservationID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderDetailID = table.Column<long>(type: "bigint", nullable: true),
                    OutboundOrderItemID = table.Column<long>(type: "bigint", nullable: true),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    ReservedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.InventoryReservationID);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_DetailProduct",
                        columns: x => new { x.SalesOrderDetailID, x.ProductID },
                        principalTable: "SalesOrderDetails",
                        principalColumns: new[] { "SalesOrderDetailID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InventoryReservations_OutboundItemProduct",
                        columns: x => new { x.OutboundOrderItemID, x.ProductID },
                        principalTable: "OutboundOrderItems",
                        principalColumns: new[] { "OutboundOrderItemID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InventoryReservations_User",
                        column: x => x.ReservedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrderDetails",
                columns: table => new
                {
                    OutboundOrderDetailID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundOrderItemID = table.Column<long>(type: "bigint", nullable: false),
                    OutboundOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    InventoryReservationID = table.Column<long>(type: "bigint", nullable: true),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RecordedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrderDetails", x => x.OutboundOrderDetailID);
                    table.ForeignKey(
                        name: "FK_OutboundOrderDetails_Item",
                        columns: x => new { x.OutboundOrderItemID, x.OutboundOrderID, x.ProductID },
                        principalTable: "OutboundOrderItems",
                        principalColumns: new[] { "OutboundOrderItemID", "OutboundOrderID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_OutboundOrderDetails_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_OutboundOrderDetails_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_OutboundOrderDetails_Reservation",
                        column: x => x.InventoryReservationID,
                        principalTable: "InventoryReservations",
                        principalColumn: "InventoryReservationID");
                    table.ForeignKey(
                        name: "FK_OutboundOrderDetails_User",
                        column: x => x.RecordedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    InventoryTransactionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationID = table.Column<long>(type: "bigint", nullable: false),
                    ProductLotID = table.Column<long>(type: "bigint", nullable: false),
                    OnHandDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InboundOrderDetailID = table.Column<long>(type: "bigint", nullable: true),
                    OutboundOrderDetailID = table.Column<long>(type: "bigint", nullable: true),
                    TransferOrderDetailID = table.Column<long>(type: "bigint", nullable: true),
                    StocktakeItemID = table.Column<long>(type: "bigint", nullable: true),
                    InventoryReservationID = table.Column<long>(type: "bigint", nullable: true),
                    PerformedByUserID = table.Column<long>(type: "bigint", nullable: false),
                    TransactionAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.InventoryTransactionID);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InboundDetail",
                        column: x => x.InboundOrderDetailID,
                        principalTable: "InboundOrderDetails",
                        principalColumn: "InboundOrderDetailID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Location",
                        column: x => x.StorageLocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "StorageLocationID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_LotProduct",
                        columns: x => new { x.ProductLotID, x.ProductID },
                        principalTable: "ProductLots",
                        principalColumns: new[] { "ProductLotID", "ProductID" });
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OutboundDetail",
                        column: x => x.OutboundOrderDetailID,
                        principalTable: "OutboundOrderDetails",
                        principalColumn: "OutboundOrderDetailID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Product",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Reservation",
                        column: x => x.InventoryReservationID,
                        principalTable: "InventoryReservations",
                        principalColumn: "InventoryReservationID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_StocktakeItem",
                        column: x => x.StocktakeItemID,
                        principalTable: "StocktakeItems",
                        principalColumn: "StocktakeItemID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_TransferDetail",
                        column: x => x.TransferOrderDetailID,
                        principalTable: "TransferOrderDetails",
                        principalColumn: "TransferOrderDetailID");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_User",
                        column: x => x.PerformedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserID",
                table: "AuditLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedByUserID",
                table: "Customers",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Customers_Code",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Customers_TaxCode",
                table: "Customers",
                column: "TaxCode",
                unique: true,
                filter: "([TaxCode] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderDetails_InboundOrderItemID_InboundOrderID_ProductID",
                table: "InboundOrderDetails",
                columns: new[] { "InboundOrderItemID", "InboundOrderID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderDetails_ProductLotID_ProductID",
                table: "InboundOrderDetails",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderDetails_RecordedByUserID",
                table: "InboundOrderDetails",
                column: "RecordedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderDetails_StorageLocationID",
                table: "InboundOrderDetails",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderItems_ProductID",
                table: "InboundOrderItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_InboundOrderItems_Composite",
                table: "InboundOrderItems",
                columns: new[] { "InboundOrderItemID", "InboundOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_InboundOrderItems_Product",
                table: "InboundOrderItems",
                columns: new[] { "InboundOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_AssignedToUserID",
                table: "InboundOrders",
                column: "AssignedToUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_CancelledByUserID",
                table: "InboundOrders",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_ConfirmedByUserID",
                table: "InboundOrders",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_CreatedByUserID",
                table: "InboundOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_ParentInboundOrderID",
                table: "InboundOrders",
                column: "ParentInboundOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_PurchaseOrderID",
                table: "InboundOrders",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_SalesOrderID",
                table: "InboundOrders",
                column: "SalesOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_StatusDue",
                table: "InboundOrders",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_TransferOrderID",
                table: "InboundOrders",
                column: "TransferOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_WarehouseID",
                table: "InboundOrders",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "UQ_InboundOrders_Number",
                table: "InboundOrders",
                column: "InboundOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_LocationProduct",
                table: "Inventory",
                columns: new[] { "StorageLocationID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductAvailable",
                table: "Inventory",
                columns: new[] { "ProductID", "AvailableQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductLotID_ProductID",
                table: "Inventory",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "UQ_Inventory_Target",
                table: "Inventory",
                columns: new[] { "ProductID", "StorageLocationID", "ProductLotID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OutboundOrderItemID_ProductID",
                table: "InventoryReservations",
                columns: new[] { "OutboundOrderItemID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProductLotID_ProductID",
                table: "InventoryReservations",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ReservedByUserID",
                table: "InventoryReservations",
                column: "ReservedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_SalesOrderDetailID_ProductID",
                table: "InventoryReservations",
                columns: new[] { "SalesOrderDetailID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_StorageLocationID",
                table: "InventoryReservations",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryReservationID",
                table: "InventoryTransactions",
                column: "InventoryReservationID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_LocationDate",
                table: "InventoryTransactions",
                columns: new[] { "StorageLocationID", "TransactionAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PerformedByUserID",
                table: "InventoryTransactions",
                column: "PerformedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductDate",
                table: "InventoryTransactions",
                columns: new[] { "ProductID", "TransactionAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductLotID_ProductID",
                table: "InventoryTransactions",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_InboundDetail",
                table: "InventoryTransactions",
                column: "InboundOrderDetailID",
                unique: true,
                filter: "([InboundOrderDetailID] IS NOT NULL AND [TransactionType]='INBOUND')");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_OutboundDetail",
                table: "InventoryTransactions",
                column: "OutboundOrderDetailID",
                unique: true,
                filter: "([OutboundOrderDetailID] IS NOT NULL AND [TransactionType]='OUTBOUND')");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_StocktakeItem",
                table: "InventoryTransactions",
                column: "StocktakeItemID",
                unique: true,
                filter: "([StocktakeItemID] IS NOT NULL AND [TransactionType]='STOCKTAKE_ADJUSTMENT')");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_TransferType",
                table: "InventoryTransactions",
                columns: new[] { "TransferOrderDetailID", "TransactionType" },
                unique: true,
                filter: "([TransferOrderDetailID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserUnread",
                table: "Notifications",
                columns: new[] { "UserID", "IsRead", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderDetails_InventoryReservationID",
                table: "OutboundOrderDetails",
                column: "InventoryReservationID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderDetails_OutboundOrderItemID_OutboundOrderID_ProductID",
                table: "OutboundOrderDetails",
                columns: new[] { "OutboundOrderItemID", "OutboundOrderID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderDetails_ProductLotID_ProductID",
                table: "OutboundOrderDetails",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderDetails_RecordedByUserID",
                table: "OutboundOrderDetails",
                column: "RecordedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderDetails_StorageLocationID",
                table: "OutboundOrderDetails",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderItems_ProductID",
                table: "OutboundOrderItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_OutboundOrderItems_Composite",
                table: "OutboundOrderItems",
                columns: new[] { "OutboundOrderItemID", "OutboundOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_OutboundOrderItems_Product",
                table: "OutboundOrderItems",
                columns: new[] { "OutboundOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_ApprovedByUserID",
                table: "OutboundOrders",
                column: "ApprovedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_AssignedToUserID",
                table: "OutboundOrders",
                column: "AssignedToUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_CancelledByUserID",
                table: "OutboundOrders",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_ConfirmedByUserID",
                table: "OutboundOrders",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_CreatedByUserID",
                table: "OutboundOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_PurchaseOrderID",
                table: "OutboundOrders",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_SalesOrderID",
                table: "OutboundOrders",
                column: "SalesOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_StatusDue",
                table: "OutboundOrders",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_TransferOrderID",
                table: "OutboundOrders",
                column: "TransferOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_WarehouseID",
                table: "OutboundOrders",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "UQ_OutboundOrders_Number",
                table: "OutboundOrders",
                column: "OutboundOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserID",
                table: "PasswordResetTokens",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Permissions_PermissionCode",
                table: "Permissions",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ProductAttributeOptions_Code",
                table: "ProductAttributeOptions",
                columns: new[] { "ProductAttributeID", "OptionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ProductAttributes_Code",
                table: "ProductAttributes",
                column: "AttributeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductAttributeID",
                table: "ProductAttributeValues",
                column: "ProductAttributeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFixedLocations_StorageLocationID",
                table: "ProductFixedLocations",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "UX_ProductFixedLocations_Default",
                table: "ProductFixedLocations",
                column: "ProductID",
                unique: true,
                filter: "([IsDefault]=(1) AND [IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_ProductGroupAttributes_ProductAttributeID",
                table: "ProductGroupAttributes",
                column: "ProductAttributeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductGroups_ParentGroupID",
                table: "ProductGroups",
                column: "ParentGroupID");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductGroups_Code",
                table: "ProductGroups",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductLots_Expiry",
                table: "ProductLots",
                columns: new[] { "ExpiryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_ProductLots_LotProduct",
                table: "ProductLots",
                columns: new[] { "ProductLotID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ProductLots_ProductLot",
                table: "ProductLots",
                columns: new[] { "ProductID", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedByUserID",
                table: "Products",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductGroupID",
                table: "Products",
                column: "ProductGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitOfMeasureID",
                table: "Products",
                column: "UnitOfMeasureID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UpdatedByUserID",
                table: "Products",
                column: "UpdatedByUserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Products_Code",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "([Barcode] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehousePolicies_WarehouseID",
                table: "ProductWarehousePolicies",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_ProductID",
                table: "PurchaseOrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_PurchaseOrderDetails_IDProduct",
                table: "PurchaseOrderDetails",
                columns: new[] { "PurchaseOrderDetailID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PurchaseOrderDetails_Product",
                table: "PurchaseOrderDetails",
                columns: new[] { "PurchaseOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ConfirmedByUserID",
                table: "PurchaseOrders",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserID",
                table: "PurchaseOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "UQ_PurchaseOrders_Number",
                table: "PurchaseOrders",
                column: "PurchaseOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionID",
                table: "RolePermissions",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleCode",
                table: "Roles",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_ProductID",
                table: "SalesOrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrderDetails_IDProduct",
                table: "SalesOrderDetails",
                columns: new[] { "SalesOrderDetailID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrderDetails_Product",
                table: "SalesOrderDetails",
                columns: new[] { "SalesOrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ConfirmedByUserID",
                table: "SalesOrders",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedByUserID",
                table: "SalesOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerID",
                table: "SalesOrders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrders_Number",
                table: "SalesOrders",
                column: "SalesOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_ApprovedByUserID",
                table: "StocktakeItems",
                column: "ApprovedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_CountedByUserID",
                table: "StocktakeItems",
                column: "CountedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_ProductLotID_ProductID",
                table: "StocktakeItems",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_StorageLocationID",
                table: "StocktakeItems",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "UQ_StocktakeItems_Target",
                table: "StocktakeItems",
                columns: new[] { "StocktakeSessionID", "StorageLocationID", "ProductID", "ProductLotID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeLocations_CountedByUserID",
                table: "StocktakeLocations",
                column: "CountedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeLocations_StorageLocationID",
                table: "StocktakeLocations",
                column: "StorageLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSchedules_CreatedByUserID",
                table: "StocktakeSchedules",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSchedules_WarehouseID",
                table: "StocktakeSchedules",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_ApprovedByUserID",
                table: "StocktakeSessions",
                column: "ApprovedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_AssignedToUserID",
                table: "StocktakeSessions",
                column: "AssignedToUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_CreatedByUserID",
                table: "StocktakeSessions",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_StocktakeScheduleID",
                table: "StocktakeSessions",
                column: "StocktakeScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_WarehouseID",
                table: "StocktakeSessions",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "UQ_StocktakeSessions_Number",
                table: "StocktakeSessions",
                column: "StocktakeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_RackID_WarehouseID",
                table: "StorageLocations",
                columns: new[] { "RackID", "WarehouseID" });

            migrationBuilder.CreateIndex(
                name: "UQ_StorageLocations_Code",
                table: "StorageLocations",
                columns: new[] { "WarehouseID", "LocationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StorageLocations_LocationWarehouse",
                table: "StorageLocations",
                columns: new[] { "StorageLocationID", "WarehouseID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageRacks_ZoneID_WarehouseID",
                table: "StorageRacks",
                columns: new[] { "ZoneID", "WarehouseID" });

            migrationBuilder.CreateIndex(
                name: "UQ_StorageRacks_Code",
                table: "StorageRacks",
                columns: new[] { "WarehouseID", "RackCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StorageRacks_RackWarehouse",
                table: "StorageRacks",
                columns: new[] { "RackID", "WarehouseID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_ProductID",
                table: "SupplierProducts",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedByUserID",
                table: "Suppliers",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UpdatedByUserID",
                table: "Suppliers",
                column: "UpdatedByUserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Suppliers_Code",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Suppliers_TaxCode",
                table: "Suppliers",
                column: "TaxCode",
                unique: true,
                filter: "[TaxCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_ConfirmedByUserID",
                table: "TransferOrderDetails",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_DestinationLocationID",
                table: "TransferOrderDetails",
                column: "DestinationLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_ProductID",
                table: "TransferOrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_ProductLotID_ProductID",
                table: "TransferOrderDetails",
                columns: new[] { "ProductLotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_SourceLocationID",
                table: "TransferOrderDetails",
                column: "SourceLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderDetails_TransferOrderID",
                table: "TransferOrderDetails",
                column: "TransferOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_AssignedToUserID",
                table: "TransferOrders",
                column: "AssignedToUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_ConfirmedByUserID",
                table: "TransferOrders",
                column: "ConfirmedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_CreatedByUserID",
                table: "TransferOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_DestinationWarehouseID",
                table: "TransferOrders",
                column: "DestinationWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_SourceWarehouseID",
                table: "TransferOrders",
                column: "SourceWarehouseID");

            migrationBuilder.CreateIndex(
                name: "UQ_TransferOrders_Number",
                table: "TransferOrders",
                column: "TransferOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UnitsOfMeasure_Code",
                table: "UnitsOfMeasure",
                column: "UnitCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordHistories_UserID",
                table: "UserPasswordHistories",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserID",
                table: "UserSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserSessions_TokenHash",
                table: "UserSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Warehouses_Code",
                table: "Warehouses",
                column: "WarehouseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Warehouses_Primary",
                table: "Warehouses",
                column: "IsPrimary",
                unique: true,
                filter: "([IsPrimary]=(1))");

            migrationBuilder.CreateIndex(
                name: "UQ_WarehouseZones_Code",
                table: "WarehouseZones",
                columns: new[] { "WarehouseID", "ZoneCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_WarehouseZones_ZoneWarehouse",
                table: "WarehouseZones",
                columns: new[] { "ZoneID", "WarehouseID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "ProductAttributeOptions");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductFixedLocations");

            migrationBuilder.DropTable(
                name: "ProductGroupAttributes");

            migrationBuilder.DropTable(
                name: "ProductWarehousePolicies");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SupplierProducts");

            migrationBuilder.DropTable(
                name: "UserPasswordHistories");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "InboundOrderDetails");

            migrationBuilder.DropTable(
                name: "OutboundOrderDetails");

            migrationBuilder.DropTable(
                name: "StocktakeItems");

            migrationBuilder.DropTable(
                name: "TransferOrderDetails");

            migrationBuilder.DropTable(
                name: "ProductAttributes");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "InboundOrderItems");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropTable(
                name: "StocktakeLocations");

            migrationBuilder.DropTable(
                name: "InboundOrders");

            migrationBuilder.DropTable(
                name: "SalesOrderDetails");

            migrationBuilder.DropTable(
                name: "ProductLots");

            migrationBuilder.DropTable(
                name: "OutboundOrderItems");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropTable(
                name: "StocktakeSessions");

            migrationBuilder.DropTable(
                name: "OutboundOrders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "StorageRacks");

            migrationBuilder.DropTable(
                name: "StocktakeSchedules");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "TransferOrders");

            migrationBuilder.DropTable(
                name: "ProductGroups");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "WarehouseZones");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
