IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [DateRegistered] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [ReliefProjects] (
        [Id] int NOT NULL IDENTITY,
        [CreatedByUserId] nvarchar(450) NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Location] nvarchar(100) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        [FundsRequired] decimal(18,2) NOT NULL,
        [FundsRaised] decimal(18,2) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ReliefProjects] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ReliefProject_Status] CHECK ([Status] IN ('Planned','Active','Completed','Suspended')),
        CONSTRAINT [FK_ReliefProjects_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [Volunteers] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Address] nvarchar(200) NULL,
        [City] nvarchar(100) NULL,
        [Province] nvarchar(50) NULL,
        [PostalCode] nvarchar(10) NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Skills] nvarchar(max) NOT NULL,
        [Availability] nvarchar(max) NOT NULL,
        [EmergencyContactName] nvarchar(max) NULL,
        [EmergencyContactPhone] nvarchar(max) NULL,
        [Status] nvarchar(50) NOT NULL,
        [ApplicationDate] datetime2 NOT NULL,
        [ApprovalDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_Volunteers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Volunteer_Status] CHECK ([Status] IN ('Pending','Approved','Active','Inactive')),
        CONSTRAINT [FK_Volunteers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [Donations] (
        [Id] int NOT NULL IDENTITY,
        [DonorId] nvarchar(450) NULL,
        [ReliefProjectId] int NULL,
        [DonationType] nvarchar(20) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DonationDate] datetime2 NOT NULL,
        [PaymentMethod] nvarchar(50) NOT NULL,
        [TransactionReference] nvarchar(100) NULL,
        [IsAnonymous] bit NOT NULL,
        [TaxCertificateIssued] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_Donations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Donation_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Donation_Currency] CHECK ([Currency] IN ('ZAR','USD','EUR')),
        CONSTRAINT [CK_Donation_Type] CHECK ([DonationType] IN ('OneTime','Recurring')),
        CONSTRAINT [FK_Donations_AspNetUsers_DonorId] FOREIGN KEY ([DonorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Donations_ReliefProjects_ReliefProjectId] FOREIGN KEY ([ReliefProjectId]) REFERENCES [ReliefProjects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [ProjectUpdates] (
        [Id] int NOT NULL IDENTITY,
        [ReliefProjectId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [PostedByUserId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_ProjectUpdates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectUpdates_AspNetUsers_PostedByUserId] FOREIGN KEY ([PostedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectUpdates_ReliefProjects_ReliefProjectId] FOREIGN KEY ([ReliefProjectId]) REFERENCES [ReliefProjects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE TABLE [VolunteerAssignments] (
        [Id] int NOT NULL IDENTITY,
        [VolunteerId] int NOT NULL,
        [ReliefProjectId] int NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_VolunteerAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VolunteerAssignments_ReliefProjects_ReliefProjectId] FOREIGN KEY ([ReliefProjectId]) REFERENCES [ReliefProjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VolunteerAssignments_Volunteers_VolunteerId] FOREIGN KEY ([VolunteerId]) REFERENCES [Volunteers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Donations_DonorId] ON [Donations] ([DonorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Donations_ReliefProjectId] ON [Donations] ([ReliefProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Donations_TransactionReference] ON [Donations] ([TransactionReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Donations_DonorId_DonationDate] ON [Donations] ([DonorId], [DonationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProjectUpdates_PostedByUserId] ON [ProjectUpdates] ([PostedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProjectUpdates_ReliefProjectId] ON [ProjectUpdates] ([ReliefProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReliefProjects_CreatedByUserId] ON [ReliefProjects] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VolunteerAssignments_ReliefProjectId] ON [VolunteerAssignments] ([ReliefProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VolunteerAssignments_VolunteerId] ON [VolunteerAssignments] ([VolunteerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VolunteerAssignments_VolunteerId_ReliefProjectId] ON [VolunteerAssignments] ([VolunteerId], [ReliefProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Volunteers_Email] ON [Volunteers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Volunteers_UserId] ON [Volunteers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Volunteers_Status_ApplicationDate] ON [Volunteers] ([Status], [ApplicationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828181123_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828181123_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [FK_Donations_ReliefProjects_ReliefProjectId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [ProjectUpdates] DROP CONSTRAINT [FK_ProjectUpdates_ReliefProjects_ReliefProjectId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    DROP INDEX [IX_Donations_TransactionReference] ON [Donations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [ReliefProjects] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [Donations] ADD [PaymentStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserTokens]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [AspNetUserTokens] ALTER COLUMN [Name] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserTokens]') AND [c].[name] = N'LoginProvider');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [AspNetUserTokens] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserLogins]') AND [c].[name] = N'ProviderKey');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [AspNetUserLogins] ALTER COLUMN [ProviderKey] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserLogins]') AND [c].[name] = N'LoginProvider');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [AspNetUserLogins] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Donations_TransactionReference] ON [Donations] ([TransactionReference]) WHERE [TransactionReference] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_ReliefProjects_ReliefProjectId] FOREIGN KEY ([ReliefProjectId]) REFERENCES [ReliefProjects] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [ProjectUpdates] ADD CONSTRAINT [FK_ProjectUpdates_ReliefProjects_ReliefProjectId] FOREIGN KEY ([ReliefProjectId]) REFERENCES [ReliefProjects] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924181920_SyncCurrentModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924181920_SyncCurrentModel', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    ALTER TABLE [Volunteers] DROP CONSTRAINT [CK_Volunteer_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    ALTER TABLE [ReliefProjects] DROP CONSTRAINT [CK_ReliefProject_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    DROP INDEX [IX_Volunteers_Status_ApplicationDate] ON [Volunteers];
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Volunteers]') AND [c].[name] = N'Status');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Volunteers] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Volunteers] ALTER COLUMN [Status] nvarchar(450) NOT NULL;
    CREATE INDEX [IX_Volunteers_Status_ApplicationDate] ON [Volunteers] ([Status], [ApplicationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReliefProjects]') AND [c].[name] = N'Status');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ReliefProjects] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ReliefProjects] ALTER COLUMN [Status] nvarchar(max) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    ALTER TABLE [Donations] ADD [DonorEmail] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    ALTER TABLE [Donations] ADD [IdempotencyKey] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [TargetEntity] nvarchar(450) NOT NULL,
        [TargetId] int NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    CREATE TABLE [Inquiries] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [SubmittedDate] datetime2 NOT NULL,
        [Handled] bit NOT NULL,
        CONSTRAINT [PK_Inquiries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    EXEC(N'ALTER TABLE [Volunteers] ADD CONSTRAINT [CK_Volunteer_Status] CHECK ([Status] IN (''Pending'',''Approved'',''Rejected''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    EXEC(N'ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_Dates] CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    EXEC(N'ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_FundsRequired_NonNegative] CHECK ([FundsRequired] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    EXEC(N'ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_Status] CHECK ([Status] IN (''Active'',''Completed'',''Suspended''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Donations_IdempotencyKey] ON [Donations] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TargetEntity_TargetId] ON [AuditLogs] ([TargetEntity], [TargetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924191049_P1PaymentsEmailAndStatuses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924191049_P1PaymentsEmailAndStatuses', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [FK_Donations_AspNetUsers_DonorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_AspNetUsers_DonorId] FOREIGN KEY ([DonorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN

    IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NULL
    BEGIN
        ALTER TABLE Donations ADD CONSTRAINT CK_Donations_Amount_Positive CHECK (Amount > 0);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN

    IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NULL
    BEGIN
        ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative CHECK (FundsRequired >= 0);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN

    IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NULL
    BEGIN
        ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924195240_P0Phase0CriticalFixes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924195240_P0Phase0CriticalFixes', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924200101_FixPendingModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924200101_FixPendingModelChanges', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925180656_SyncDonorCascade'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [FK_Donations_AspNetUsers_DonorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925180656_SyncDonorCascade'
)
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_AspNetUsers_DonorId] FOREIGN KEY ([DonorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925180656_SyncDonorCascade'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260925180656_SyncDonorCascade', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Volunteers]') AND [c].[name] = N'PhoneNumber');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Volunteers] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Volunteers] ALTER COLUMN [PhoneNumber] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Volunteers]') AND [c].[name] = N'EmergencyContactPhone');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Volunteers] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Volunteers] ALTER COLUMN [EmergencyContactPhone] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Volunteers]') AND [c].[name] = N'EmergencyContactName');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Volunteers] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Volunteers] ALTER COLUMN [EmergencyContactName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Volunteers]') AND [c].[name] = N'DateOfBirth');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Volunteers] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Volunteers] ALTER COLUMN [DateOfBirth] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Volunteers] ADD [PrivacyConsentAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [ReliefProjects] ADD [OfflineFundsZar] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Inquiries] ADD [PrivacyConsentAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [AmountZar] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [DonorName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [ExchangeRateToZar] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [PaymentVerifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [PrivacyConsentAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [Donations] ADD [ProviderPaymentId] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'UserId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DROP INDEX [IX_AuditLogs_TargetEntity_TargetId] ON [AuditLogs];
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'TargetEntity');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [TargetEntity] nvarchar(100) NOT NULL;
    CREATE INDEX [IX_AuditLogs_TargetEntity_TargetId] ON [AuditLogs] ([TargetEntity], [TargetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'Action');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [AuditLogs] ALTER COLUMN [Action] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [Details] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    CREATE TABLE [DataProtectionKeys] (
        [Id] int NOT NULL IDENTITY,
        [FriendlyName] nvarchar(max) NULL,
        [Xml] nvarchar(max) NULL,
        CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN

    UPDATE Donations SET ExchangeRateToZar = 1, AmountZar = Amount WHERE Currency = 'ZAR';
    UPDATE Donations SET ExchangeRateToZar = 18.50, AmountZar = ROUND(Amount * 18.50, 2) WHERE Currency = 'USD';
    UPDATE Donations SET ExchangeRateToZar = 20.00, AmountZar = ROUND(Amount * 20.00, 2) WHERE Currency = 'EUR';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    UPDATE Donations SET PaymentMethod = 'Card' WHERE PaymentMethod NOT IN ('Card','EFT','Cash');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    UPDATE Donations SET PaymentVerifiedAt = DonationDate WHERE PaymentStatus = 1 AND PaymentVerifiedAt IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN

    IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NOT NULL ALTER TABLE Donations DROP CONSTRAINT CK_Donations_Amount_Positive;
    IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NOT NULL ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative;
    IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NOT NULL ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_Dates;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    EXEC(N'ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_OfflineFunds_NonNegative] CHECK ([OfflineFundsZar] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    CREATE INDEX [IX_Donations_PaymentStatus_DonationDate] ON [Donations] ([PaymentStatus], [DonationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    EXEC(N'ALTER TABLE [Donations] ADD CONSTRAINT [CK_Donation_AmountZar] CHECK ([AmountZar] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    EXEC(N'ALTER TABLE [Donations] ADD CONSTRAINT [CK_Donation_ExchangeRate] CHECK ([ExchangeRateToZar] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    EXEC(N'ALTER TABLE [Donations] ADD CONSTRAINT [CK_Donation_PaymentMethod] CHECK ([PaymentMethod] IN (''Card'',''EFT'',''Cash''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102112731_AuditRemediationNov2026'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102112731_AuditRemediationNov2026', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102132424_Part3PerformanceIndexes'
)
BEGIN
    ALTER TABLE [ReliefProjects] DROP CONSTRAINT [CK_ReliefProject_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102132424_Part3PerformanceIndexes'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReliefProjects]') AND [c].[name] = N'Status');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [ReliefProjects] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [ReliefProjects] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102132424_Part3PerformanceIndexes'
)
BEGIN
    EXEC(N'ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_Status] CHECK ([Status] IN (''Active'',''Completed'',''Suspended''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102132424_Part3PerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_ReliefProjects_Status_CreatedDate] ON [ReliefProjects] ([Status], [CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102132424_Part3PerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102132424_Part3PerformanceIndexes', N'10.0.14');
END;

COMMIT;
GO

