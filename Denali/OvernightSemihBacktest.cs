public class OvernightSemihBacktest
{
    // === OVERNIGHT STRATEJI — HASSASIYET ANALIZI ===
    // 17:55'te alim yaparken gun henuz kapanmamis. Backtest kapanmis Close kullanir.
    // Bu test: esikleri sikilarstirarak 17:55 belirsizliginin etkisini olcer.
    // NOT: IdealPro GrafikFiyatOku("G","Hacim") tum sifir donuyor — hacim filtresi DEVRE DISI

    static double alisKayma = 0.15;
    static double satisKayma = 0.15;
    static bool endeksFiltre = true;
    static int maxGunlukPozisyon = 5;
    static double hisseBasiTutar = 10000;
    static int minBarEsik = 200;

    // Hassasiyet senaryolari
    // Her senaryo: {gkMinYuk, gkMinCR, t2MinYuk, t2MaxYuk, t2MinCR, alisKaymaEk}
    // alisKaymaEk: 17:55'te alisin ek belirsizligi (fiyat kapanisa kadar degisebilir)
    static string[] senaryoAdlari = new string[] {
        "Baz(3.0/90)",       // Mevcut parametreler
        "Siki(3.5/92)",      // +0.5% yuk, +2 CR
        "CokSiki(4.0/93)",   // +1.0% yuk, +3 CR
        "Genis(2.5/88)",     // -0.5% yuk, -2 CR (17:55'te sinyal var ama kapanista kaybetmis)
        "BazKayma05",        // Baz + ekstra %0.5 kayma (17:55 vs 18:00 fiyat farki)
        "BazKayma10"         // Baz + ekstra %1.0 kayma
    };
    static double[][] senaryoParams = new double[][] {
        new double[] { 3.0, 90.0, 7.0, 10.0, 80.0, 0.0 },   // Baz
        new double[] { 3.5, 92.0, 7.5, 10.0, 82.0, 0.0 },   // Siki
        new double[] { 4.0, 93.0, 8.0, 10.0, 83.0, 0.0 },   // CokSiki
        new double[] { 2.5, 88.0, 6.5, 10.0, 78.0, 0.0 },   // Genis
        new double[] { 3.0, 90.0, 7.0, 10.0, 80.0, 0.5 },   // BazKayma05
        new double[] { 3.0, 90.0, 7.0, 10.0, 80.0, 1.0 }    // BazKayma10
    };

    // Karaliste
    static System.Collections.Generic.HashSet<string> karaliste = KaralisteOlustur();

    private static System.Collections.Generic.HashSet<string> KaralisteOlustur()
    {
        var set = new System.Collections.Generic.HashSet<string>();
        set.Add("OBAMS"); set.Add("KGYO"); set.Add("LIDER"); set.Add("GESAN");
        set.Add("ESEN"); set.Add("PASEU"); set.Add("BLUME"); set.Add("ESCAR");
        set.Add("TRCAS"); set.Add("DERIM"); set.Add("YEOTK"); set.Add("YGGYO");
        set.Add("ASGYO"); set.Add("PENTA"); set.Add("ANHYT"); set.Add("GOLTS");
        set.Add("AKSA"); set.Add("FMIZP"); set.Add("DURKN"); set.Add("GSRAY");
        set.Add("CEOEM"); set.Add("AGHOL"); set.Add("EGGUB"); set.Add("AKBNK");
        set.Add("KRVGD"); set.Add("SKTAS"); set.Add("RYGYO"); set.Add("AKSGY");
        return set;
    }

    class HisseVeri
    {
        public string HisseAdi;
        public System.Collections.Generic.List<double> Close = new System.Collections.Generic.List<double>();
        public System.Collections.Generic.List<double> Open = new System.Collections.Generic.List<double>();
        public System.Collections.Generic.List<double> High = new System.Collections.Generic.List<double>();
        public System.Collections.Generic.List<double> Low = new System.Collections.Generic.List<double>();
        public int BarCount { get { return Close.Count; } }
    }

    class GunlukSinyal
    {
        public string HisseAdi;
        public string SinyalTipi;
        public double Yukselis;
        public double CloseRange;
        public double AlisFiyat;
        public double SatisFiyat;
        public double PnlYuzde;
        public double KarTL;
        public int Lot;
    }

    class SenaryoSonuc
    {
        public string Ad;
        public int Islem;
        public int Kazanan;
        public double KarTL;
        public double BrutKar;
        public double BrutZarar;
        public double WR { get { return Islem > 0 ? Kazanan * 100.0 / Islem : 0; } }
        public double PF { get { return BrutZarar > 0 ? BrutKar / BrutZarar : 0; } }
    }

    public static void Baslat(dynamic Sistem)
    {
        if (Sistem == null) return;
        try { Sistem.Mesaj("=== OVERNIGHT STRATEJI — HASSASIYET ANALIZI ==="); } catch { }
        try { Sistem.Mesaj("17:55 vs 18:00 kapanisi: Esik ve kayma hassasiyeti testi"); } catch { }

        // 1. Hisse listesi
        var hisseler = HisseListesiGetir(Sistem);
        if (hisseler == null || hisseler.Count == 0) { try { Sistem.Mesaj("HATA: Hisse listesi alinamadi"); } catch { } return; }

        // 2. Veri cek
        var veriler = new System.Collections.Generic.List<HisseVeri>();
        int atlanan = 0;
        int sayac = 0;
        foreach (var hisse in hisseler)
        {
            if (karaliste.Contains(hisse)) { atlanan++; continue; }
            sayac++;
            if (sayac % 50 == 0)
                try { Sistem.Mesaj(string.Format("Veri: {0}/{1}...", sayac, hisseler.Count - atlanan)); } catch { }

            var veri = VeriCekGunluk(Sistem, hisse, false);
            if (veri != null && veri.BarCount >= minBarEsik)
                veriler.Add(veri);
        }

        if (veriler.Count == 0) { try { Sistem.Mesaj("HATA: Veri alinamadi"); } catch { } return; }

        // 3. XU100
        HisseVeri endeksVeri = null;
        if (endeksFiltre)
        {
            endeksVeri = VeriCekGunluk(Sistem, "XU100", true);
            if (endeksVeri == null || endeksVeri.BarCount < 2)
                endeksFiltre = false;
        }

        // 4. Ortak bar
        int minBar = veriler[0].BarCount;
        for (int i = 1; i < veriler.Count; i++)
            if (veriler[i].BarCount < minBar) minBar = veriler[i].BarCount;
        if (endeksVeri != null && endeksVeri.BarCount < minBar)
            minBar = endeksVeri.BarCount;

        int baslangic = 5;

        try { Sistem.Mesaj(string.Format("Hisse: {0}, Bar: {1}, Test: bar {2}~{3} ({4} gun)",
            veriler.Count, minBar, baslangic, minBar - 2, minBar - 2 - baslangic)); } catch { }

        // 5. Sonuc yapilari
        int senaryoSayisi = senaryoAdlari.Length;
        var sonuclar = new SenaryoSonuc[senaryoSayisi];
        for (int si = 0; si < senaryoSayisi; si++)
        {
            sonuclar[si] = new SenaryoSonuc();
            sonuclar[si].Ad = senaryoAdlari[si];
        }

        // 6. Sinir sinyal istatigi (baz senaryoda sinyal ama siki senaryoda degil)
        int sinirSinyalSayisi = 0; // baz'da var, siki'da yok
        int toplamBazSinyal = 0;

        // 7. Gunluk detay (baz senaryo icin)
        var gunlukDetay = new System.Collections.Generic.List<string>();
        var sinyalDetay = new System.Collections.Generic.List<string>();
        int endeksNegatifGun = 0;

        for (int d = baslangic; d < minBar - 1; d++)
        {
            double xu100Pct = 0;
            bool endeksOK = true;
            if (endeksFiltre && endeksVeri != null)
            {
                int ei = endeksVeri.BarCount - minBar + d;
                if (ei >= 1 && ei < endeksVeri.BarCount)
                {
                    xu100Pct = (endeksVeri.Close[ei] - endeksVeri.Close[ei - 1]) / endeksVeri.Close[ei - 1] * 100;
                    endeksOK = xu100Pct > 0;
                }
            }

            if (!endeksOK)
            {
                endeksNegatifGun++;
                gunlukDetay.Add(string.Format("{0},{1:F2},0,0,0,0.00,ENDEKS_NEGATIF", d, xu100Pct));
                continue;
            }

            // Her hisse icin ham sinyal bilgisi topla (once tum sinyal verilerini hesapla)
            var hamSinyaller = new System.Collections.Generic.List<double[]>(); // yukselis, cr, alisFiyat, satisFiyat, pnl, lot, karTL
            var hamHisseAdi = new System.Collections.Generic.List<string>();

            for (int s = 0; s < veriler.Count; s++)
            {
                var v = veriler[s];
                int vi = v.BarCount - minBar + d;
                if (vi < 2 || vi >= v.BarCount) continue;

                double c = v.Close[vi];
                double o = v.Open[vi];
                double h = v.High[vi];
                double l = v.Low[vi];
                double pc = v.Close[vi - 1];
                if (c <= 0 || pc <= 0 || o <= 0) continue;

                double yukselis = (c - pc) / pc * 100;
                if (yukselis < 2.0) continue; // en genis senaryodan bile dusuk, atla
                if (c <= o) continue; // yesil mum (tum senaryolarda ortak)

                double rng = h - l;
                double cr = rng > 0 ? (c - l) / rng * 100 : 50.0;

                int viNext = vi + 1;
                if (viNext >= v.BarCount || v.Open[viNext] <= 0) continue;

                // Alim fiyati: baz kayma ile hesapla (ek kayma senaryoya ozel)
                double bazAlis = c * (1 + alisKayma / 100);
                double satisFiyat = v.Open[viNext] * (1 - satisKayma / 100);
                double bazPnl = (satisFiyat - bazAlis) / bazAlis * 100;
                if (bazPnl > 15 || bazPnl < -15) continue;
                int lot = (int)(hisseBasiTutar / bazAlis);
                if (lot <= 0) lot = 1;

                hamSinyaller.Add(new double[] { yukselis, cr, bazAlis, satisFiyat, bazPnl, lot, c });
                hamHisseAdi.Add(v.HisseAdi);
            }

            // Her senaryo icin sinyal filtrele ve sonuclari hesapla
            for (int si = 0; si < senaryoSayisi; si++)
            {
                double gkMinYuk = senaryoParams[si][0];
                double gkMinCR = senaryoParams[si][1];
                double t2MinYuk = senaryoParams[si][2];
                double t2MaxYuk = senaryoParams[si][3];
                double t2MinCR = senaryoParams[si][4];
                double ekKayma = senaryoParams[si][5];

                var t2 = new System.Collections.Generic.List<int>();
                var t2Yuk = new System.Collections.Generic.List<double>();
                var gk = new System.Collections.Generic.List<int>();
                var gkYuk = new System.Collections.Generic.List<double>();

                for (int i = 0; i < hamSinyaller.Count; i++)
                {
                    double yuk = hamSinyaller[i][0];
                    double cr = hamSinyaller[i][1];

                    // T2
                    if (yuk >= t2MinYuk && yuk <= t2MaxYuk && cr >= t2MinCR)
                    {
                        t2.Add(i); t2Yuk.Add(yuk);
                        continue;
                    }
                    // GK
                    if (yuk >= gkMinYuk && cr >= gkMinCR)
                    {
                        gk.Add(i); gkYuk.Add(yuk);
                    }
                }

                // T2 sirala (yukselis desc)
                for (int i = 0; i < t2.Count - 1; i++)
                    for (int j = i + 1; j < t2.Count; j++)
                        if (t2Yuk[j] > t2Yuk[i])
                        {
                            double ty = t2Yuk[i]; t2Yuk[i] = t2Yuk[j]; t2Yuk[j] = ty;
                            int ti = t2[i]; t2[i] = t2[j]; t2[j] = ti;
                        }
                // GK sirala
                for (int i = 0; i < gk.Count - 1; i++)
                    for (int j = i + 1; j < gk.Count; j++)
                        if (gkYuk[j] > gkYuk[i])
                        {
                            double gy = gkYuk[i]; gkYuk[i] = gkYuk[j]; gkYuk[j] = gy;
                            int gi = gk[i]; gk[i] = gk[j]; gk[j] = gi;
                        }

                // Birlestir: T2 oncelikli
                var secim = new System.Collections.Generic.List<int>();
                foreach (int idx in t2) secim.Add(idx);
                foreach (int idx in gk) secim.Add(idx);

                // Sinir sinyal takibi (baz vs siki)
                if (si == 0) toplamBazSinyal += secim.Count;
                if (si == 0)
                {
                    // Baz senaryodaki sinyalleri kontrol et — siki senaryoda var mi?
                    double sikiGkYuk = senaryoParams[1][0];
                    double sikiGkCR = senaryoParams[1][1];
                    double sikiT2Yuk = senaryoParams[1][2];
                    double sikiT2Max = senaryoParams[1][3];
                    double sikiT2CR = senaryoParams[1][4];

                    for (int i = 0; i < secim.Count && i < maxGunlukPozisyon; i++)
                    {
                        int idx = secim[i];
                        double yuk = hamSinyaller[idx][0];
                        double cr = hamSinyaller[idx][1];
                        bool sikiOK = false;
                        if (yuk >= sikiT2Yuk && yuk <= sikiT2Max && cr >= sikiT2CR) sikiOK = true;
                        if (yuk >= sikiGkYuk && cr >= sikiGkCR) sikiOK = true;
                        if (!sikiOK) sinirSinyalSayisi++;
                    }
                }

                int limit = secim.Count;
                if (limit > maxGunlukPozisyon) limit = maxGunlukPozisyon;

                double gunKarTL = 0;
                int gunKazanan = 0;

                for (int i = 0; i < limit; i++)
                {
                    int idx = secim[i];
                    double bazAlis = hamSinyaller[idx][2];
                    double satisFiyat = hamSinyaller[idx][3];
                    double closePrice = hamSinyaller[idx][6];

                    // Ek kayma uygula (17:55 vs 18:00 fiyat farki simulasyonu)
                    double efektifAlis = bazAlis;
                    double efektifPnl = hamSinyaller[idx][4];
                    if (ekKayma > 0)
                    {
                        efektifAlis = closePrice * (1 + (alisKayma + ekKayma) / 100);
                        efektifPnl = (satisFiyat - efektifAlis) / efektifAlis * 100;
                    }

                    int lot = (int)(hisseBasiTutar / efektifAlis);
                    if (lot <= 0) lot = 1;
                    double karTL = lot * (satisFiyat - efektifAlis);

                    sonuclar[si].Islem++;
                    sonuclar[si].KarTL += karTL;
                    if (efektifPnl > 0)
                    {
                        sonuclar[si].Kazanan++;
                        sonuclar[si].BrutKar += karTL;
                    }
                    else
                    {
                        sonuclar[si].BrutZarar += System.Math.Abs(karTL);
                    }

                    // Baz senaryo detay
                    if (si == 0)
                    {
                        double yuk = hamSinyaller[idx][0];
                        double cr = hamSinyaller[idx][1];
                        string tip = (yuk >= 7.0 && yuk <= 10.0 && cr >= 80.0) ? "T2" : "GK";
                        gunKarTL += karTL;
                        if (efektifPnl > 0) gunKazanan++;

                        sinyalDetay.Add(string.Format("{0},{1},{2},{3:F2},{4:F1},{5:F2},{6:F2},{7:F4},{8:F2},{9},SECILDI",
                            d, hamHisseAdi[idx], tip, yuk, cr, bazAlis, satisFiyat, efektifPnl, karTL, lot));
                    }
                }

                // Baz senaryo secilemeyenler
                if (si == 0)
                {
                    for (int i = limit; i < secim.Count; i++)
                    {
                        int idx = secim[i];
                        double yuk = hamSinyaller[idx][0];
                        double cr = hamSinyaller[idx][1];
                        string tip = (yuk >= 7.0 && yuk <= 10.0 && cr >= 80.0) ? "T2" : "GK";
                        sinyalDetay.Add(string.Format("{0},{1},{2},{3:F2},{4:F1},{5:F2},{6:F2},{7:F4},{8:F2},{9},",
                            d, hamHisseAdi[idx], tip, yuk, cr,
                            hamSinyaller[idx][2], hamSinyaller[idx][3], hamSinyaller[idx][4],
                            (int)hamSinyaller[idx][5] * (hamSinyaller[idx][3] - hamSinyaller[idx][2]),
                            (int)hamSinyaller[idx][5]));
                    }

                    gunlukDetay.Add(string.Format("{0},{1:F2},{2},{3},{4},{5:F2}",
                        d, xu100Pct, secim.Count, limit, gunKazanan, gunKarTL));
                }
            }

            if (d % 50 == 0)
                try { Sistem.Mesaj(string.Format("Bar {0}/{1}...", d, minBar - 2)); } catch { }
        }

        // Sonuclari goster
        try
        {
            Sistem.Mesaj(string.Format("\n=== HASSASIYET ANALIZI ({0} gun, {1} hisse, endeksNegatif: {2}) ===",
                minBar - 2 - baslangic, veriler.Count, endeksNegatifGun));
            Sistem.Mesaj(string.Format("MaxPoz={0} | HisseBasiTutar={1:N0} | Slippage={2}+{3}",
                maxGunlukPozisyon, hisseBasiTutar, alisKayma, satisKayma));
            Sistem.Mesaj("");
            Sistem.Mesaj("Senaryo          | Islem | Kazanan | WR%   | PF   | KarTL      | Fark%");
            Sistem.Mesaj("-----------------|-------|---------|-------|------|------------|------");

            double bazKar = sonuclar[0].KarTL;
            for (int si = 0; si < senaryoSayisi; si++)
            {
                var sn = sonuclar[si];
                double farkYuzde = bazKar > 0 ? (sn.KarTL - bazKar) / bazKar * 100 : 0;
                Sistem.Mesaj(string.Format("{0,-16} | {1,5} | {2,7} | {3,5:F1} | {4,4:F2} | {5,10:N0} | {6:F1}%",
                    sn.Ad, sn.Islem, sn.Kazanan, sn.WR, sn.PF, sn.KarTL, farkYuzde));
            }

            Sistem.Mesaj("");
            Sistem.Mesaj(string.Format("Sinir sinyal: Baz'da secilen ama Siki'da sinyal olmayan = {0}/{1} ({2:F1}%)",
                sinirSinyalSayisi, sonuclar[0].Islem,
                sonuclar[0].Islem > 0 ? sinirSinyalSayisi * 100.0 / sonuclar[0].Islem : 0));
            Sistem.Mesaj("(Bu sinyaller 17:55 belirsizligiyle kaybolabilir)");
        }
        catch { }

        // CSV
        string csvDir = @"C:\iko\robot\backtest_sonuc";
        try
        {
            if (!System.IO.Directory.Exists(csvDir)) System.IO.Directory.CreateDirectory(csvDir);
            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Ozet CSV
            string ozetDosya = System.IO.Path.Combine(csvDir, "overnight_hassasiyet_ozet_" + ts + ".csv");
            var ozetSat = new System.Collections.Generic.List<string>();
            ozetSat.Add("# Overnight Strateji Hassasiyet Analizi");
            ozetSat.Add(string.Format("# Hisse={0} Gun={1} EndeksNegatif={2}", veriler.Count, minBar - 2 - baslangic, endeksNegatifGun));
            ozetSat.Add(string.Format("# MaxPoz={0} HisseBasiTutar={1} Slippage={2}+{3}", maxGunlukPozisyon, hisseBasiTutar, alisKayma, satisKayma));
            ozetSat.Add(string.Format("# SinirSinyal={0}/{1} ({2:F1}%)", sinirSinyalSayisi, sonuclar[0].Islem,
                sonuclar[0].Islem > 0 ? sinirSinyalSayisi * 100.0 / sonuclar[0].Islem : 0));
            ozetSat.Add("Senaryo,GK_MinYuk,GK_MinCR,T2_MinYuk,T2_MaxYuk,T2_MinCR,EkKayma,Islem,Kazanan,WR,PF,KarTL,BrutKar,BrutZarar");
            for (int si = 0; si < senaryoSayisi; si++)
            {
                var sn = sonuclar[si];
                ozetSat.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:F2},{10:F2},{11:F2},{12:F2},{13:F2}",
                    sn.Ad, senaryoParams[si][0], senaryoParams[si][1], senaryoParams[si][2],
                    senaryoParams[si][3], senaryoParams[si][4], senaryoParams[si][5],
                    sn.Islem, sn.Kazanan, sn.WR, sn.PF, sn.KarTL, sn.BrutKar, sn.BrutZarar));
            }
            System.IO.File.WriteAllLines(ozetDosya, ozetSat);

            // Gunluk CSV (baz senaryo)
            string gunDosya = System.IO.Path.Combine(csvDir, "overnight_hassasiyet_gunluk_" + ts + ".csv");
            var gunSat = new System.Collections.Generic.List<string>();
            gunSat.Add("BarNo,XU100,ToplamSinyal,Secilen,Kazanan,KarTL");
            for (int i = 0; i < gunlukDetay.Count; i++) gunSat.Add(gunlukDetay[i]);
            System.IO.File.WriteAllLines(gunDosya, gunSat);

            // Detay CSV
            string detDosya = System.IO.Path.Combine(csvDir, "overnight_hassasiyet_detay_" + ts + ".csv");
            var detSat = new System.Collections.Generic.List<string>();
            detSat.Add("BarNo,Hisse,Tip,Yukselis,CR,AlisFiyat,SatisFiyat,PnlYuzde,KarTL,Lot,Secildi");
            for (int i = 0; i < sinyalDetay.Count; i++) detSat.Add(sinyalDetay[i]);
            System.IO.File.WriteAllLines(detDosya, detSat);

            try { Sistem.Mesaj(string.Format("\nCSV: {0}\nCSV: {1}\nCSV: {2}", ozetDosya, gunDosya, detDosya)); } catch { }
        }
        catch (System.Exception ex) { try { Sistem.Mesaj("CSV HATA: " + ex.Message); } catch { } }
    }

    static HisseVeri VeriCekGunluk(dynamic Sistem, string hisse, bool endeks)
    {
        string sembol = endeks ? "IMKBX'" + hisse : "IMKBH'" + hisse;
        try
        {
            var kapanislar = Sistem.GrafikFiyatOku(sembol, "G", "Kapanis");
            if (kapanislar == null || kapanislar.Count < 2) return null;
            var acilislar = Sistem.GrafikFiyatOku(sembol, "G", "Acilis");
            var yuksekler = Sistem.GrafikFiyatOku(sembol, "G", "Yuksek");
            var dusukler = Sistem.GrafikFiyatOku(sembol, "G", "Dusuk");
            if (acilislar == null || yuksekler == null || dusukler == null) return null;
            int count = kapanislar.Count;
            if (acilislar.Count != count || yuksekler.Count != count || dusukler.Count != count) return null;
            var veri = new HisseVeri();
            veri.HisseAdi = hisse;
            for (int i = 0; i < count; i++)
            {
                veri.Close.Add((double)kapanislar[i]);
                veri.Open.Add((double)acilislar[i]);
                veri.High.Add((double)yuksekler[i]);
                veri.Low.Add((double)dusukler[i]);
            }
            return veri;
        }
        catch { return null; }
    }

    static System.Collections.Generic.List<string> HisseListesiGetir(dynamic Sistem)
    {
        var hisseler = new System.Collections.Generic.List<string>();
        try
        {
            var liste = Sistem.YuzeyselListeGetir("IMKBH'");
            if (liste != null && liste.Count > 0)
            {
                for (int i = 0; i < liste.Count; i++)
                {
                    string grup = "";
                    try { grup = liste[i].Grup.ToString().Trim(); } catch { }
                    if (grup != "Y" && grup != "A") continue;
                    string sym = liste[i].Symbol.ToString();
                    if (sym.Contains("'")) sym = sym.Substring(sym.IndexOf("'") + 1);
                    sym = sym.Trim();
                    if (sym.Length > 0 && sym.Length <= 10) hisseler.Add(sym);
                }
            }
            try { Sistem.Mesaj("Liste: " + hisseler.Count + " hisse (Y+A)"); } catch { }
        }
        catch { }
        if (hisseler.Count == 0)
        {
            try { hisseler = DatabaseManager.Bist100HisselerGetir(); } catch { }
        }
        return hisseler;
    }
}
