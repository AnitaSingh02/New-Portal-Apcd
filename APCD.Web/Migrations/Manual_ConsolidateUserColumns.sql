BEGIN TRANSACTION;

-- 1. Check if both columns exist. If so, drop the 'new' CompanyName column first.
IF COL_LENGTH('Users', 'CompanyName') IS NOT NULL AND COL_LENGTH('Users', 'FullName') IS NOT NULL
BEGIN
    -- Optional: Copy data from FullName to CompanyName if CompanyName is empty
    UPDATE Users SET CompanyName = FullName WHERE CompanyName IS NULL OR CompanyName = '';
    
    -- Drop the mistakenly added column
    ALTER TABLE Users DROP COLUMN CompanyName;
END

-- 2. Rename FullName to CompanyName
IF COL_LENGTH('Users', 'FullName') IS NOT NULL
BEGIN
    EXEC sp_rename 'Users.FullName', 'CompanyName', 'COLUMN';
END

COMMIT;
