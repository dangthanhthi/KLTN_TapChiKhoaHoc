SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.JournalWorkflowRecord','U') IS NULL
BEGIN
    CREATE TABLE dbo.JournalWorkflowRecord (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserId INT NULL,
        ArticleId INT NULL,
        Kind NVARCHAR(40) NOT NULL,
        State NVARCHAR(30) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        UpdatedUtc DATETIME2 NOT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT CK_JournalWorkflowRecord_Json CHECK (ISJSON(Payload)=1)
    );
    CREATE INDEX IX_JournalWorkflowRecord_Kind_UserId_State ON dbo.JournalWorkflowRecord(Kind,UserId,State);
    CREATE INDEX IX_JournalWorkflowRecord_ArticleId_Kind ON dbo.JournalWorkflowRecord(ArticleId,Kind);
END;
IF OBJECT_ID('dbo.JournalWorkflowFile','U') IS NULL
BEGIN
    CREATE TABLE dbo.JournalWorkflowFile (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RecordId UNIQUEIDENTIFIER NOT NULL,
        UserId INT NOT NULL,
        ArticleId INT NULL,
        Kind NVARCHAR(40) NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        Path NVARCHAR(500) NOT NULL,
        Size BIGINT NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_JournalWorkflowFile_Record FOREIGN KEY(RecordId) REFERENCES dbo.JournalWorkflowRecord(Id) ON DELETE CASCADE,
        CONSTRAINT CK_JournalWorkflowFile_Size CHECK(Size > 0)
    );
    CREATE INDEX IX_JournalWorkflowFile_RecordId ON dbo.JournalWorkflowFile(RecordId);
END;
COMMIT;
