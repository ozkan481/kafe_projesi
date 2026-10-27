using Microsoft.Data.Sqlite;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Data
{
    public static class DatabaseHelper
    {
        // DB dosyası exe ile aynı klasörde: kafe.db
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kafe.db");

        private static string ConnStr => $"Data Source={DbPath}";

        // ─── Init (ilk çalıştırmada tabloları ve örnek verileri oluşturur) ──
        public static void Initialize()
        {
            using var conn = Open();

            // Products tablosu
            Exec(conn, """
                CREATE TABLE IF NOT EXISTS products (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT    NOT NULL,
                    description TEXT    NOT NULL DEFAULT '',
                    price       REAL    NOT NULL,
                    category    TEXT    NOT NULL DEFAULT 'Kahve',
                    image_path  TEXT,
                    is_active   INTEGER NOT NULL DEFAULT 1,
                    created_at  TEXT    DEFAULT (datetime('now'))
                );
                """);

            // Admin ayarları tablosu
            Exec(conn, """
                CREATE TABLE IF NOT EXISTS admin_settings (
                    setting_key   TEXT PRIMARY KEY,
                    setting_value TEXT NOT NULL
                );
                """);

            // Varsayılan admin şifresi
            Exec(conn, """
                INSERT OR IGNORE INTO admin_settings (setting_key, setting_value)
                VALUES ('admin_password','admin123'), ('shop_name','Kahve Köşesi');
                """);

            // Örnek ürünler – sadece tablo boşsa ekle
            long count = (long)(Scalar(conn, "SELECT COUNT(*) FROM products") ?? 0L);
            if (count == 0)
                SeedProducts(conn);
        }

        // ─── Products ─────────────────────────────────────────────
        public static List<Product> GetAllProducts(bool activeOnly = true)
        {
            using var conn = Open();
            string sql = activeOnly
                ? "SELECT * FROM products WHERE is_active=1 ORDER BY category,name"
                : "SELECT * FROM products ORDER BY category,name";
            return ReadProducts(conn, sql);
        }

        public static int AddProduct(Product p)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO products (name,description,price,category,image_path,is_active)
                VALUES ($n,$d,$p,$c,$i,$a);
                SELECT last_insert_rowid();
                """;
            BindProduct(cmd, p);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateProduct(Product p)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE products
                SET name=$n, description=$d, price=$p, category=$c, image_path=$i, is_active=$a
                WHERE id=$id;
                """;
            BindProduct(cmd, p);
            cmd.Parameters.AddWithValue("$id", p.Id);
            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new Exception($"Güncelleme başarısız: ID={p.Id} olan ürün bulunamadı.");
        }

        public static void DeleteProduct(int id)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM products WHERE id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ─── Admin Settings ────────────────────────────────────────
        public static string GetSetting(string key, string def = "")
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "SELECT setting_value FROM admin_settings WHERE setting_key=$k";
            cmd.Parameters.AddWithValue("$k", key);
            return cmd.ExecuteScalar()?.ToString() ?? def;
        }

        public static void SetSetting(string key, string value)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO admin_settings (setting_key,setting_value) VALUES ($k,$v)
                ON CONFLICT(setting_key) DO UPDATE SET setting_value=$v;
                """;
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }

        // ─── Helpers ───────────────────────────────────────────────
        private static SqliteConnection Open()
        {
            var conn = new SqliteConnection(ConnStr);
            conn.Open();
            return conn;
        }

        private static void Exec(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static object? Scalar(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        private static void BindProduct(SqliteCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("$n", p.Name);
            cmd.Parameters.AddWithValue("$d", p.Description);
            cmd.Parameters.AddWithValue("$p", (double)p.Price);   // decimal→double: SQLite REAL tipi gerektirir
            cmd.Parameters.AddWithValue("$c", p.Category);
            cmd.Parameters.AddWithValue("$i", (object?)p.ImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a", p.IsActive ? 1 : 0);
        }

        private static List<Product> ReadProducts(SqliteConnection conn, string sql)
        {
            var list = new List<Product>();
            using var cmd    = conn.CreateCommand();
            cmd.CommandText  = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Product
                {
                    Id          = reader.GetInt32(reader.GetOrdinal("id")),
                    Name        = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    Price       = reader.GetDecimal(reader.GetOrdinal("price")),
                    Category    = reader.GetString(reader.GetOrdinal("category")),
                    ImagePath   = reader.IsDBNull(reader.GetOrdinal("image_path")) ? null : reader.GetString(reader.GetOrdinal("image_path")),
                    IsActive    = reader.GetInt32(reader.GetOrdinal("is_active")) == 1
                });
            }
            return list;
        }

        // ─── Seed Data ─────────────────────────────────────────────
        private static void SeedProducts(SqliteConnection conn)
        {
            var products = new (string name, string desc, decimal price, string cat)[]
            {
                ("Türk Kahvesi",     "Geleneksel yöntemle hazırlanan köpüklü Türk kahvesi.",          22.00m, "Kahve"),
                ("Espresso",         "Yoğun ve aromatik tek shot espresso.",                            25.00m, "Kahve"),
                ("Latte",            "Espresso üzerine sıcak süt köpüğü ile hazırlanan latte.",        38.00m, "Kahve"),
                ("Cappuccino",       "Espresso, buharlı süt ve bol süt köpüğünden oluşur.",            38.00m, "Kahve"),
                ("Americano",        "Espressoya sıcak su eklenerek hazırlanan americano.",             30.00m, "Kahve"),
                ("Mocha",            "Çikolata sosu, espresso ve süt köpüğünün harika uyumu.",         42.00m, "Kahve"),
                ("White Latte",      "Ekstra kremali ve az kafeinli beyaz latte.",                     40.00m, "Kahve"),
                ("Soğuk Kahve",      "Buzlu, taze demlenen filtre kahve.",                             35.00m, "Kahve"),
                ("Çikolatalı Pasta", "Bol fındıklı, nemli çikolata ganajlı pasta dilimi.",             55.00m, "Pasta"),
                ("Cheesecake",       "New York usulü, yaban mersinli cheesecake.",                     60.00m, "Pasta"),
                ("Tiramisu",         "İtalyan usulü, mascarpone kremalı tiramisu.",                    65.00m, "Pasta"),
                ("Limonlu Kek",      "Taze limon aromalı, şeker glazürlü kek dilimi.",                 45.00m, "Pasta"),
                ("Butter Cookie",    "Tereyağlı, gevrek sade kurabiye.",                               18.00m, "Kurabiye"),
                ("Brownie",          "Çikolatalı, yoğun ve nemli brownie.",                            32.00m, "Kurabiye"),
                ("Macaron",          "Fransız usulü renkli macaron çifti.",                            28.00m, "Kurabiye"),
                ("Fıstıklı Kurabiye","Antep fıstığı parçacıklı çıtır kurabiye.",                       22.00m, "Kurabiye"),
                ("Sütlü Çikolata",   "El yapımı kremalı sütlü çikolata.",                             35.00m, "Çikolata"),
                ("Bitter Çikolata",  "%70 kakao oranıyla yoğun bitter çikolata.",                     35.00m, "Çikolata"),
                ("Truffle",          "Kakao tozu kaplı, ganajlı çikolata trüfü (3 adet).",            48.00m, "Çikolata"),
                ("Çikolata Sos",     "Scone veya krep için sıcak çikolata sos.",                       25.00m, "Çikolata"),
            };

            foreach (var (name, desc, price, cat) in products)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO products (name,description,price,category,is_active)
                    VALUES ($n,$d,$p,$c,1)
                    """;
                cmd.Parameters.AddWithValue("$n", name);
                cmd.Parameters.AddWithValue("$d", desc);
                cmd.Parameters.AddWithValue("$p", (double)price);
                cmd.Parameters.AddWithValue("$c", cat);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
