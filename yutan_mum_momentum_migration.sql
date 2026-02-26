-- =============================================
-- Yutan Mum Momentum Overnight Stratejisi
-- Migration Script
-- 2026-02-25
-- =============================================

-- 1. YutanMumConfig yeni kolonlar
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumConfig') AND name = 'SinyalTipi')
BEGIN
    ALTER TABLE YutanMumConfig ADD SinyalTipi int NOT NULL DEFAULT 1  -- 0=Engulfing, 1=Momentum
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumConfig') AND name = 'CloseThreshold')
BEGIN
    ALTER TABLE YutanMumConfig ADD CloseThreshold float NOT NULL DEFAULT 0.80
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumConfig') AND name = 'MinMomentum')
BEGIN
    ALTER TABLE YutanMumConfig ADD MinMomentum float NOT NULL DEFAULT 1.0
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumConfig') AND name = 'OvernightMod')
BEGIN
    ALTER TABLE YutanMumConfig ADD OvernightMod bit NOT NULL DEFAULT 1  -- 1=ertesi acilista sat
END
GO

-- 2. YutanMumHareket yeni kolonlar
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumHareket') AND name = 'BugunYuksek')
BEGIN
    ALTER TABLE YutanMumHareket ADD BugunYuksek float NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumHareket') AND name = 'BugunDusuk')
BEGIN
    ALTER TABLE YutanMumHareket ADD BugunDusuk float NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('YutanMumHareket') AND name = 'MomentumYuzde')
BEGIN
    ALTER TABLE YutanMumHareket ADD MomentumYuzde float NULL
END
GO

-- 3. sel_yutanMumConfig SP guncelle (yeni kolonlari da dondurmesi icin)
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_yutanMumConfig')
    DROP PROCEDURE sel_yutanMumConfig
GO

CREATE PROCEDURE sel_yutanMumConfig
AS
BEGIN
    SET NOCOUNT ON
    SELECT TOP 1 * FROM YutanMumConfig
END
GO

-- 4. ins_yutanMumHareket SP guncelle (+BugunYuksek, BugunDusuk, MomentumYuzde)
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'ins_yutanMumHareket')
    DROP PROCEDURE ins_yutanMumHareket
GO

CREATE PROCEDURE ins_yutanMumHareket
    @BatchId bigint,
    @HisseAdi varchar(50),
    @Lot int,
    @AlisFiyati decimal(18,2),
    @DunkuAcilis decimal(18,2) = NULL,
    @DunkuKapanis decimal(18,2) = NULL,
    @BugunAcilis decimal(18,2) = NULL,
    @BugunKapanis decimal(18,2) = NULL,
    @DunkuHacim bigint = NULL,
    @BugunHacim bigint = NULL,
    @BugunYuksek float = NULL,
    @BugunDusuk float = NULL,
    @MomentumYuzde float = NULL
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO YutanMumHareket (BatchId, HisseAdi, Lot, AlisFiyati, DunkuAcilis, DunkuKapanis, BugunAcilis, BugunKapanis, DunkuHacim, BugunHacim, BugunYuksek, BugunDusuk, MomentumYuzde)
    VALUES (@BatchId, @HisseAdi, @Lot, @AlisFiyati, @DunkuAcilis, @DunkuKapanis, @BugunAcilis, @BugunKapanis, @DunkuHacim, @BugunHacim, @BugunYuksek, @BugunDusuk, @MomentumYuzde)
END
GO

-- 5. Config guncelleme: Momentum + Overnight mod default yap
UPDATE YutanMumConfig SET SinyalTipi = 1, IslemSaati = '17:55', OvernightMod = 1
GO
