IF OBJECT_ID('dbo.UniqueCoupons', 'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[UniqueCoupons]
	(
		[Id] [bigint] IDENTITY(1,1) NOT NULL,
		[PromotionId] [int] NOT NULL,
		[Code] [nvarchar](max) NOT NULL,
		[Valid] [datetime2](7) NOT NULL,
		[Expiration] [datetime2](7) NULL,
		[CustomerId] [uniqueidentifier] NULL,
		[Created] [datetime2](7) NULL,
		[MaxRedemptions] [int] NULL,
		[UsedRedemptions] [int] NULL,
		CONSTRAINT [PK_UniqueCoupons] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	CREATE NONCLUSTERED INDEX [IDX_UniqueCoupons_PromotionId] ON [dbo].[UniqueCoupons]
	(
		[PromotionId] ASC
	);
END
GO

IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'udttUniqueCoupons' AND is_table_type = 1)
	CREATE TYPE [dbo].[udttUniqueCoupons] AS TABLE
	(
		[Id] [bigint] NOT NULL,
		[PromotionId] [int] NOT NULL,
		[Code] [nvarchar](max) NOT NULL,
		[Valid] [datetime2](7) NOT NULL,
		[Expiration] [datetime2](7) NULL,
		[CustomerId] [uniqueidentifier] NULL,
		[Created] [datetime2](7) NOT NULL,
		[MaxRedemptions] [int] NOT NULL,
		[UsedRedemptions] [int] NOT NULL
	);
GO

IF OBJECT_ID('dbo.UniqueCoupons_DeleteById', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_DeleteById]
	@Id BIGINT
AS
BEGIN
	DELETE FROM UniqueCoupons WHERE Id = @Id
END');
GO

IF OBJECT_ID('dbo.UniqueCoupons_DeleteByPromotionId', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_DeleteByPromotionId]
	@PromotionId INT
AS
BEGIN
	DELETE FROM UniqueCoupons WHERE PromotionId = @PromotionId
END');
GO

IF OBJECT_ID('dbo.UniqueCoupons_GetById', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_GetById]
	@Id BIGINT
AS
BEGIN
	SELECT * FROM UniqueCoupons WHERE Id = @Id
END');
GO

IF OBJECT_ID('dbo.UniqueCoupons_GetByPromotionId', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_GetByPromotionId]
	@PromotionId INT
AS
BEGIN
	SELECT * FROM UniqueCoupons WHERE PromotionId = @PromotionId
END');
GO

IF OBJECT_ID('dbo.UniqueCoupons_Save', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_Save]
	@Data dbo.[udttUniqueCoupons] readonly
AS
BEGIN
	MERGE dbo.UniqueCoupons AS TARGET
	USING @Data AS SOURCE
	On (TARGET.Id = SOURCE.Id)
	WHEN MATCHED THEN
		UPDATE SET PromotionId = SOURCE.PromotionId,
				   Code = SOURCE.Code,
				   Valid = SOURCE.Valid,
				   Expiration = SOURCE.Expiration,
				   CustomerId = SOURCE.CustomerId,
				   Created = SOURCE.Created,
				   MaxRedemptions = SOURCE.MaxRedemptions,
				   UsedRedemptions = SOURCE.UsedRedemptions
	WHEN NOT MATCHED THEN
		INSERT (PromotionId, Code, Valid, Expiration, CustomerId, Created, MaxRedemptions, UsedRedemptions)
		VALUES (SOURCE.PromotionId, SOURCE.Code, SOURCE.Valid, SOURCE.Expiration, SOURCE.CustomerId, SOURCE.Created, SOURCE.MaxRedemptions, SOURCE.UsedRedemptions);
END');
GO

IF OBJECT_ID('dbo.UniqueCoupons_DeleteExpiredCoupons', 'P') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UniqueCoupons_DeleteExpiredCoupons]
AS
BEGIN
	DELETE FROM UniqueCoupons WHERE Expiration < GETDATE()
END');
GO
