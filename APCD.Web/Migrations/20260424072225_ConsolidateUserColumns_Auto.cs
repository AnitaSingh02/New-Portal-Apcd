using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class ConsolidateUserColumns_Auto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'CompanyName') IS NOT NULL AND COL_LENGTH('Users', 'FullName') IS NOT NULL
                BEGIN
                    -- Copy data
                    UPDATE Users SET CompanyName = FullName WHERE CompanyName IS NULL OR CompanyName = '';
                    
                    -- Drop default constraint if it exists
                    DECLARE @ConstraintName nvarchar(200)
                    SELECT @ConstraintName = Name FROM sys.default_constraints 
                    WHERE parent_object_id = OBJECT_ID('Users') AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('Users'), 'CompanyName', 'ColumnId')
                    
                    IF @ConstraintName IS NOT NULL
                        EXEC('ALTER TABLE Users DROP CONSTRAINT ' + @ConstraintName)

                    -- Drop column
                    ALTER TABLE Users DROP COLUMN CompanyName;
                END
                
                IF COL_LENGTH('Users', 'FullName') IS NOT NULL
                BEGIN
                    EXEC sp_rename 'Users.FullName', 'CompanyName', 'COLUMN';
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationId",
                table: "Applications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationId",
                table: "Applications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
