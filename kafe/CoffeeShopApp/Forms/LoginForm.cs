using System;
using System.Drawing;
using System.Windows.Forms;
using CoffeeShopApp.Data;

namespace CoffeeShopApp.Forms
{
    public class LoginForm : Form
    {
        // ── Colours ────────────────────────────────────────────────
        private static readonly Color BgDark     = Color.FromArgb(28, 15, 10);
        private static readonly Color CardBg     = Color.FromArgb(44, 24, 16);
        private static readonly Color GoldColor  = Color.FromArgb(212, 165, 116);
        private static readonly Color TextPrimary= Color.FromArgb(245, 230, 211);

        private TextBox txtPassword = null!;
        private Button  btnLogin    = null!;
        private Label   lblError    = null!;

        public LoginForm()
        {
            InitUI();
        }

        private void InitUI()
        {
            Text             = "Admin Giriş – Kahve Köşesi";
            Size             = new Size(420, 320);
            StartPosition    = FormStartPosition.CenterScreen;
            FormBorderStyle  = FormBorderStyle.FixedDialog;
            MaximizeBox      = false;
            BackColor        = BgDark;
            Icon             = SystemIcons.Application;

            // Logo label
            var lblLogo = new Label
            {
                Text      = "☕",
                Font      = new Font("Segoe UI Emoji", 36),
                ForeColor = GoldColor,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 20, 420, 70)
            };

            var lblTitle = new Label
            {
                Text      = "Admin Paneli",
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = GoldColor,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 95, 420, 36)
            };

            var lblSubtitle = new Label
            {
                Text      = "Lütfen admin şifresini giriniz",
                Font      = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(160, 128, 112),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 130, 420, 22)
            };

            txtPassword = new TextBox
            {
                Bounds       = new Rectangle(60, 168, 300, 36),
                PasswordChar = '*',
                Font         = new Font("Segoe UI", 12),
                BackColor    = Color.FromArgb(60, 36, 24),
                ForeColor    = TextPrimary,
                BorderStyle  = BorderStyle.FixedSingle
            };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };

            btnLogin = new Button
            {
                Text      = "GİRİŞ YAP",
                Bounds    = new Rectangle(60, 218, 300, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = GoldColor,
                ForeColor = BgDark,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => TryLogin();

            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(220, 80, 60),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 266, 420, 24),
                Font      = new Font("Segoe UI", 9)
            };

            Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSubtitle, txtPassword, btnLogin, lblError });
        }

        private void TryLogin()
        {
            try
            {
                string correct = DatabaseHelper.GetSetting("admin_password", "admin123");
                if (txtPassword.Text.Trim() == correct.Trim())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    lblError.Text = "❌ Hatalı şifre! Lütfen tekrar deneyiniz.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "⚠ DB Hatası: " + ex.Message;
            }
        }
    }
}
