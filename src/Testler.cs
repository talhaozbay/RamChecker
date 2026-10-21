// Gömülü test koşucusu: RamChecker.exe --test
// Her modülün test sınıfı (SmbiosTestleri, UreticiTestleri, PlatformTestleri,
// DanismanTestleri, RaporTestleri, ToplayiciTestleri) burada sırayla çağrılır.
using System;
using System.Collections.Generic;

namespace RamDanismani
{
    public class TestKosucu
    {
        public int Gecen;
        public int Kalan;
        public List<string> Hatalar = new List<string>();
        string grup = "";

        public void Grup(string ad)
        {
            grup = ad;
            Console.WriteLine("== " + ad);
        }

        public void Dogru(bool kosul, string ad)
        {
            if (kosul) { Gecen++; return; }
            Kalan++;
            string m = "[" + grup + "] " + ad;
            Hatalar.Add(m);
            Console.WriteLine("  BAŞARISIZ: " + ad);
        }

        public void Esit(object beklenen, object gercek, string ad)
        {
            bool esit = object.Equals(beklenen, gercek);
            if (!esit && beklenen != null && gercek != null)
                esit = beklenen.ToString() == gercek.ToString();
            if (esit) { Gecen++; return; }
            Kalan++;
            string m = "[" + grup + "] " + ad + " — beklenen: <" + beklenen + ">, gelen: <" + gercek + ">";
            Hatalar.Add(m);
            Console.WriteLine("  BAŞARISIZ: " + ad + " — beklenen: <" + beklenen + ">, gelen: <" + gercek + ">");
        }

        public void Icerir(string metin, string parca, string ad)
        {
            bool var = metin != null && parca != null && metin.IndexOf(parca, StringComparison.Ordinal) >= 0;
            Dogru(var, ad + " (\"" + parca + "\" bekleniyordu)");
        }

        public void Icermez(string metin, string parca, string ad)
        {
            bool yok = metin == null || parca == null || metin.IndexOf(parca, StringComparison.Ordinal) < 0;
            Dogru(yok, ad + " (\"" + parca + "\" olmamalıydı)");
        }

        /// <summary>Test gövdesi istisna fırlatırsa diğer testler yine de koşsun.</summary>
        public void Kos(string ad, Action govde)
        {
            try { govde(); }
            catch (Exception ex)
            {
                Kalan++;
                string m = "[" + grup + "] " + ad + " istisna: " + ex.GetType().Name + ": " + ex.Message;
                Hatalar.Add(m);
                Console.WriteLine("  İSTİSNA: " + ad + " — " + ex);
            }
        }
    }

    public static class Testler
    {
        public static int Calistir()
        {
            TestKosucu t = new TestKosucu();
            t.Kos("SmbiosTestleri", delegate { SmbiosTestleri.Calistir(t); });
            t.Kos("UreticiTestleri", delegate { UreticiTestleri.Calistir(t); });
            t.Kos("PlatformTestleri", delegate { PlatformTestleri.Calistir(t); });
            t.Kos("ToplayiciTestleri", delegate { ToplayiciTestleri.Calistir(t); });
            t.Kos("DanismanTestleri", delegate { DanismanTestleri.Calistir(t); });
            t.Kos("RaporTestleri", delegate { RaporTestleri.Calistir(t); });

            Console.WriteLine();
            Console.WriteLine("Geçen: " + t.Gecen + "  Kalan: " + t.Kalan);
            if (t.Kalan > 0)
            {
                Console.WriteLine("Başarısız testler:");
                foreach (string h in t.Hatalar) Console.WriteLine("  - " + h);
            }
            return t.Kalan == 0 ? 0 : 1;
        }
    }
}
