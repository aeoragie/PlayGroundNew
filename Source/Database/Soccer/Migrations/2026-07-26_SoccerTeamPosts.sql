-- 팀 게시판 (Design.TeamBoard) — 신규 테이블 3종. 멱등. **다른 PC 필수 실행.**
-- Tables/ 의 CREATE는 신규 DB에만 반영되므로 기존 로컬 DB는 이 스크립트로 따라오게 한다.

IF OBJECT_ID('dbo.SoccerTeamPosts', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SoccerTeamPosts]
    (
        [PostId]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [TeamId]     UNIQUEIDENTIFIER NOT NULL,
        [Type]       VARCHAR(20)      NOT NULL,
        [Title]      VARCHAR(300)     NOT NULL,
        [Body]       VARCHAR(6000)    NOT NULL,
        [IsPinned]   BIT              NOT NULL DEFAULT 0,
        [IsPublic]   BIT              NOT NULL DEFAULT 0,
        [AuthorId]   UNIQUEIDENTIFIER NOT NULL,
        [AuthorName] VARCHAR(300)     NULL,
        [EditedAt]   DATETIME2        NULL,
        [CreatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [DeletedAt]  DATETIME2        NULL,
        CONSTRAINT [PK_SoccerTeamPosts] PRIMARY KEY ([PostId])
    );
END

IF OBJECT_ID('dbo.SoccerTeamPostFiles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SoccerTeamPostFiles]
    (
        [FileId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [PostId]       UNIQUEIDENTIFIER NOT NULL,
        [FileUrl]      VARCHAR(400)     NOT NULL,
        [FileName]     VARCHAR(300)     NOT NULL,
        [SizeBytes]    BIGINT           NOT NULL DEFAULT 0,
        [DisplayOrder] INT              NOT NULL DEFAULT 0,
        [CreatedAt]    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SoccerTeamPostFiles] PRIMARY KEY ([FileId])
    );
END

IF OBJECT_ID('dbo.SoccerTeamPostReads', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SoccerTeamPostReads]
    (
        [PostId]  UNIQUEIDENTIFIER NOT NULL,
        [UserId]  UNIQUEIDENTIFIER NOT NULL,
        [ReadAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SoccerTeamPostReads] PRIMARY KEY ([PostId], [UserId])
    );
END
