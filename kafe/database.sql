-- Kahve Dükkanı Veritabanı Kurulum Scripti
-- MySQL 8.x

CREATE DATABASE IF NOT EXISTS kafe CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci;
USE kafe;

CREATE TABLE IF NOT EXISTS products (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    description TEXT,
    price       DECIMAL(10,2) NOT NULL,
    category    VARCHAR(50) NOT NULL DEFAULT 'Kahve',
    image_path  VARCHAR(500) NULL,
    is_active   TINYINT(1) NOT NULL DEFAULT 1,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Örnek Veriler
INSERT INTO products (name, description, price, category, image_path, is_active) VALUES
('Türk Kahvesi',     'Geleneksel yöntemle hazırlanan, köpüklü Türk kahvesi.',          22.00, 'Kahve',    NULL, 1),
('Espresso',         'Yoğun ve aromatik tek shot espresso.',                            25.00, 'Kahve',    NULL, 1),
('Latte',            'Espresso üzerine sıcak süt köpüğü ile hazırlanan latte.',        38.00, 'Kahve',    NULL, 1),
('Cappuccino',       'Espresso, buharlı süt ve bol süt köpüğünden oluşan cappuccino.', 38.00, 'Kahve',    NULL, 1),
('Americano',        'Espressoya sıcak su eklenerek hazırlanan americano.',             30.00, 'Kahve',    NULL, 1),
('Mocha',            'Çikolata sosu, espresso ve süt köpüğünün harika uyumu.',         42.00, 'Kahve',    NULL, 1),
('Sütlü Latte',      'Ekstra kremali ve az kafeinli beyaz latte.',                     40.00, 'Kahve',    NULL, 1),
('Soğuk Kahve',      'Buzlu, taze demlenen filtre kahve.',                             35.00, 'Kahve',    NULL, 1),
('Çikolatalı Pasta', 'Bol fındıklı, nemli çikolata ganajlı pasta dilimi.',             55.00, 'Pasta',    NULL, 1),
('Cheesecake',       'New York usulü, yaban mersinli cheesecake.',                     60.00, 'Pasta',    NULL, 1),
('Tiramisu',         'İtalyan usulü, mascarpone kremalı tiramisu.',                    65.00, 'Pasta',    NULL, 1),
('Limonlu Kek',      'Taze limon aromalı, şeker glazürlü kek dilimi.',                 45.00, 'Pasta',    NULL, 1),
('Butter Cookie',    'Tereyağlı, gevrek sade kurabiye.',                               18.00, 'Kurabiye', NULL, 1),
('Brownie',          'Çikolatalı, yoğun ve nemli brownie.',                            32.00, 'Kurabiye', NULL, 1),
('Macaron',          'Fransız usulü renkli macaron çifti.',                            28.00, 'Kurabiye', NULL, 1),
('Fıstıklı Kurabiye','Antep fıstığı parçacıklı çıtır kurabiye.',                       22.00, 'Kurabiye', NULL, 1),
('Sütlü Çikolata',   'El yapımı kremalı sütlü çikolata.',                             35.00, 'Çikolata', NULL, 1),
('Bitter Çikolata',  '%70 kakao oranıyla yoğun bitter çikolata.',                     35.00, 'Çikolata', NULL, 1),
('Truffle',          'Kakao tozu kaplı, ganajlı çikolata trüfü (3 adet).',            48.00, 'Çikolata', NULL, 1),
('Çikolata Sos',     'Scone veya krep için sıcak çikolata sos.',                       25.00, 'Çikolata', NULL, 1);

-- Admin tablosu
CREATE TABLE IF NOT EXISTS admin_settings (
    id      INT AUTO_INCREMENT PRIMARY KEY,
    setting_key   VARCHAR(100) NOT NULL UNIQUE,
    setting_value VARCHAR(500) NOT NULL
);

INSERT INTO admin_settings (setting_key, setting_value) VALUES
('admin_password', 'admin123'),
('shop_name', 'Kahve Köşesi')
ON DUPLICATE KEY UPDATE setting_value = VALUES(setting_value);
