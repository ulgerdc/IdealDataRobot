-- ============================================================
-- Gelismis Arbitraj - Pozisyon Yonetimi Migration
-- Tarih: 2026-02-25
-- Aciklama: ins_arbitrajGelismisHareket SP (insert/update pattern)
-- ============================================================

-- ============================================================
-- SP: ins_arbitrajGelismisHareket
-- @Id = 0 → INSERT yeni pozisyon (AktifMi=1)
-- @Id > 0 → UPDATE kapama (Kar hesapla, AktifMi=0)
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'ins_arbitrajGelismisHareket')
    DROP PROCEDURE ins_arbitrajGelismisHareket
GO

CREATE PROCEDURE [dbo].[ins_arbitrajGelismisHareket]
    @Id bigint,
    @ArbitrajGelismisId bigint,
    @RobotAdi varchar(50),
    @HisseAdi varchar(50),
    @ArbitrajTipi int,
    @Bacak1Sembol varchar(30),
    @Bacak1Yon varchar(5),
    @Bacak1GirisFiyat decimal(18,2),
    @Bacak1CikisFiyat decimal(18,2) = NULL,
    @Bacak1Lot int,
    @Bacak2Sembol varchar(30),
    @Bacak2Yon varchar(5),
    @Bacak2GirisFiyat decimal(18,2),
    @Bacak2CikisFiyat decimal(18,2) = NULL,
    @Bacak2Lot int,
    @GirisSpreadYuzde decimal(18,4),
    @CikisSpreadYuzde decimal(18,4) = NULL
AS
BEGIN
    SET NOCOUNT ON

    IF @Id = 0
    BEGIN
        INSERT INTO [dbo].[ArbitrajGelismisHareket]
            (ArbitrajGelismisId, RobotAdi, HisseAdi, ArbitrajTipi,
             Bacak1Sembol, Bacak1Yon, Bacak1GirisFiyat, Bacak1Lot,
             Bacak2Sembol, Bacak2Yon, Bacak2GirisFiyat, Bacak2Lot,
             GirisSpreadYuzde, AktifMi, PozisyonTarihi)
        VALUES
            (@ArbitrajGelismisId, @RobotAdi, @HisseAdi, @ArbitrajTipi,
             @Bacak1Sembol, @Bacak1Yon, @Bacak1GirisFiyat, @Bacak1Lot,
             @Bacak2Sembol, @Bacak2Yon, @Bacak2GirisFiyat, @Bacak2Lot,
             @GirisSpreadYuzde, 1, GETDATE())
    END
    ELSE
    BEGIN
        -- Kar hesapla: ALIS bacagi = (Cikis - Giris) * Lot, SATIS bacagi = (Giris - Cikis) * Lot
        DECLARE @kar1 decimal(18,2), @kar2 decimal(18,2), @toplamKar decimal(18,2)
        DECLARE @mevcutBacak1Yon varchar(5), @mevcutBacak1GirisFiyat decimal(18,2), @mevcutBacak1Lot int
        DECLARE @mevcutBacak2Yon varchar(5), @mevcutBacak2GirisFiyat decimal(18,2), @mevcutBacak2Lot int

        SELECT @mevcutBacak1Yon = Bacak1Yon, @mevcutBacak1GirisFiyat = Bacak1GirisFiyat, @mevcutBacak1Lot = Bacak1Lot,
               @mevcutBacak2Yon = Bacak2Yon, @mevcutBacak2GirisFiyat = Bacak2GirisFiyat, @mevcutBacak2Lot = Bacak2Lot
        FROM [dbo].[ArbitrajGelismisHareket]
        WHERE Id = @Id

        -- Bacak 1 kar
        IF @mevcutBacak1Yon = 'ALIS'
            SET @kar1 = (@Bacak1CikisFiyat - @mevcutBacak1GirisFiyat) * @mevcutBacak1Lot
        ELSE
            SET @kar1 = (@mevcutBacak1GirisFiyat - @Bacak1CikisFiyat) * @mevcutBacak1Lot

        -- Bacak 2 kar
        IF @mevcutBacak2Yon = 'ALIS'
            SET @kar2 = (@Bacak2CikisFiyat - @mevcutBacak2GirisFiyat) * @mevcutBacak2Lot
        ELSE
            SET @kar2 = (@mevcutBacak2GirisFiyat - @Bacak2CikisFiyat) * @mevcutBacak2Lot

        SET @toplamKar = @kar1 + @kar2

        UPDATE [dbo].[ArbitrajGelismisHareket]
        SET Bacak1CikisFiyat = @Bacak1CikisFiyat,
            Bacak2CikisFiyat = @Bacak2CikisFiyat,
            CikisSpreadYuzde = @CikisSpreadYuzde,
            Kar = @toplamKar,
            AktifMi = 0,
            KapanisTarihi = GETDATE()
        WHERE Id = @Id
    END
END
GO
PRINT 'ins_arbitrajGelismisHareket SP olusturuldu.'
GO

PRINT '=== Pozisyon yonetimi migration tamamlandi ==='
GO
