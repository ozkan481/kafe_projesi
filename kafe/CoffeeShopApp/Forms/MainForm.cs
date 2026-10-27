using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CoffeeShopApp.Data;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Forms
{
    public class MainForm : Form
    {
        // ── Theme ──────────────────────────────────────────────────
        private static readonly Color BgDark      = Color.FromArgb(20,  10,   6);
        private static readonly Color HeaderBg    = Color.FromArgb(36,  18,  10);
        private static readonly Color CardBg      = Color.FromArgb(52,  30,  18);
        private static readonly Color CardHover   = Color.FromArgb(68,  42,  24);
        private static readonly Color Gold        = Color.FromArgb(212, 165, 116);
        private static readonly Color GoldBright  = Color.FromArgb(240, 196, 120);
        private static readonly Color TextPrimary = Color.FromArgb(245, 230, 210);
        private static readonly Color TextSec     = Color.FromArgb(160, 128, 100);
        private static readonly Color AddBtnColor = Color.FromArgb(180, 120,  60);
        private static readonly Color AddBtnHover = Color.FromArgb(210, 150,  80);
        private static readonly Color CartBg      = Color.FromArgb(32,  16,   8);
        private static readonly Color DividerColor= Color.FromArgb(70,  40,  22);
        private static readonly Color RemoveRed   = Color.FromArgb(180,  50,  50);

        // ── Pagination ─────────────────────────────────────────────
        private const int COLS = 4;
        private const int ROWS = 2;
        private const int PAGE_SIZE = COLS * ROWS; // 8
        private int currentPage  = 0;
        private int totalPages   = 1;

        // ── Data ───────────────────────────────────────────────────
        private List<Product>  allProducts      = new();
        private List<Product>  filteredProducts = new();
        private List<CartItem> cartItems        = new();

        // ── Layout ─────────────────────────────────────────────────
        private Panel   headerPanel    = null!;
        private Panel   contentPanel   = null!;   // left side
        private Panel   cartPanel      = null!;   // right side
        private Panel   gridArea       = null!;   // 4x2 product cards
        private Panel   pagingPanel    = null!;
        private TextBox txtSearch      = null!;
        private Label   lblPageInfo    = null!;
        private Button  btnPrev        = null!;
        private Button  btnNext        = null!;
        private Panel   cartListPanel  = null!;
        private Label   lblTotal       = null!;
        private Label   lblCartCount   = null!;
        private Label   lblEmptyCart   = null!;
        private Button  btnAdmin       = null!;
        private Button  btnOrder       = null!;

        // Card size
        private const int CARD_W = 210;
        private const int CARD_H = 295;

        public MainForm()
        {
            InitUI();
            LoadProducts();
        }

        // ══════════════════════════════════════════════════════════════
        // UI INIT
        // ══════════════════════════════════════════════════════════════
        private void InitUI()
        {
            Text             = "Kahve Köşesi – Sipariş Ekranı";
            Size             = new Size(1280, 820);
            MinimumSize      = new Size(1200, 700);
            StartPosition    = FormStartPosition.CenterScreen;
            BackColor        = BgDark;
            Icon             = SystemIcons.Application;
            DoubleBuffered   = true;

            // ── HEADER ──────────────────────────────────────────────
            headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = HeaderBg
            };
            headerPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Gold, 2), 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            // Left side: logo + title
            var lblLogo = new Label
            {
                Text      = "☕",
                Font      = new Font("Segoe UI Emoji", 24),
                ForeColor = Gold,
                AutoSize  = true,
                Location  = new Point(20, 14)
            };
            var lblTitle = new Label
            {
                Text      = "Kahve Köşesi",
                Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = true,
                Location  = new Point(66, 20)
            };
            var lblSubtitle = new Label
            {
                Text      = "Lezzet dolu bir deneyim",
                Font      = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = TextSec,
                AutoSize  = true,
                Location  = new Point(68, 46)
            };

            // Right side: FlowLayoutPanel with search + admin button (anchored right)
            var rightFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Anchor        = AnchorStyles.Top | AnchorStyles.Right,
                Padding       = new Padding(0),
                Margin        = new Padding(0)
            };

            // Search box
            var searchContainer = new Panel
            {
                Size      = new Size(280, 38),
                BackColor = CardBg,
                Margin    = new Padding(0, 0, 8, 0)
            };
            var lblSearchIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Segoe UI Emoji", 12),
                ForeColor = TextSec,
                AutoSize  = false,
                Size      = new Size(34, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(4, 0)
            };
            txtSearch = new TextBox
            {
                Font            = new Font("Segoe UI", 11),
                BackColor       = CardBg,
                ForeColor       = TextPrimary,
                BorderStyle     = BorderStyle.None,
                Size            = new Size(236, 32),
                Location        = new Point(40, 6),
                PlaceholderText = "Ürün ara (örn. Latte)..."
            };
            txtSearch.TextChanged += (s, e) => { currentPage = 0; Filter(txtSearch.Text); };
            searchContainer.Controls.AddRange(new Control[] { lblSearchIcon, txtSearch });

            // Admin button
            btnAdmin = new Button
            {
                Text      = "⚙  Admin",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(80, 48, 24),
                ForeColor = Gold,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(110, 38),
                Margin    = new Padding(0),
                Cursor    = Cursors.Hand
            };
            btnAdmin.FlatAppearance.BorderColor = DividerColor;
            btnAdmin.FlatAppearance.BorderSize  = 1;
            btnAdmin.Click += (s, e) => OpenAdmin();

            rightFlow.Controls.Add(searchContainer);
            rightFlow.Controls.Add(btnAdmin);

            // Position rightFlow: anchored right, so we place it and let Anchor handle reflow
            headerPanel.Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSubtitle, rightFlow });

            // Set initial position of rightFlow, then keep it updated on resize
            void PositionRightFlow()
            {
                rightFlow.Location = new Point(
                    headerPanel.ClientSize.Width - rightFlow.PreferredSize.Width - 16,
                    16);
            }
            headerPanel.Layout  += (s, e) => PositionRightFlow();
            headerPanel.Resize  += (s, e) => PositionRightFlow();

            // ── MAIN SPLIT ──────────────────────────────────────────
            var mainRow = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

            // Right: Cart
            cartPanel = BuildCartPanel();
            cartPanel.Dock = DockStyle.Right;
            cartPanel.Width = 320;

            // Left: products + paging
            contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgDark,
                Padding   = new Padding(16, 12, 12, 8)
            };

            // Paging at bottom of left
            pagingPanel = BuildPagingPanel();
            pagingPanel.Dock = DockStyle.Bottom;

            // Grid area
            gridArea = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgDark
            };

            contentPanel.Controls.Add(gridArea);
            contentPanel.Controls.Add(pagingPanel);

            mainRow.Controls.Add(contentPanel);
            mainRow.Controls.Add(cartPanel);

            Controls.Add(mainRow);
            Controls.Add(headerPanel);

            Resize += (s, e) => RenderProductCards();
        }

        // ── Cart panel ─────────────────────────────────────────────
        private Panel BuildCartPanel()
        {
            var panel = new Panel { BackColor = CartBg };
            panel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(DividerColor, 2), 0, 0, 0, panel.Height);
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(44, 20, 10) };
            var lblCart = new Label
            {
                Text      = "🛒  Sipariş Sepeti",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = true,
                Location  = new Point(12, 10)
            };
            lblCartCount = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 9),
                ForeColor = TextSec,
                AutoSize  = true,
                Location  = new Point(14, 36)
            };
            hdr.Controls.AddRange(new Control[] { lblCart, lblCartCount });

            // Scrollable items
            cartListPanel = new Panel
            {
                Dock            = DockStyle.Fill,
                BackColor       = CartBg,
                AutoScroll      = true,
                Padding         = new Padding(6, 4, 4, 4)
            };

            lblEmptyCart = new Label
            {
                Text      = "Sepetiniz boş.\nÜrün eklemek için\nmenüden seçim yapın.",
                Font      = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = TextSec,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                Visible   = true
            };
            cartListPanel.Controls.Add(lblEmptyCart);

            // Bottom total + order
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 130, BackColor = Color.FromArgb(44, 20, 10) };
            bottomPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(DividerColor, 1), 0, 0, bottomPanel.Width, 0);
            };

            var lblTotalLabel = new Label
            {
                Text      = "TOPLAM:",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextSec,
                AutoSize  = true,
                Location  = new Point(12, 16)
            };
            lblTotal = new Label
            {
                Text      = "0,00 ₺",
                Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = true,
                Location  = new Point(12, 36)
            };

            btnOrder = new Button
            {
                Text      = "✔  SİPARİŞİ TAMAMLA",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(76, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Bottom,
                Height    = 48,
                Cursor    = Cursors.Hand
            };
            btnOrder.FlatAppearance.BorderSize = 0;
            btnOrder.Click += BtnOrder_Click;

            var btnClear = new Button
            {
                Text      = "🗑  Sepeti Temizle",
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(100, 40, 30),
                ForeColor = Color.FromArgb(220, 150, 130),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Bottom,
                Height    = 32,
                Cursor    = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => { cartItems.Clear(); RefreshCart(); };

            bottomPanel.Controls.AddRange(new Control[] { lblTotalLabel, lblTotal, btnClear, btnOrder });

            panel.Controls.Add(cartListPanel);
            panel.Controls.Add(bottomPanel);
            panel.Controls.Add(hdr);
            return panel;
        }

        // ── Paging panel ───────────────────────────────────────────
        private Panel BuildPagingPanel()
        {
            var paging = new Panel { Height = 52, BackColor = BgDark };

            btnPrev = MakePagingBtn("◀ Önceki");
            btnPrev.Location = new Point(0, 8);
            btnPrev.Click    += (s, e) => { if (currentPage > 0) { currentPage--; RenderProductCards(); } };

            lblPageInfo = new Label
            {
                Text      = "Sayfa 1 / 1",
                Font      = new Font("Segoe UI", 10),
                ForeColor = TextSec,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(150, 36),
                Location  = new Point(140, 8)
            };

            btnNext = MakePagingBtn("Sonraki ▶");
            btnNext.Location = new Point(300, 8);
            btnNext.Click    += (s, e) => { if (currentPage < totalPages - 1) { currentPage++; RenderProductCards(); } };

            paging.Controls.AddRange(new Control[] { btnPrev, lblPageInfo, btnNext });
            return paging;
        }

        // ══════════════════════════════════════════════════════════════
        // DATA
        // ══════════════════════════════════════════════════════════════
        private void LoadProducts()
        {
            try
            {
                allProducts      = DatabaseHelper.GetAllProducts(true);
                filteredProducts = allProducts.ToList();
                currentPage      = 0;
                RenderProductCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veritabanı bağlantı hatası:\n" + ex.Message +
                    "\n\nLütfen MySQL'in çalıştığından ve database.sql scriptinin çalıştırıldığından emin olun.",
                    "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Filter(string kw)
        {
            filteredProducts = string.IsNullOrWhiteSpace(kw)
                ? allProducts.ToList()
                : allProducts.Where(p =>
                    p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            currentPage = 0;
            RenderProductCards();
        }

        // ══════════════════════════════════════════════════════════════
        // PRODUCT CARDS RENDERING
        // ══════════════════════════════════════════════════════════════
        private void RenderProductCards()
        {
            if (gridArea == null) return;

            gridArea.SuspendLayout();
            gridArea.Controls.Clear();

            totalPages = Math.Max(1, (int)Math.Ceiling(filteredProducts.Count / (double)PAGE_SIZE));
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            lblPageInfo.Text = $"Sayfa {currentPage + 1} / {totalPages}";
            btnPrev.Enabled  = currentPage > 0;
            btnNext.Enabled  = currentPage < totalPages - 1;

            var page = filteredProducts.Skip(currentPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            if (page.Count == 0)
            {
                var lbl = new Label
                {
                    Text      = filteredProducts.Count == 0
                        ? "🔍 Arama sonucu bulunamadı.\nFarklı bir anahtar kelime deneyin."
                        : "Menüde ürün bulunmuyor.",
                    Font      = new Font("Segoe UI", 13, FontStyle.Italic),
                    ForeColor = TextSec,
                    AutoSize  = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock      = DockStyle.Fill
                };
                gridArea.Controls.Add(lbl);
                gridArea.ResumeLayout();
                return;
            }

            // Calculate card layout
            int areaW    = gridArea.ClientSize.Width  - gridArea.Padding.Horizontal;
            int areaH    = gridArea.ClientSize.Height - gridArea.Padding.Vertical;
            int hPad     = gridArea.Padding.Left;
            int vPad     = gridArea.Padding.Top;

            int gapX = Math.Max(8, (areaW - COLS * CARD_W) / (COLS + 1));
            int gapY = Math.Max(8, (areaH - ROWS * CARD_H) / (ROWS + 1));

            for (int i = 0; i < page.Count; i++)
            {
                int col = i % COLS;
                int row = i / COLS;
                int x   = hPad + gapX + col * (CARD_W + gapX);
                int y   = vPad + gapY + row * (CARD_H + gapY);

                var card = BuildProductCard(page[i]);
                card.Location = new Point(x, y);
                gridArea.Controls.Add(card);
            }

            gridArea.ResumeLayout();
        }

        private Panel BuildProductCard(Product p)
        {
            var card = new Panel
            {
                Size      = new Size(CARD_W, CARD_H),
                BackColor = CardBg,
                Cursor    = Cursors.Default
            };

            // Hover effect
            card.MouseEnter += (s, e) => { ((Panel)s!).BackColor = CardHover; };
            card.MouseLeave += (s, e) => { ((Panel)s!).BackColor = CardBg; };

            // Rounded-ish border via Paint
            card.Paint += (s, e) =>
            {
                var g   = e.Graphics;
                var pen = new Pen(Color.FromArgb(80, 50, 28), 1);
                g.DrawRectangle(pen, 0, 0, CARD_W - 1, CARD_H - 1);
                pen.Dispose();
            };

            // Category badge
            var badge = new Label
            {
                Text      = CategoryEmoji(p.Category) + " " + p.Category,
                Font      = new Font("Segoe UI Emoji", 7, FontStyle.Bold),
                ForeColor = GoldBright,
                BackColor = Color.FromArgb(60, 35, 12),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(CARD_W, 22),
                Location  = new Point(0, 0)
            };

            // Image
            var pic = new PictureBox
            {
                Size      = new Size(CARD_W, 138),
                Location  = new Point(0, 22),
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(38, 20, 12)
            };
            LoadCardImage(pic, p.ImagePath, p.Category);
            pic.MouseEnter += (s, e) => { card.BackColor = CardHover; };
            pic.MouseLeave += (s, e) => { card.BackColor = CardBg; };

            // Name
            var lblName = new Label
            {
                Text      = p.Name,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size      = new Size(CARD_W - 16, 40),
                Location  = new Point(8, 164),
                UseMnemonic = false
            };

            // Description
            var lblDesc = new Label
            {
                Text      = p.Description,
                Font      = new Font("Segoe UI", 8),
                ForeColor = TextSec,
                AutoSize  = false,
                TextAlign = ContentAlignment.TopLeft,
                Size      = new Size(CARD_W - 16, 40),
                Location  = new Point(8, 200),
                UseMnemonic = false
            };

            // Price
            var lblPrice = new Label
            {
                Text      = p.Price.ToString("N2") + " ₺",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = true,
                Location  = new Point(8, 242)
            };

            // EKLE button
            var btnAdd = new Button
            {
                Text      = "+ EKLE",
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = AddBtnColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(78, 32),
                Location  = new Point(CARD_W - 86, 256),
                Cursor    = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.MouseEnter += (s, e) => { ((Button)s!).BackColor = AddBtnHover; };
            btnAdd.MouseLeave += (s, e) => { ((Button)s!).BackColor = AddBtnColor; };
            btnAdd.Click += (s, e) => AddToCart(p);

            card.Controls.AddRange(new Control[] { badge, pic, lblName, lblDesc, lblPrice, btnAdd });

            // Make whole card propagate hover
            foreach (Control c in card.Controls)
            {
                if (c == btnAdd) continue;
                c.MouseEnter += (s, e) => card.BackColor = CardHover;
                c.MouseLeave += (s, e) => card.BackColor = CardBg;
            }

            return card;
        }

        private void LoadCardImage(PictureBox pic, string? path, string category)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    using var ms = new MemoryStream(File.ReadAllBytes(path));
                    pic.Image = Image.FromStream(ms);
                    return;
                }
                catch { /* fall through */ }
            }
            // Default placeholder based on category
            pic.Image = null;
            pic.Paint += (s, e) =>
            {
                var g    = e.Graphics;
                g.Clear(Color.FromArgb(38, 20, 12));
                var emoji = CategoryEmoji(category) + "\n" + category;
                var sf    = new System.Drawing.StringFormat
                {
                    Alignment     = System.Drawing.StringAlignment.Center,
                    LineAlignment = System.Drawing.StringAlignment.Center
                };
                using var fnt = new Font("Segoe UI Emoji", 28);
                using var fnt2 = new Font("Segoe UI", 9);
                var r1 = new RectangleF(0, 10, CARD_W, 80);
                var r2 = new RectangleF(0, 90, CARD_W, 40);
                g.DrawString(CategoryEmoji(category), fnt,  new SolidBrush(Gold),     r1, sf);
                g.DrawString(category,                fnt2, new SolidBrush(TextSec),  r2, sf);
            };
        }

        private static string CategoryEmoji(string cat) => cat switch
        {
            "Kahve"    => "☕",
            "Pasta"    => "🎂",
            "Kurabiye" => "🍪",
            "Çikolata" => "🍫",
            _          => "🍽"
        };

        // ══════════════════════════════════════════════════════════════
        // CART LOGIC
        // ══════════════════════════════════════════════════════════════
        private void AddToCart(Product p)
        {
            var existing = cartItems.FirstOrDefault(c => c.Product.Id == p.Id);
            if (existing != null)
                existing.Quantity++;
            else
                cartItems.Add(new CartItem(p));
            RefreshCart();
        }

        private void RefreshCart()
        {
            cartListPanel.SuspendLayout();
            cartListPanel.Controls.Clear();

            lblEmptyCart.Visible = cartItems.Count == 0;

            if (cartItems.Count == 0)
            {
                cartListPanel.Controls.Add(lblEmptyCart);
                lblTotal.Text     = "0,00 ₺";
                lblCartCount.Text = "";
                cartListPanel.ResumeLayout();
                return;
            }

            lblCartCount.Text = $"{cartItems.Sum(c => c.Quantity)} ürün";

            int y = 4;
            foreach (var item in cartItems.ToList())
            {
                var row = BuildCartRow(item);
                row.Location = new Point(0, y);
                cartListPanel.Controls.Add(row);
                y += row.Height + 4;
            }

            decimal total = cartItems.Sum(c => c.LineTotal);
            lblTotal.Text = total.ToString("N2") + " ₺";

            cartListPanel.ResumeLayout();
        }

        private Panel BuildCartRow(CartItem item)
        {
            var row = new Panel
            {
                Size      = new Size(cartPanel.Width - 20, 72),
                BackColor = Color.FromArgb(48, 26, 14),
                Padding   = new Padding(6)
            };

            var lblName = new Label
            {
                Text      = item.Product.Name,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize  = false,
                Size      = new Size(row.Width - 90, 20),
                Location  = new Point(6, 6),
                UseMnemonic = false
            };

            var lblUnitPrice = new Label
            {
                Text      = item.Product.Price.ToString("N2") + " ₺ / adet",
                Font      = new Font("Segoe UI", 8),
                ForeColor = TextSec,
                AutoSize  = true,
                Location  = new Point(6, 24)
            };

            // Qty controls
            var btnMinus = MakeSmallBtn("−", Color.FromArgb(80, 46, 20));
            btnMinus.Location = new Point(6, 46);
            btnMinus.Click += (s, e) =>
            {
                if (item.Quantity > 1) item.Quantity--;
                else cartItems.Remove(item);
                RefreshCart();
            };

            var lblQty = new Label
            {
                Text      = item.Quantity.ToString(),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(32, 26),
                Location  = new Point(36, 46)
            };

            var btnPlus = MakeSmallBtn("+", Color.FromArgb(80, 46, 20));
            btnPlus.Location = new Point(70, 46);
            btnPlus.Click += (s, e) =>
            {
                item.Quantity++;
                RefreshCart();
            };

            var lblLineTotal = new Label
            {
                Text      = item.LineTotal.ToString("N2") + " ₺",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = GoldBright,
                AutoSize  = true,
                Location  = new Point(108, 48)
            };

            var btnRemove = new Button
            {
                Text      = "✕",
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = RemoveRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(26, 26),
                Location  = new Point(row.Width - 36, 6),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Click += (s, e) => { cartItems.Remove(item); RefreshCart(); };

            row.Controls.AddRange(new Control[] { lblName, lblUnitPrice, btnMinus, lblQty, btnPlus, lblLineTotal, btnRemove });

            // Draw bottom border
            row.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(DividerColor, 1), 0, row.Height - 1, row.Width, row.Height - 1);
            };

            return row;
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════
        private void BtnOrder_Click(object? sender, EventArgs e)
        {
            if (cartItems.Count == 0) { MessageBox.Show("Sepetiniz boş!", "Sipariş", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            decimal total = cartItems.Sum(c => c.LineTotal);
            var msg = "SİPARİŞ ÖZETI\n" + new string('─', 30) + "\n";
            foreach (var ci in cartItems)
                msg += $"{ci.Product.Name} x{ci.Quantity}  →  {ci.LineTotal:N2} ₺\n";
            msg += new string('─', 30) + $"\nTOPLAM: {total:N2} ₺\n\nSiparişi onaylıyor musunuz?";

            var res = MessageBox.Show(msg, "Sipariş Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                MessageBox.Show("✔ Siparişiniz alındı! Afiyet olsun 😊", "Teşekkürler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cartItems.Clear();
                RefreshCart();
            }
        }

        private void OpenAdmin()
        {
            try
            {
                using var login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK) return;
                using var admin = new AdminForm();
                admin.ShowDialog();
                // Reload products after admin may have changed them
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Admin paneli açılırken bir hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════
        private Button MakePagingBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10),
                BackColor = CardBg,
                ForeColor = Gold,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(120, 36),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = DividerColor;
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private Button MakeHeaderBtn(string text, int width)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(60, 36, 20),
                ForeColor = Gold,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(width, 34),
                Location  = new Point(0, 18),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = DividerColor;
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private Button MakeSmallBtn(string text, Color back)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = back,
                ForeColor = GoldBright,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(28, 26),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
