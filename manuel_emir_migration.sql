-- Manuel Core Emir Sistemi Migration
-- ManuelEmir tablosu + SP'ler

-- 1. Tablo olustur
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ManuelEmir')
BEGIN
    CREATE TABLE ManuelEmir (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        HisseAdi varchar(20) NOT NULL,
        Lot int NOT NULL,
        AlisFiyati float NOT NULL,           -- Limit fiyat
        Durum int NOT NULL DEFAULT 0,        -- 0=Bekliyor, 1=Gerceklesti, 2=Iptal
        OlusturmaTarihi smalldatetime NOT NULL DEFAULT GETDATE(),
        GerceklesmeTarihi smalldatetime NULL,
        GercekFiyat float NULL,              -- Gerceklesen fiyat (piyasa fiyati)
        Aciklama varchar(200) NULL
    )
END
GO

-- 2. sel_manuelEmir: Bekleyen emirleri getir
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sel_manuelEmir')
    DROP PROCEDURE sel_manuelEmir
GO

CREATE PROCEDURE sel_manuelEmir
    @HisseAdi varchar(20) = NULL
AS
BEGIN
    SELECT * FROM ManuelEmir
    WHERE Durum = 0
    AND (@HisseAdi IS NULL OR HisseAdi = @HisseAdi)
    ORDER BY OlusturmaTarihi ASC
END
GO

-- 3. upd_manuelEmir: Emir durumunu guncelle
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'upd_manuelEmir')
    DROP PROCEDURE upd_manuelEmir
GO

CREATE PROCEDURE upd_manuelEmir
    @Id bigint,
    @Durum int,
    @GercekFiyat float = NULL
AS
BEGIN
    UPDATE ManuelEmir
    SET Durum = @Durum,
        GerceklesmeTarihi = CASE WHEN @Durum IN (1,2) THEN GETDATE() ELSE NULL END,
        GercekFiyat = @GercekFiyat
    WHERE Id = @Id
END
GO
