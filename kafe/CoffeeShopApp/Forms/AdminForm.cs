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
    public class AdminForm : Form
    {
        // ── Theme ──────────────────────────────────────────────────
        private static readonly Color BgDark      = Color.FromArgb(28,  15,  10);
        private static readonly Color PanelBg     = Color.FromArgb(44,  24,  16);
        private static readonly Color CardBg      = Color.FromArgb(60,  36,  24);
        private static readonly Color Gold        = Color.FromArgb(212, 165, 116);
        private static readonly Color GoldBright  = Color.FromArgb(240, 192, 112);
        private static readonly Color TextPrimary = Color.FromArgb(245, 230, 211);
        private static readonly Color TextSec     = Color.FromArgb(160, 128, 112);
        private static readonly Color DangerRed   = Color.FromArgb(200,  60,  60);
        private static readonly Color SuccessGrn  = Color.FromArgb( 76, 175,  80);

        // ── Controls ───────────────────────────────────────────────
        private DataGridView dgv = null!;
        private Panel        detailPanel = null!;
        private PictureBox   picPreview  = null!;
        private TextBox      txtName     = null!;
        private TextBox      txtDesc     = null!;
        private TextBox      txtPrice    = null!;
        private ComboBox     cmbCategory = null!;
        private CheckBox     chkActive   = null!;
        private Button       btnSave     = null!;
        private Button       btnDelete   = null!;
        private Button       btnNew      = null!;
        private Button       btnPickImg  = null!;
        private Button       btnClearImg = null!;
        private Label        lblStatus   = null!;
        private TextBox      txtSearch   = null!;

        private List<Product> allProducts = new();
        private Product?      editing     = null;
        private string?       pendingImagePath = null;  // path chosen but not yet saved

        public AdminForm()
        {
            InitUI();
            LoadGrid();
        }

        // ══════════════════════════════════════════════════════════════
        // UI INIT
        // ══════════════════════════════════════════════════════════════
        private void InitUI()
        {
            Text            = "Yönetim Paneli – Kahve Köşesi";
            Size            = new Size(1260, 760);
            MinimumSize     = new Size(1100, 700);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = BgDark;
            Icon            = SystemIcons.Application;

            // ── Header ──────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = PanelBg };

            var lblTitle = new Label
            {
                Text      = "☕  Kahve Köşesi – Ürün Yönetimi",
                Font      = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Gold,
                AutoSize  = true,
                Location  = new Point(20, 16)
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Ürün ara...",
                Font            = new Font("Segoe UI", 11),
                BackColor       = CardBg,
                ForeColor       = TextPrimary,
                BorderStyle     = BorderStyle.FixedSingle,
                Size            = new Size(240, 32),
                Anchor          = AnchorStyles.Right | AnchorStyles.Top
            };
            txtSearch.Location = new Point(header.Width - 270, 14);
            txtSearch.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text);

            header.Controls.AddRange(new Control[] { lblTitle, txtSearch });
            header.Resize += (s, e) => txtSearch.Location = new Point(header.Width - 270, 14);

            // ── Main Layout: left=grid, right=detail ────────────────
            var bodyPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgDark
            };

            // RIGHT: Detail panel (fixed width, dedicated area for product CRUD)
            detailPanel = new Panel
            {
                Dock       = DockStyle.Right,
                Width      = 440,
                Padding    = new Padding(16),
                BackColor  = PanelBg,
                AutoScroll = true
            };
            BuildDetailPanel();

            // LEFT: DataGridView
            dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = PanelBg,
                GridColor             = Color.FromArgb(70, 45, 30),
                BorderStyle           = BorderStyle.None,
                DefaultCellStyle      = new DataGridViewCellStyle
                {
                    BackColor  = CardBg,
                    ForeColor  = TextPrimary,
                    Font       = new Font("Segoe UI", 10),
                    SelectionBackColor = Color.FromArgb(100, 60, 30),
                    SelectionForeColor = GoldBright
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor  = PanelBg,
                    ForeColor  = Gold,
                    Font       = new Font("Segoe UI", 10, FontStyle.Bold),
                    SelectionBackColor = PanelBg,
                    SelectionForeColor = Gold
                },
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars            = ScrollBars.Vertical
            };
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 38;
            dgv.RowTemplate.Height  = 38;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "ID",       FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",  HeaderText = "Kategori", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",      HeaderText = "Ürün Adı", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",     HeaderText = "Fiyat",    FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colActive",    HeaderText = "Aktif",    FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colImage",     HeaderText = "Görsel",   FillWeight = 34 });
            dgv.SelectionChanged += Dgv_SelectionChanged;

            // Toolbar above grid
            var gridToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = PanelBg, Padding = new Padding(6, 8, 6, 6) };
            btnNew = MakeButton("+ Yeni Ürün", SuccessGrn);
            btnNew.Click += (s, e) => StartNew();
            gridToolbar.Controls.Add(btnNew);

            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };
            leftPanel.Controls.Add(dgv);
            leftPanel.Controls.Add(gridToolbar);

            bodyPanel.Controls.Add(leftPanel);
            bodyPanel.Controls.Add(detailPanel);

            // ── Status bar ──────────────────────────────────────────
            lblStatus = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 30,
                BackColor = BgDark,
                ForeColor = TextSec,
                Font      = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0),
                Text      = "Hazır"
            };

            Controls.Add(bodyPanel);
            Controls.Add(header);
            Controls.Add(lblStatus);
        }

        private void BuildDetailPanel()
        {
            detailPanel.Controls.Clear();
            int y = 10, gap = 8;

            var lblHead = MakeLabel("Ürün Detayı", 15, FontStyle.Bold, Gold);
            lblHead.Location = new Point(0, y); lblHead.AutoSize = true;
            detailPanel.Controls.Add(lblHead); y += 36;

            // Image preview
            picPreview = new PictureBox
            {
                Size      = new Size(detailPanel.ClientSize.Width - 0, 180),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = CardBg,
                SizeMode  = PictureBoxSizeMode.Zoom,
                Location  = new Point(0, y),
                BorderStyle = BorderStyle.None
            };
            detailPanel.Controls.Add(picPreview); y += picPreview.Height + gap;

            // Image buttons row
            var imgRow = new FlowLayoutPanel
            {
                Location      = new Point(0, y),
                Size          = new Size(detailPanel.ClientSize.Width, 36),
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            btnPickImg  = MakeButton("📁 Görsel Seç",   Color.FromArgb(80, 120, 160)); btnPickImg.Width  = 140; btnPickImg.Height = 34;
            btnClearImg = MakeButton("✕ Görseli Kaldır", DangerRed);                   btnClearImg.Width = 150; btnClearImg.Height= 34;
            btnPickImg.Click  += BtnPickImg_Click;
            btnClearImg.Click += (s, e) => { pendingImagePath = null; picPreview.Image = null; picPreview.Refresh(); };
            imgRow.Controls.AddRange(new Control[] { btnPickImg, btnClearImg });
            detailPanel.Controls.Add(imgRow); y += 44;

            // Fields
            detailPanel.Controls.Add(MakeFieldLabel("Ürün Adı *",  y)); y += 22;
            txtName = MakeTextBox(y); detailPanel.Controls.Add(txtName); y += 38;

            detailPanel.Controls.Add(MakeFieldLabel("Açıklama",    y)); y += 22;
            txtDesc = new TextBox
            {
                Location    = new Point(0, y),
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline   = true,
                Height      = 64,
                Font        = new Font("Segoe UI", 10),
                BackColor   = CardBg,
                ForeColor   = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars  = ScrollBars.Vertical
            };
            detailPanel.Controls.Add(txtDesc); y += 72;

            detailPanel.Controls.Add(MakeFieldLabel("Fiyat (₺) *", y)); y += 22;
            txtPrice = MakeTextBox(y); detailPanel.Controls.Add(txtPrice); y += 38;

            detailPanel.Controls.Add(MakeFieldLabel("Kategori",    y)); y += 22;
            cmbCategory = new ComboBox
            {
                Location      = new Point(0, y),
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height        = 36,
                Font          = new Font("Segoe UI", 10),
                BackColor     = CardBg,
                ForeColor     = TextPrimary,
                FlatStyle     = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.AddRange(new object[] { "Kahve", "Pasta", "Kurabiye", "Çikolata" });
            cmbCategory.SelectedIndex = 0;
            detailPanel.Controls.Add(cmbCategory); y += 44;

            chkActive = new CheckBox
            {
                Text      = "Ürün Aktif (menüde görünsün)",
                Location  = new Point(0, y),
                Checked   = true,
                Font      = new Font("Segoe UI", 10),
                ForeColor = TextPrimary,
                AutoSize  = true
            };
            detailPanel.Controls.Add(chkActive); y += 38;

            // Save / Delete
            var btnRow = new FlowLayoutPanel
            {
                Location      = new Point(0, y),
                Size          = new Size(detailPanel.ClientSize.Width, 44),
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Margin        = Padding.Empty
            };
            btnSave = MakeButton("💾 Kaydet", SuccessGrn); btnSave.Width = 140; btnSave.Height = 42;
            btnDelete = MakeButton("🗑 Sil", DangerRed);  btnDelete.Width = 120; btnDelete.Height = 42;
            btnSave.Click   += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRow.Controls.AddRange(new Control[] { btnSave, btnDelete });
            detailPanel.Controls.Add(btnRow);

            // Resize sync for anchored controls
            detailPanel.Resize += (s, e) =>
            {
                int w = detailPanel.ClientSize.Width;
                picPreview.Width  = w;
                imgRow.Width      = w;
                txtName.Width     = w;
                txtDesc.Width     = w;
                txtPrice.Width    = w;
                cmbCategory.Width = w;
                btnRow.Width      = w;
            };

            ClearDetail();
        }

        // ══════════════════════════════════════════════════════════════
        // DATA
        // ══════════════════════════════════════════════════════════════
        private void LoadGrid(string filter = "")
        {
            try
            {
                allProducts = DatabaseHelper.GetAllProducts(false);
                FilterGrid(filter);
                SetStatus($"Toplam {allProducts.Count} ürün yüklendi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veritabanı hatası:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterGrid(string kw)
        {
            dgv.Rows.Clear();
            var list = string.IsNullOrWhiteSpace(kw)
                ? allProducts
                : allProducts.Where(p => p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                      || p.Category.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                      || p.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var p in list)
            {
                dgv.Rows.Add(
                    p.Id,
                    p.Category,
                    p.Name,
                    p.Price.ToString("N2") + " ₺",
                    p.IsActive ? "✔" : "✘",
                    p.ImagePath ?? "(yok)"
                );
                dgv.Rows[dgv.Rows.Count - 1].Tag = p;
                if (!p.IsActive)
                    dgv.Rows[dgv.Rows.Count - 1].DefaultCellStyle.ForeColor = TextSec;
            }
        }

        private void Dgv_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var row = dgv.SelectedRows[0];
            if (row.Tag is Product p)
                LoadDetail(p);
        }

        private void LoadDetail(Product p)
        {
            editing           = p;
            pendingImagePath  = p.ImagePath;
            txtName.Text      = p.Name;
            txtDesc.Text      = p.Description;
            txtPrice.Text     = p.Price.ToString("N2");
            int idx = cmbCategory.Items.IndexOf(p.Category);
            cmbCategory.SelectedIndex = idx >= 0 ? idx : 0;
            chkActive.Checked = p.IsActive;
            LoadPreview(p.ImagePath);
            btnDelete.Enabled = true;
        }

        private void LoadPreview(string? path)
        {
            var old = picPreview.Image;
            picPreview.Image = null;
            old?.Dispose();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                using var ms = new MemoryStream(File.ReadAllBytes(path));
                picPreview.Image = Image.FromStream(ms);
            }
            catch { /* ignore */ }
        }

        private void StartNew()
        {
            dgv.ClearSelection();
            ClearDetail();
            editing          = null;
            pendingImagePath = null;
            txtName.Focus();
        }

        private void ClearDetail()
        {
            editing           = null;
            pendingImagePath  = null;
            txtName.Text      = "";
            txtDesc.Text      = "";
            txtPrice.Text     = "";
            cmbCategory.SelectedIndex = 0;
            chkActive.Checked = true;
            picPreview.Image  = null;
            btnDelete.Enabled = false;
        }

        // ── Image picker ───────────────────────────────────────────
        private void BtnPickImg_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Ürün Görseli Seç",
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tüm Dosyalar|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Copy image to app images folder
            string imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            Directory.CreateDirectory(imgDir);
            string ext     = Path.GetExtension(dlg.FileName);
            string newName = $"product_{DateTime.Now:yyyyMMddHHmmssff}{ext}";
            string dest    = Path.Combine(imgDir, newName);
            File.Copy(dlg.FileName, dest, overwrite: true);
            pendingImagePath = dest;
            LoadPreview(dest);
        }

        // ── Save ───────────────────────────────────────────────────
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput(out decimal price)) return;

            var p = editing ?? new Product();
            p.Name        = txtName.Text.Trim();
            p.Description = txtDesc.Text.Trim();
            p.Price       = price;
            p.Category    = cmbCategory.SelectedItem?.ToString() ?? "Kahve";
            p.ImagePath   = pendingImagePath;
            p.IsActive    = chkActive.Checked;

            try
            {
                if (editing == null)
                {
                    p.Id = DatabaseHelper.AddProduct(p);
                    SetStatus($"✔ '{p.Name}' ürünü eklendi. (ID: {p.Id})");
                }
                else
                {
                    DatabaseHelper.UpdateProduct(p);
                    SetStatus($"✔ '{p.Name}' güncellendi.");
                }
                LoadGrid(txtSearch.Text);
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Delete ─────────────────────────────────────────────────
        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (editing == null) return;
            var res = MessageBox.Show($"'{editing.Name}' ürünü silinecek. Emin misiniz?",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;
            try
            {
                DatabaseHelper.DeleteProduct(editing.Id);
                SetStatus($"🗑 '{editing.Name}' silindi.");
                LoadGrid(txtSearch.Text);
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silme hatası:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════
        private bool ValidateInput(out decimal price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(txtName.Text)) { ShowError("Ürün adı boş olamaz."); return false; }
            string priceStr = txtPrice.Text.Replace(",", ".").Trim();
            if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out price) || price <= 0)
            {
                ShowError("Geçerli bir fiyat giriniz."); return false;
            }
            return true;
        }

        private void ShowError(string msg) =>
            MessageBox.Show(msg, "Doğrulama Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void SetStatus(string msg) => lblStatus.Text = "  " + msg;

        private Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label { Text = text, Font = new Font("Segoe UI", size, style), ForeColor = color, AutoSize = true };
        }

        private Label MakeFieldLabel(string text, int y)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(0, y),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextSec,
                AutoSize  = true
            };
        }

        private TextBox MakeTextBox(int y)
        {
            return new TextBox
            {
                Location    = new Point(0, y),
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height      = 32,
                Font        = new Font("Segoe UI", 10),
                BackColor   = CardBg,
                ForeColor   = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private Button MakeButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text      = text,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Height    = 36,
                Margin    = new Padding(0, 0, 8, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}
