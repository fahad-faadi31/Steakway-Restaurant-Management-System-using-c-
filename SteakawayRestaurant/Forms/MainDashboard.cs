using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;
using SteakawayRestaurant.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SteakawayRestaurant.Forms
{
    public class MainDashboard : Form
    {
        // Layout
        private Panel pnlSidebar, pnlContent;
        // Pages
        private Panel pgMenu, pgCart, pgOrders, pgProfile;
        // Menu page
        private FlowLayoutPanel flMenu;
        private TextBox txtSearch;
        private ComboBox cmbCategory;
        // Cart
        private DataGridView dgvCart;
        private Label lblCartTotal, lblCartEmpty;
        private ComboBox cmbOrderType, cmbPayment;
        private TextBox txtTable, txtDeliveryAddr, txtCartNotes;
        // Orders
        private DataGridView dgvOrders;
        private DataGridView dgvOrderItems;
        private Button btnRateOrder;
        // Profile
        private TextBox txtPName, txtPPhone, txtPEmail, txtPAddr;
        private DataGridView dgvAddresses;
        // Cart badge
        private Label lblCartBadge;
        // Cart items list
        private List<CartItem> cartItems = new List<CartItem>();

        private class CartItem
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public string Note { get; set; }
        }

        private Button _activeNav;

        public MainDashboard()
        {
            this.Text = $"Steakaway — Customer Portal ({SessionManager.FullName})";
            this.Size = new Size(1200, 740);
            this.MinimumSize = new Size(1000, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);

            BuildMenuPage();
            BuildCartPage();
            BuildOrdersPage();
            BuildProfilePage();
            BuildShell();

            ShowPage(pgMenu);
            LoadMenu();
        }

        private void BuildShell()
        {
            pnlSidebar = new Panel { Location = new Point(0, 0), Size = new Size(220, 740), BackColor = Color.FromArgb(12, 12, 18), Dock = DockStyle.Left };

            var logo = new Label
            {
                Text = "🥩 STEAKAWAY",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = false,
                Size = new Size(220, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Location = new Point(0, 0)
            };

            var user = new Label
            {
                Text = $"👤  {SessionManager.FullName}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(160, 160, 180),
                AutoSize = false,
                Size = new Size(200, 24),
                Location = new Point(10, 64),
                BackColor = Color.Transparent
            };
            var div = new Panel { Location = new Point(0, 95), Size = new Size(220, 1), BackColor = Color.FromArgb(40, 40, 60) };

            int ny = 108;

            var btnMenu = NavBtn("🍽  Menu", ny);
            btnMenu.Click += (s, e) => { ShowPage(pgMenu); LoadMenu(); SetActiveNav((Button)s); };
            pnlSidebar.Controls.Add(btnMenu);
            ny += 46;

            var btnCart = NavBtn("🛒  My Cart", ny);
            btnCart.Click += (s, e) => { ShowPage(pgCart); LoadCartFromDatabase(); SetActiveNav((Button)s); };
            pnlSidebar.Controls.Add(btnCart);

            lblCartBadge = new Label
            {
                Size = new Size(20, 20),
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(165, ny + 8),
                Text = "0",
                Visible = false
            };
            pnlSidebar.Controls.Add(lblCartBadge);
            ny += 46;

            var btnOrders = NavBtn("📋  My Orders", ny);
            btnOrders.Click += (s, e) => { ShowPage(pgOrders); LoadOrders(); SetActiveNav((Button)s); };
            pnlSidebar.Controls.Add(btnOrders);
            ny += 46;

            var btnProfile = NavBtn("👤  Profile", ny);
            btnProfile.Click += (s, e) => { ShowPage(pgProfile); LoadProfile(); SetActiveNav((Button)s); };
            pnlSidebar.Controls.Add(btnProfile);
            ny += 46;

            var sep2 = new Panel { Location = new Point(0, 740 - 60), Size = new Size(220, 1), BackColor = Color.FromArgb(40, 40, 60), Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right) };
            var btnLogout = NavBtn("⏻  Logout", 740 - 52);
            btnLogout.ForeColor = Color.FromArgb(230, 70, 70);
            btnLogout.Click += (s, e) => this.Close();
            btnLogout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            pnlSidebar.Controls.AddRange(new Control[] { logo, user, div, sep2, btnLogout });

            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 24), Padding = new Padding(20) };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            UpdateCartBadge();
        }

        private Button NavBtn(string text, int y)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(220, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(160, 160, 180),
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => { if (b != _activeNav) b.BackColor = Color.FromArgb(28, 28, 40); };
            b.MouseLeave += (s, e) => { if (b != _activeNav) b.BackColor = Color.Transparent; };
            return b;
        }

        private void SetActiveNav(Button b)
        {
            if (_activeNav != null)
            {
                _activeNav.BackColor = Color.Transparent;
                _activeNav.ForeColor = Color.FromArgb(160, 160, 180);
            }
            _activeNav = b;
            b.BackColor = Color.FromArgb(38, 38, 52);
            b.ForeColor = Color.FromArgb(255, 140, 30);
        }

        private void ShowPage(Panel pg)
        {
            if (pg == null) return;

            if (pgMenu != null) pgMenu.Hide();
            if (pgCart != null) pgCart.Hide();
            if (pgOrders != null) pgOrders.Hide();
            if (pgProfile != null) pgProfile.Hide();

            if (!pnlContent.Controls.Contains(pg))
            {
                pg.Dock = DockStyle.Fill;
                pnlContent.Controls.Add(pg);
            }
            pg.Show();
            pg.BringToFront();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MENU PAGE - FIXED FOR NO OVERFLOW
        // ══════════════════════════════════════════════════════════════════════
        private void BuildMenuPage()
        {
            pgMenu = new Panel { BackColor = Color.FromArgb(18, 18, 24), Visible = false };
            pgMenu.AutoScroll = true;  // Enable scrolling

            var lbl = H1("Browse Menu");
            lbl.Dock = DockStyle.Top;
            lbl.Padding = new Padding(0, 0, 0, 5);

            // Search + filter bar
            var pnlBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent
            };

            txtSearch = new TextBox
            {
                Location = new Point(0, 8),
                Size = new Size(280, 30),
                BackColor = Color.FromArgb(28, 28, 38),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadMenu();

            var lblSrch = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(286, 9)
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(320, 8),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(28, 28, 38),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbCategory.Items.Add("All Categories");

            using (var categories = DB.Query("SELECT Name FROM Categories ORDER BY Name"))
            {
                foreach (DataRow r in categories.Rows)
                    cmbCategory.Items.Add(r["Name"].ToString());
            }

            cmbCategory.SelectedIndex = 0;
            cmbCategory.SelectedIndexChanged += (s, e) => LoadMenu();

            pnlBar.Controls.AddRange(new Control[] { txtSearch, lblSrch, cmbCategory });

            // Menu cards flow panel
            flMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(5)
            };

            pgMenu.Controls.Add(lbl);
            pgMenu.Controls.Add(pnlBar);
            pgMenu.Controls.Add(flMenu);
        }

        private void LoadMenu()
        {
            if (flMenu == null) return;

            flMenu.SuspendLayout();
            flMenu.Controls.Clear();
            string search = txtSearch.Text.Trim().ToLower();
            string cat = cmbCategory.SelectedIndex > 0 ? cmbCategory.SelectedItem.ToString() : null;

            string sql = @"SELECT m.ItemId,m.Name,c.Name AS Category,m.Price,m.Description
                           FROM MenuItems m JOIN Categories c ON m.CategoryId=c.CategoryId
                           WHERE m.IsAvailable=1";
            if (cat != null) sql += " AND c.Name=@cat";
            var parms = cat != null
                ? new[] { DB.P("@cat", cat) }
                : Array.Empty<System.Data.SQLite.SQLiteParameter>();

            using (var queryResult = DB.Query(sql, parms))
            {
                foreach (DataRow row in queryResult.Rows)
                {
                    string name = row["Name"].ToString();
                    if (!string.IsNullOrEmpty(search) && !name.ToLower().Contains(search)
                        && !row["Category"].ToString().ToLower().Contains(search)
                        && !row["Description"].ToString().ToLower().Contains(search)) continue;

                    int itemId = Convert.ToInt32(row["ItemId"]);
                    string catName = row["Category"].ToString();
                    decimal price = Convert.ToDecimal(row["Price"]);
                    string desc = row["Description"].ToString();

                    // SMALLER CARD TO PREVENT OVERFLOW
                    var card = new Panel
                    {
                        Size = new Size(195, 155),  // Reduced from 210x165
                        BackColor = Color.FromArgb(28, 28, 38),
                        Margin = new Padding(5, 5, 5, 5)  // Consistent margins
                    };

                    card.Paint += (s, e) =>
                    {
                        using (var p = new Pen(Color.FromArgb(50, 50, 68), 1))
                            e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                    };

                    // Category badge
                    var badge = new Label
                    {
                        Text = catName,
                        Font = new Font("Segoe UI", 7, FontStyle.Bold),
                        ForeColor = Color.FromArgb(255, 140, 30),
                        BackColor = Color.FromArgb(50, 40, 20),
                        AutoSize = true,
                        Location = new Point(5, 5),
                        Padding = new Padding(4, 2, 4, 2)
                    };

                    // Item name - truncated if too long
                    var lblN = new Label
                    {
                        Text = name.Length > 20 ? name.Substring(0, 17) + "..." : name,
                        Font = new Font("Segoe UI", 9, FontStyle.Bold),  // Reduced font size
                        ForeColor = Color.FromArgb(240, 240, 248),
                        AutoSize = false,
                        Size = new Size(180, 32),  // Adjusted size
                        Location = new Point(5, 28),
                        BackColor = Color.Transparent
                    };

                    // Description - truncated
                    var lblD = new Label
                    {
                        Text = desc.Length > 45 ? desc.Substring(0, 42) + "..." : desc,
                        Font = new Font("Segoe UI", 7),  // Reduced font size
                        ForeColor = Color.FromArgb(110, 110, 130),
                        AutoSize = false,
                        Size = new Size(180, 28),
                        Location = new Point(5, 62),
                        BackColor = Color.Transparent
                    };

                    // Price
                    var lblP = new Label
                    {
                        Text = $"Rs {price:F0}",
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),  // Reduced font size
                        ForeColor = Color.FromArgb(255, 140, 30),
                        AutoSize = true,
                        Location = new Point(5, 95),
                        BackColor = Color.Transparent
                    };

                    // Add button - smaller
                    var btnAdd = new Button
                    {
                        Text = "Add",
                        Location = new Point(120, 93),
                        Size = new Size(65, 28),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(255, 140, 30),
                        ForeColor = Color.FromArgb(18, 18, 24),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    btnAdd.FlatAppearance.BorderSize = 0;

                    var localId = itemId;
                    var localName = name;
                    var localPrice = price;
                    btnAdd.Click += (s, e) => AddToCart(localId, localName, localPrice);

                    card.Controls.AddRange(new Control[] { badge, lblN, lblD, lblP, btnAdd });
                    flMenu.Controls.Add(card);
                }
            }
            flMenu.ResumeLayout();
        }

        private void AddToCart(int itemId, string name, decimal price)
        {
            var existing = cartItems.FirstOrDefault(x => x.ItemId == itemId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cartItems.Add(new CartItem { ItemId = itemId, Name = name, Quantity = 1, Price = price, Note = "" });
            }

            // Save to database
            var cnt = DB.Scalar("SELECT COUNT(*) FROM Cart WHERE CustomerId=@c AND ItemId=@i",
                DB.P("@c", SessionManager.UserId), DB.P("@i", itemId));
            if (Convert.ToInt64(cnt) > 0)
            {
                DB.NonQuery("UPDATE Cart SET Quantity=Quantity+1 WHERE CustomerId=@c AND ItemId=@i",
                    DB.P("@c", SessionManager.UserId), DB.P("@i", itemId));
            }
            else
            {
                DB.NonQuery("INSERT INTO Cart(CustomerId,ItemId,ItemName,Quantity,UnitPrice) VALUES(@c,@i,@n,1,@p)",
                    DB.P("@c", SessionManager.UserId), DB.P("@i", itemId),
                    DB.P("@n", name), DB.P("@p", (double)price));
            }

            UpdateCartBadge();
            ShowToast($"'{name}' added to cart!");
        }

        private void UpdateCartBadge()
        {
            if (lblCartBadge == null) return;
            var cnt = DB.Scalar("SELECT COALESCE(SUM(Quantity),0) FROM Cart WHERE CustomerId=@c", DB.P("@c", SessionManager.UserId));
            int n = Convert.ToInt32(cnt);
            lblCartBadge.Text = n.ToString();
            lblCartBadge.Visible = n > 0;
        }

        private void ShowToast(string msg)
        {
            var toast = new Label
            {
                Text = msg,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 248),
                BackColor = Color.FromArgb(50, 180, 100),
                AutoSize = false,
                Size = new Size(280, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(pnlContent.Width - 300, pnlContent.Height - 50)
            };
            pnlContent.Controls.Add(toast);
            toast.BringToFront();
            var t = new Timer { Interval = 2000 };
            t.Tick += (s, e) => { t.Stop(); pnlContent.Controls.Remove(toast); toast.Dispose(); };
            t.Start();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CART PAGE (Keep your existing implementation)
        // ══════════════════════════════════════════════════════════════════════
        private void BuildCartPage()
        {
            pgCart = new Panel { BackColor = Color.FromArgb(18, 18, 24), Visible = false };

            var lbl = H1("My Cart");
            lbl.Location = new Point(0, 0);

            dgvCart = StyledGrid(new Point(0, 44), new Size(650, 320));
            dgvCart.CellValueChanged += DgvCart_CellChanged;
            dgvCart.CurrentCellDirtyStateChanged += (s, e) => { if (dgvCart.IsCurrentCellDirty) dgvCart.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            var btnRemove = DarkBtn("✕ Remove Item", new Point(0, 374), Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnRemove.Click += (s, e) => RemoveCartItem();

            var btnClear = DarkBtn("🗑 Clear Cart", new Point(150, 374), Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180));
            btnClear.Click += (s, e) => { DB.NonQuery("DELETE FROM Cart WHERE CustomerId=@c", DB.P("@c", SessionManager.UserId)); LoadCartFromDatabase(); };

            lblCartTotal = new Label
            {
                Text = "Total: Rs 0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(460, 374)
            };

            lblCartEmpty = new Label
            {
                Text = "🛒  Your cart is empty.\nBrowse the menu to add items.",
                Font = new Font("Segoe UI", 13),
                ForeColor = Color.FromArgb(80, 80, 100),
                AutoSize = false,
                Size = new Size(650, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Location = new Point(0, 130),
                Visible = false
            };

            var optCard = new Panel { Location = new Point(0, 415), Size = new Size(650, 230), BackColor = Color.FromArgb(28, 28, 38) };
            optCard.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(50, 50, 68), 1)) e.Graphics.DrawRectangle(p, 0, 0, optCard.Width - 1, optCard.Height - 1); };

            var lblOpts = new Label
            {
                Text = "Order Options",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(14, 10)
            };

            SmallLbl(optCard, "Order Type:", new Point(14, 42));
            cmbOrderType = new ComboBox
            {
                Location = new Point(110, 38),
                Size = new Size(140, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbOrderType.Items.AddRange(new[] { "DineIn", "Online" });
            cmbOrderType.SelectedIndex = 0;
            cmbOrderType.SelectedIndexChanged += (s, e) => {
                bool online = cmbOrderType.SelectedItem?.ToString() == "Online";
                txtTable.Visible = !online;
                txtDeliveryAddr.Visible = online;
            };

            SmallLbl(optCard, "Payment:", new Point(14, 80));
            cmbPayment = new ComboBox
            {
                Location = new Point(110, 76),
                Size = new Size(160, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPayment.Items.AddRange(new[] { "Cash on Delivery", "Card" });
            cmbPayment.SelectedIndex = 0;

            SmallLbl(optCard, "Table No:", new Point(14, 116));
            txtTable = new TextBox
            {
                Location = new Point(110, 112),
                Size = new Size(80, 26),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            SmallLbl(optCard, "Address:", new Point(14, 116));
            txtDeliveryAddr = new TextBox
            {
                Location = new Point(110, 112),
                Size = new Size(250, 26),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            LoadDefaultAddress();

            SmallLbl(optCard, "Note:", new Point(14, 152));
            txtCartNotes = new TextBox
            {
                Location = new Point(110, 148),
                Size = new Size(350, 26),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnCheckout = new Button
            {
                Text = "PLACE ORDER  →",
                Location = new Point(14, 185),
                Size = new Size(240, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += BtnCheckout_Click;

            optCard.Controls.AddRange(new Control[]{ lblOpts, cmbOrderType, cmbPayment,
                txtTable, txtDeliveryAddr, txtCartNotes, btnCheckout });

            pgCart.Controls.AddRange(new Control[]{ lbl, dgvCart, btnRemove, btnClear,
                lblCartTotal, lblCartEmpty, optCard });
        }

        private void LoadDefaultAddress()
        {
            using (var dt = DB.Query("SELECT Address FROM CustomerAddresses WHERE CustomerId=@c AND IsDefault=1 LIMIT 1", DB.P("@c", SessionManager.UserId)))
            {
                if (dt.Rows.Count > 0) txtDeliveryAddr.Text = dt.Rows[0]["Address"].ToString();
            }
        }

        private void LoadCartFromDatabase()
        {
            UpdateCartBadge();
            using (var cartData = DB.Query(@"SELECT CartId,ItemName AS Item,Quantity,UnitPrice AS [Price Rs],
                                       ROUND(Quantity*UnitPrice,0) AS [Subtotal Rs]
                                FROM Cart WHERE CustomerId=@c",
                DB.P("@c", SessionManager.UserId)))
            {
                dgvCart.DataSource = cartData;
                if (dgvCart.Columns.Contains("Quantity"))
                    dgvCart.Columns["Quantity"].ReadOnly = false;

                decimal total = 0;
                foreach (DataRow r in cartData.Rows) total += Convert.ToDecimal(r["Subtotal Rs"]);
                lblCartTotal.Text = $"Total: Rs {total:F0}";
                lblCartEmpty.Visible = cartData.Rows.Count == 0;
                dgvCart.Visible = cartData.Rows.Count > 0;
            }
        }

        private void DgvCart_CellChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (dgvCart.Columns[e.ColumnIndex].Name != "Quantity") return;
            var row = (DataRowView)dgvCart.Rows[e.RowIndex].DataBoundItem;
            if (row == null) return;
            int cartId = Convert.ToInt32(row["CartId"]);
            int.TryParse(dgvCart.Rows[e.RowIndex].Cells["Quantity"].Value?.ToString(), out int qty);
            if (qty < 1) qty = 1;
            DB.NonQuery("UPDATE Cart SET Quantity=@q WHERE CartId=@id",
                DB.P("@q", qty), DB.P("@id", cartId));
            LoadCartFromDatabase();
        }

        private void RemoveCartItem()
        {
            if (dgvCart.CurrentRow == null) return;
            var row = (DataRowView)dgvCart.CurrentRow.DataBoundItem;
            int cartId = Convert.ToInt32(row["CartId"]);
            DB.NonQuery("DELETE FROM Cart WHERE CartId=@id", DB.P("@id", cartId));
            LoadCartFromDatabase();
        }

        private void BtnCheckout_Click(object s, EventArgs e)
        {
            using (var cartData = DB.Query("SELECT * FROM Cart WHERE CustomerId=@c", DB.P("@c", SessionManager.UserId)))
            {
                if (cartData.Rows.Count == 0) { MessageBox.Show("Cart is empty!", "Info"); return; }

                string type = cmbOrderType.SelectedItem.ToString();
                string payment = cmbPayment.SelectedItem.ToString();
                string table = txtTable.Text.Trim();
                string addr = txtDeliveryAddr.Text.Trim();

                if (type == "DineIn" && string.IsNullOrWhiteSpace(table))
                { MessageBox.Show("Please enter table number for dine-in.", "Validation"); return; }
                if (type == "Online" && string.IsNullOrWhiteSpace(addr))
                { MessageBox.Show("Please enter delivery address.", "Validation"); return; }

                using (var userData = DB.Query("SELECT FullName,Phone FROM Users WHERE UserId=@u", DB.P("@u", SessionManager.UserId)))
                {
                    long orderId = DB.Insert(
                        @"INSERT INTO Orders(CustomerId,CustomerName,Phone,OrderType,TableNumber,Address,
                          Status,PaymentMethod,SpecialNotes)
                          VALUES(@cid,@cn,@ph,@ot,@tn,@addr,'Pending',@pm,@sn)",
                        DB.P("@cid", SessionManager.UserId),
                        DB.P("@cn", userData.Rows[0]["FullName"].ToString()),
                        DB.P("@ph", userData.Rows[0]["Phone"].ToString()),
                        DB.P("@ot", type),
                        DB.P("@tn", table),
                        DB.P("@addr", addr),
                        DB.P("@pm", payment),
                        DB.P("@sn", txtCartNotes.Text.Trim()));

                    foreach (DataRow item in cartData.Rows)
                        DB.NonQuery(@"INSERT INTO OrderItems(OrderId,ItemId,ItemName,Quantity,UnitPrice)
                                      VALUES(@oid,@iid,@iname,@qty,@up)",
                            DB.P("@oid", (int)orderId),
                            DB.P("@iid", Convert.ToInt32(item["ItemId"])),
                            DB.P("@iname", item["ItemName"].ToString()),
                            DB.P("@qty", Convert.ToInt32(item["Quantity"])),
                            DB.P("@up", Convert.ToDouble(item["UnitPrice"])));

                    DB.NonQuery("UPDATE Orders SET TotalAmount=(SELECT COALESCE(SUM(Quantity*UnitPrice),0) FROM OrderItems WHERE OrderId=@id),FinalAmount=(SELECT COALESCE(SUM(Quantity*UnitPrice),0) FROM OrderItems WHERE OrderId=@id) WHERE OrderId=@id",
                        DB.P("@id", (int)orderId));
                }

                DB.NonQuery("DELETE FROM Cart WHERE CustomerId=@c", DB.P("@c", SessionManager.UserId));

                MessageBox.Show($"✅  Order placed successfully!", "Order Placed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCartFromDatabase();
                ShowPage(pgOrders);
                LoadOrders();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ORDERS PAGE (Keep your existing implementation)
        // ══════════════════════════════════════════════════════════════════════
        private void BuildOrdersPage()
        {
            pgOrders = new Panel { BackColor = Color.FromArgb(18, 18, 24), Visible = false };
            var lbl = H1("My Orders"); lbl.Location = new Point(0, 0);

            var btnRefresh = DarkBtn("↻ Refresh", new Point(0, 40), Color.FromArgb(28, 28, 38), Color.FromArgb(160, 160, 180));
            btnRefresh.Click += (s, e) => LoadOrders();

            dgvOrders = StyledGrid(new Point(0, 82), new Size(580, 260));
            dgvOrders.SelectionChanged += (s, e) => LoadOrderItems();

            var lblItems = new Label
            {
                Text = "Order Items:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, 352)
            };

            dgvOrderItems = StyledGrid(new Point(0, 376), new Size(580, 160));

            btnRateOrder = DarkBtn("⭐ Rate Order", new Point(0, 548), Color.FromArgb(50, 40, 10), Color.FromArgb(255, 190, 40));
            btnRateOrder.Click += BtnRate_Click;

            pgOrders.Controls.AddRange(new Control[] { lbl, btnRefresh, dgvOrders, lblItems, dgvOrderItems, btnRateOrder });
        }

        private void LoadOrders()
        {
            using (var ordersData = DB.Query(
                @"SELECT OrderId AS [#],OrderType AS Type,TableNumber AS [Table#],
                         Status,TotalAmount AS [Total Rs],PaymentMethod AS Payment,
                         Rating,datetime(CreatedAt,'localtime') AS Date
                  FROM Orders WHERE CustomerId=@c ORDER BY CreatedAt DESC",
                DB.P("@c", SessionManager.UserId)))
            {
                dgvOrders.DataSource = ordersData;
            }
        }

        private void LoadOrderItems()
        {
            if (dgvOrders.CurrentRow == null) return;
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            using (var itemsData = DB.Query(
                "SELECT ItemName AS Item,Quantity AS Qty,UnitPrice AS [Price Rs],ROUND(Quantity*UnitPrice,0) AS [Subtotal Rs],Status FROM OrderItems WHERE OrderId=@id",
                DB.P("@id", oid)))
            {
                dgvOrderItems.DataSource = itemsData;
            }
        }

        private void BtnRate_Click(object s, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            string status = dgvOrders.CurrentRow.Cells["Status"].Value.ToString();
            if (status != "Delivered" && status != "Closed" && status != "ReadyForDelivery")
            { MessageBox.Show("You can only rate completed/delivered orders.", "Info"); return; }

            using (var d = new RatingDialog())
            {
                if (d.ShowDialog() == DialogResult.OK)
                {
                    DB.NonQuery("UPDATE Orders SET Rating=@r WHERE OrderId=@id",
                        DB.P("@r", d.Rating), DB.P("@id", oid));
                    LoadOrders();
                    MessageBox.Show("Thank you for your rating!", "Rated");
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PROFILE PAGE (Keep your existing implementation)
        // ══════════════════════════════════════════════════════════════════════
        private void BuildProfilePage()
        {
            pgProfile = new Panel { BackColor = Color.FromArgb(18, 18, 24), Visible = false };
            var lbl = H1("My Profile"); lbl.Location = new Point(0, 0);

            var profCard = new Panel { Location = new Point(0, 44), Size = new Size(500, 290), BackColor = Color.FromArgb(28, 28, 38) };
            profCard.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(50, 50, 68))) e.Graphics.DrawRectangle(p, 0, 0, profCard.Width - 1, profCard.Height - 1); };

            var lblPTitle = new Label
            {
                Text = "Personal Information",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 12)
            };

            int py = 44;
            SmallLbl(profCard, "Full Name", new Point(16, py)); txtPName = PInput(profCard, new Point(130, py - 4), 250); py += 44;
            SmallLbl(profCard, "Phone", new Point(16, py)); txtPPhone = PInput(profCard, new Point(130, py - 4), 250); py += 44;
            SmallLbl(profCard, "Email", new Point(16, py)); txtPEmail = PInput(profCard, new Point(130, py - 4), 250); py += 44;
            SmallLbl(profCard, "Default Address", new Point(16, py)); txtPAddr = PInput(profCard, new Point(130, py - 4), 250); py += 54;

            var btnSaveProf = new Button
            {
                Text = "Save Profile",
                Location = new Point(130, py - 10),
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveProf.FlatAppearance.BorderSize = 0;
            btnSaveProf.Click += BtnSaveProfile_Click;

            profCard.Controls.AddRange(new Control[] { lblPTitle, btnSaveProf });

            var addrCard = new Panel { Location = new Point(0, 345), Size = new Size(700, 290), BackColor = Color.FromArgb(28, 28, 38) };
            addrCard.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(50, 50, 68))) e.Graphics.DrawRectangle(p, 0, 0, addrCard.Width - 1, addrCard.Height - 1); };

            var lblAddrTitle = new Label
            {
                Text = "Delivery Addresses",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 12)
            };

            dgvAddresses = StyledGrid(new Point(0, 40), new Size(700, 150));
            dgvAddresses.Size = new Size(698, 148);

            var btnAddAddr = DarkBtn("＋ Add Address", new Point(16, 198), Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            var btnDelAddr = DarkBtn("✕ Remove", new Point(170, 198), Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            var btnSetDef = DarkBtn("★ Set Default", new Point(290, 198), Color.FromArgb(28, 28, 38), Color.FromArgb(160, 160, 180));

            btnAddAddr.Click += (s, e) =>
            {
                using (var d = new InputDialog("Add Address", "Enter address:"))
                {
                    if (d.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(d.Value))
                    {
                        DB.NonQuery("INSERT INTO CustomerAddresses(CustomerId,Label,Address,IsDefault) VALUES(@c,'Home',@a,0)",
                            DB.P("@c", SessionManager.UserId), DB.P("@a", d.Value));
                        LoadAddresses();
                    }
                }
            };
            btnDelAddr.Click += (s, e) =>
            {
                if (dgvAddresses.CurrentRow == null) return;
                int aid = Convert.ToInt32(((DataRowView)dgvAddresses.CurrentRow.DataBoundItem)["AddressId"]);
                DB.NonQuery("DELETE FROM CustomerAddresses WHERE AddressId=@id", DB.P("@id", aid));
                LoadAddresses();
            };
            btnSetDef.Click += (s, e) =>
            {
                if (dgvAddresses.CurrentRow == null) return;
                int aid = Convert.ToInt32(((DataRowView)dgvAddresses.CurrentRow.DataBoundItem)["AddressId"]);
                DB.NonQuery("UPDATE CustomerAddresses SET IsDefault=0 WHERE CustomerId=@c", DB.P("@c", SessionManager.UserId));
                DB.NonQuery("UPDATE CustomerAddresses SET IsDefault=1 WHERE AddressId=@id", DB.P("@id", aid));
                LoadAddresses();
            };

            addrCard.Controls.AddRange(new Control[] { lblAddrTitle, dgvAddresses, btnAddAddr, btnDelAddr, btnSetDef });
            pgProfile.Controls.AddRange(new Control[] { lbl, profCard, addrCard });
        }

        private void LoadProfile()
        {
            using (var dt = DB.Query("SELECT FullName,Phone,Email,Address FROM Users WHERE UserId=@u", DB.P("@u", SessionManager.UserId)))
            {
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                txtPName.Text = r["FullName"].ToString();
                txtPPhone.Text = r["Phone"].ToString();
                txtPEmail.Text = r["Email"].ToString();
                txtPAddr.Text = r["Address"].ToString();
            }
            LoadAddresses();
        }

        private void LoadAddresses()
        {
            using (var addressesData = DB.Query(
                "SELECT AddressId,Label,Address,IsDefault AS [Default] FROM CustomerAddresses WHERE CustomerId=@c",
                DB.P("@c", SessionManager.UserId)))
            {
                dgvAddresses.DataSource = addressesData;
            }
        }

        private void BtnSaveProfile_Click(object s, EventArgs e)
        {
            DB.NonQuery("UPDATE Users SET FullName=@fn,Phone=@ph,Email=@em,Address=@addr WHERE UserId=@u",
                DB.P("@fn", txtPName.Text.Trim()), DB.P("@ph", txtPPhone.Text.Trim()),
                DB.P("@em", txtPEmail.Text.Trim()), DB.P("@addr", txtPAddr.Text.Trim()),
                DB.P("@u", SessionManager.UserId));
            MessageBox.Show("Profile updated!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Shared helpers ────────────────────────────────────────────────────
        private static Label H1(string t) => new Label
        { Text = t, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(240, 240, 248), AutoSize = true, BackColor = Color.Transparent };

        private static DataGridView StyledGrid(Point loc, Size sz)
        {
            var g = new DataGridView
            {
                Location = loc,
                Size = sz,
                BackgroundColor = Color.FromArgb(22, 22, 32),
                GridColor = Color.FromArgb(40, 40, 56),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            g.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            g.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 232);
            g.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 50, 20);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 140, 30);
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(12, 12, 18);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 140, 30);
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.RowTemplate.Height = 32;
            return g;
        }

        private static Button DarkBtn(string t, Point p, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = t,
                Location = p,
                Size = new Size(138, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = fg;
            return b;
        }

        private static void SmallLbl(Control parent, string t, Point p) =>
            parent.Controls.Add(new Label
            {
                Text = t,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(p.X, p.Y + 6)
            });

        private static TextBox PInput(Control parent, Point p, int w)
        {
            var tb = new TextBox
            {
                Location = p,
                Size = new Size(w, 26),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(tb);
            return tb;
        }
    }

    // Rating Dialog
    public class RatingDialog : Form
    {
        public int Rating { get; private set; } = 5;
        public RatingDialog()
        {
            this.Text = "Rate Your Order";
            this.Size = new Size(320, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(28, 28, 38);

            var lbl = new Label
            {
                Text = "Rate your experience (1-5 stars):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 220, 232),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(20, 20)
            };
            var nud = new NumericUpDown
            {
                Location = new Point(20, 52),
                Size = new Size(80, 30),
                Minimum = 1,
                Maximum = 5,
                Value = 5,
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 14)
            };
            var lbl2 = new Label { Text = "⭐", AutoSize = true, Font = new Font("Segoe UI", 16), Location = new Point(110, 50), BackColor = Color.Transparent };

            var btnOk = new Button
            {
                Text = "Submit Rating",
                Location = new Point(20, 100),
                Size = new Size(200, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { Rating = (int)nud.Value; this.DialogResult = DialogResult.OK; this.Close(); };

            this.Controls.AddRange(new Control[] { lbl, nud, lbl2, btnOk });
        }
    }

    // Input Dialog
    public class InputDialog : Form
    {
        public string Value { get; private set; }
        private TextBox tb;
        public InputDialog(string title, string prompt)
        {
            this.Text = title;
            this.Size = new Size(400, 170);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(28, 28, 38);
            var lbl = new Label
            {
                Text = prompt,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 220, 232),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(20, 20)
            };
            tb = new TextBox
            {
                Location = new Point(20, 46),
                Size = new Size(340, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(20, 88),
                Size = new Size(120, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { Value = tb.Text; this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.AddRange(new Control[] { lbl, tb, btnOk });
            this.AcceptButton = btnOk;
        }
    }
}