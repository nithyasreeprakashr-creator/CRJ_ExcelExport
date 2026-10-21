/*
    CRJ SQL Query Version History
    Run this script in SSMS against the SQL Server project database.

    Notes:
    - Stores query text and successful execution history in SQL Server only.
    - Revert re-executes the selected historical query and creates a NEW version.
    - Use only for trusted SQL scripts. Do not grant arbitrary users permission to execute arbitrary SQL.
    - Submit one SQL batch per execution. Do not include GO, USE, or transaction-control statements
      (BEGIN/COMMIT/ROLLBACK) inside @QueryText.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.CRJ_QueryVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CRJ_QueryVersion
    (
        VersionId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_CRJ_QueryVersion PRIMARY KEY,
        QueryName NVARCHAR(200) NOT NULL,
        VersionNo INT NOT NULL,
        QueryText NVARCHAR(MAX) NOT NULL,
        ChangeNote NVARCHAR(1000) NULL,
        ExecutedBy NVARCHAR(200) NOT NULL,
        ExecutedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_CRJ_QueryVersion_ExecutedAt DEFAULT SYSDATETIME(),
        IsRevert BIT NOT NULL
            CONSTRAINT DF_CRJ_QueryVersion_IsRevert DEFAULT (0),
        SourceVersionNo INT NULL,
        CONSTRAINT UQ_CRJ_QueryVersion_Name_Version
            UNIQUE (QueryName, VersionNo)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ExecuteAndVersionCRJQuery
    @QueryName NVARCHAR(200),
    @QueryText NVARCHAR(MAX),
    @ChangeNote NVARCHAR(1000) = NULL,
    @IsRevert BIT = 0,
    @SourceVersionNo INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@QueryName)), N'') IS NULL
        THROW 50001, 'QueryName is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@QueryText)), N'') IS NULL
        THROW 50002, 'QueryText is required.', 1;

    DECLARE @VersionNo INT;
    DECLARE @LockResult INT;
    DECLARE @LockName NVARCHAR(255);

    SET @LockName = N'CRJ_QueryVersion:' + @QueryName;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockName,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 50003, 'Could not acquire version lock.', 1;

        SELECT @VersionNo = ISNULL(MAX(VersionNo), 0) + 1
        FROM dbo.CRJ_QueryVersion WITH (UPDLOCK, HOLDLOCK)
        WHERE QueryName = @QueryName;

        EXEC sys.sp_executesql @QueryText;

        INSERT INTO dbo.CRJ_QueryVersion
        (
            QueryName, VersionNo, QueryText, ChangeNote,
            ExecutedBy, IsRevert, SourceVersionNo
        )
        VALUES
        (
            @QueryName, @VersionNo, @QueryText, @ChangeNote,
            ORIGINAL_LOGIN(), @IsRevert, @SourceVersionNo
        );

        COMMIT TRANSACTION;

        SELECT
            @QueryName AS QueryName,
            @VersionNo AS VersionNo,
            RIGHT('000' + CONVERT(VARCHAR(10), @VersionNo), 3)
                + N'_' + @QueryName AS VersionName,
            @IsRevert AS IsRevert,
            @SourceVersionNo AS SourceVersionNo,
            N'Success' AS ExecutionStatus;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetCRJQueryHistory
    @QueryName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        QueryName,
        VersionNo,
        RIGHT('000' + CONVERT(VARCHAR(10), VersionNo), 3)
            + N'_' + QueryName AS VersionName,
        ChangeNote,
        ExecutedBy,
        ExecutedAt,
        IsRevert,
        SourceVersionNo,
        QueryText
    FROM dbo.CRJ_QueryVersion
    WHERE QueryName = @QueryName
    ORDER BY VersionNo DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetCRJQueryVersion
    @QueryName NVARCHAR(200),
    @VersionNo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        QueryName, VersionNo, QueryText, ChangeNote,
        ExecutedBy, ExecutedAt, IsRevert, SourceVersionNo
    FROM dbo.CRJ_QueryVersion
    WHERE QueryName = @QueryName
      AND VersionNo = @VersionNo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RevertCRJQueryVersion
    @QueryName NVARCHAR(200),
    @TargetVersionNo INT,
    @ChangeNote NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @QueryText NVARCHAR(MAX);

    SELECT @QueryText = QueryText
    FROM dbo.CRJ_QueryVersion
    WHERE QueryName = @QueryName
      AND VersionNo = @TargetVersionNo;

    IF @QueryText IS NULL
        THROW 50010, 'Requested query version was not found.', 1;

    IF @ChangeNote IS NULL
        SET @ChangeNote = N'Re-executed version '
            + CONVERT(NVARCHAR(10), @TargetVersionNo);

    EXEC dbo.usp_ExecuteAndVersionCRJQuery
        @QueryName = @QueryName,
        @QueryText = @QueryText,
        @ChangeNote = @ChangeNote,
        @IsRevert = 1,
        @SourceVersionNo = @TargetVersionNo;
END;
GO

/*
    TEST EXAMPLES (run separately after the script above):

    EXEC dbo.usp_ExecuteAndVersionCRJQuery
        @QueryName = N'CRJ_dataEntry',
        @QueryText = N'SELECT 1 AS TestValue;',
        @ChangeNote = N'Initial version';

    EXEC dbo.usp_ExecuteAndVersionCRJQuery
        @QueryName = N'CRJ_dataEntry',
        @QueryText = N'SELECT 1 AS TestValue, GETDATE() AS RunDate;',
        @ChangeNote = N'Added run date';

    EXEC dbo.usp_GetCRJQueryHistory @QueryName = N'CRJ_dataEntry';

    EXEC dbo.usp_GetCRJQueryVersion
        @QueryName = N'CRJ_dataEntry', @VersionNo = 1;

    EXEC dbo.usp_RevertCRJQueryVersion
        @QueryName = N'CRJ_dataEntry', @TargetVersionNo = 1,
        @ChangeNote = N'Re-executed initial query';
*/
