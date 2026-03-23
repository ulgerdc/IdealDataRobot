
public class OvernightAnaliz
{
    public static void Baslat(dynamic Sistem)
    {
        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false) return;

        // Hedef hisseler: 12/03 kapanisinda alinmis, 13/03 acilisinda satilmis
        string[] hedefler = new string[] {
            "KONKA", "ARDYZ", "ODAS", "PETKM", "ASTOR",
            "CATES", "NETCD", "EMPAE", "KONTR", "HDFGS",
            "AKSEN", "BTCIM", "EGGUB", "KATMR", "TATEN"
        };

        // 14/03 Cumartesi — piyasa kapali, son trading bar = 13/03 Cuma
        // son   = 13/03 (Cuma - satis gunu)
        // son-1 = 12/03 (Persembe - alim gunu)
        // son-2 = 11/03 (Carsamba - onceki gun)
        // son-3 = 10/03 (Sali)

        string log = "=== OVERNIGHT ANALIZ (12/03 alim) ===\n";
        log += "Hisse|Yuk%|GunIci%|CR%|HacOr|HacTL|DunY%|AcFark%\n";

        for (int i = 0; i < hedefler.Length; i++)
        {
            try
            {
                string sembol = "IMKBH'" + hedefler[i];

                var kapanislar = Sistem.GrafikFiyatOku(sembol, "G", "Kapanis");
                var acilislar = Sistem.GrafikFiyatOku(sembol, "G", "Acilis");
                var yuksekler = Sistem.GrafikFiyatOku(sembol, "G", "Yuksek");
                var dusukler = Sistem.GrafikFiyatOku(sembol, "G", "Dusuk");
                var hacimler = Sistem.GrafikFiyatOku(sembol, "G", "Hacim");

                if (kapanislar == null || kapanislar.Count < 4) continue;

                int son = kapanislar.Count - 1;
                // Alim gunu = son-1 (12/03 Persembe)
                int alimBar = son - 1;
                int oncekiBar = son - 2; // 11/03

                double alimKapanis = kapanislar[alimBar];
                double oncekiKapanis = kapanislar[oncekiBar];
                double alimAcilis = acilislar[alimBar];
                double alimYuksek = yuksekler[alimBar];
                double alimDusuk = dusukler[alimBar];

                // 1. Yukselis: (C - C[1]) / C[1] * 100 (dune gore)
                double yukselis = (alimKapanis - oncekiKapanis) / oncekiKapanis * 100;

                // 2. Gun ici momentum: (C - O) / O * 100
                double gunIci = alimAcilis > 0 ? (alimKapanis - alimAcilis) / alimAcilis * 100 : 0;

                // 3. Close Range: (C - L) / (H - L) * 100
                double cr = 0;
                double range = alimYuksek - alimDusuk;
                if (range > 0) cr = (alimKapanis - alimDusuk) / range * 100;

                // 4. Hacim / onceki gun hacim
                double hacimOran = 0;
                double alimHacimTL = 0;
                if (hacimler != null && hacimler.Count > alimBar)
                {
                    double alimHacim = (double)hacimler[alimBar];
                    alimHacimTL = alimHacim * alimKapanis;
                    if (hacimler.Count > oncekiBar)
                    {
                        double oncekiHacim = (double)hacimler[oncekiBar];
                        if (oncekiHacim > 0) hacimOran = alimHacim / oncekiHacim;
                    }
                }
                // Debug: ilk hisse icin farkli hacim alanlari dene
                if (i == 0)
                {
                    string hDbg = "HACIM_DBG " + hedefler[i] + ": ";
                    string[] alanlar = new string[] { "Hacim", "Miktar", "Volume", "IslemHacmi", "Islem Hacmi", "IslemMiktari" };
                    for (int a = 0; a < alanlar.Length; a++)
                    {
                        try
                        {
                            var test = Sistem.GrafikFiyatOku(sembol, "G", alanlar[a]);
                            if (test != null && test.Count > alimBar)
                                hDbg += alanlar[a] + "=" + test[alimBar] + " ";
                            else if (test != null)
                                hDbg += alanlar[a] + "(cnt=" + test.Count + ") ";
                            else
                                hDbg += alanlar[a] + "=null ";
                        }
                        catch { hDbg += alanlar[a] + "=ERR "; }
                    }
                    // GrafikVerileriniOku ile de deneyelim
                    try
                    {
                        var grafik = Sistem.GrafikVerileriniOku(sembol, "G");
                        if (grafik != null)
                        {
                            hDbg += "| GrafVeri: ";
                            try { var v = grafik.Hacim; hDbg += "Hacim=OK "; } catch { hDbg += "Hacim=ERR "; }
                            try { var v = grafik.Volume; hDbg += "Vol=OK "; } catch { hDbg += "Vol=ERR "; }
                        }
                    }
                    catch { hDbg += "| GrafVeri=ERR"; }
                    log += hDbg + "\n";
                }

                // 5. Onceki gun yukselis
                double dunYuk = 0;
                if (oncekiBar > 0)
                {
                    double ikincekiKapanis = kapanislar[oncekiBar - 1];
                    if (ikincekiKapanis > 0) dunYuk = (oncekiKapanis - ikincekiKapanis) / ikincekiKapanis * 100;
                }

                // 6. Ertesi gun acilis farki (gap): (satis gunu acilis - alim gunu kapanis) / alim gunu kapanis
                int satisBar = son; // 13/03
                double satisAcilis = acilislar[satisBar];
                double acilisFark = alimKapanis > 0 ? (satisAcilis - alimKapanis) / alimKapanis * 100 : 0;

                log += string.Format("{0}|{1:F1}|{2:F1}|{3:F0}|{4:F1}x|{5:N0}|{6:F1}|{7:F2}\n",
                    hedefler[i], yukselis, gunIci, cr, hacimOran, alimHacimTL, dunYuk, acilisFark);
            }
            catch
            {
                log += hedefler[i] + "|HATA\n";
            }
        }

        // Karsilastirma: DB'deki Bist100 listesinden tara
        log += "\n=== KARSILASTIRMA (BIST100 + yukselen) ===\n";
        try
        {
            var bist100 = DatabaseManager.Bist100HisselerGetir();
            var hedefSet = new System.Collections.Generic.HashSet<string>();
            for (int h = 0; h < hedefler.Length; h++) hedefSet.Add(hedefler[h]);

            int toplamYukselen = 0;
            string secilmeyenOrnek = "";

            for (int h = 0; h < bist100.Count; h++)
            {
                try
                {
                    string hisse = bist100[h];
                    if (hedefSet.Contains(hisse)) continue;

                    string sembol = "IMKBH'" + hisse;
                    var kap = Sistem.GrafikFiyatOku(sembol, "G", "Kapanis");
                    if (kap == null || kap.Count < 4) continue;
                    int s = kap.Count - 1;
                    int ab = s - 1; // 12/03
                    int ob = s - 2; // 11/03

                    double kC = kap[ab];
                    double kP = kap[ob];
                    if (kP <= 0) continue;
                    double yuk = (kC - kP) / kP * 100;

                    if (yuk >= 1.0)
                    {
                        toplamYukselen++;

                        var ac = Sistem.GrafikFiyatOku(sembol, "G", "Acilis");
                        var yu = Sistem.GrafikFiyatOku(sembol, "G", "Yuksek");
                        var du = Sistem.GrafikFiyatOku(sembol, "G", "Dusuk");

                        double gI = ac != null && ac.Count > ab && ac[ab] > 0 ? (kC - ac[ab]) / ac[ab] * 100 : 0;
                        double cR = 0;
                        if (yu != null && du != null && yu.Count > ab && du.Count > ab)
                        {
                            double rng = yu[ab] - du[ab];
                            if (rng > 0) cR = (kC - du[ab]) / rng * 100;
                        }

                        double aF = 0;
                        if (ac != null && ac.Count > s && kC > 0)
                            aF = (ac[s] - kC) / kC * 100;

                        secilmeyenOrnek += string.Format("{0}|{1:F1}|{2:F1}|{3:F0}|{4:F2}\n",
                            hisse, yuk, gI, cR, aF);
                    }
                }
                catch { }
            }

            log += "BIST100 >=1% yukselen: " + toplamYukselen + "\n";
            log += "Hisse|Yukselis%|GunIci%|CR%|AcilisFark%\n";
            log += secilmeyenOrnek;
        }
        catch (System.Exception ex) { log += "HATA: " + ex.Message; }

        Sistem.Mesaj(log);
    }
}
