-- Manual script to create the ContactUs table if EF Migrations are not used.
-- Target Database: APCDPortalDb

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactUs')
BEGIN
    CREATE TABLE [ContactUs] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NULL,
        [State] nvarchar(max) NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [CreateTime] datetime NOT NULL DEFAULT (getdate()),
        CONSTRAINT [PK_ContactUs] PRIMARY KEY ([Id])
    );
END
GO
