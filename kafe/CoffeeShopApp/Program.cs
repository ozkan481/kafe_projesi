using CoffeeShopApp.Data;
using CoffeeShopApp.Forms;

namespace CoffeeShopApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // SQLite veritabanını ilk çalıştırmada otomatik oluşturur
            // Sonraki çalıştırmalarda mevcut veriyi korur
            DatabaseHelper.Initialize();

            Application.Run(new MainForm());
        }
    }
}