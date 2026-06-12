using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;

namespace SteakawayRestaurant.Forms
{
    public class OrderForm : Form
    {
        private TabControl tabs;
        // Active orders tab
        private DataGridView dgvOrders, dgvOrderItems;
        private Label lblOrderDetail;
        // New order tab
        private DataGridView dgvMenu, dgvCart;
        private TextBox txtCustName, txtTableNum, txtOrderNotes, txtItemInstr, txtSearchMenu;
        private ComboBox cmbOrderType, cmbMenuCat;
        private NumericUpDown nudQty;
        private Label lblCartTotal;
        private List<CartRow> _cart = new List<CartRow>();
        private int? _editingOrderId = null;

        private class CartRow
        {
            public int ItemId; public string Name; public decimal Price; public int Qty; public string Note;
        }

        public OrderForm()
        {
            this.Text = $"Waiter Panel — {SessionManager.FullName}";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(1000, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
            LoadActiveOrders();
        }

        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(24, 24, 36) };
            hdr.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, hdr.Width, 4);
            hdr.Controls.Add(Lbl($"🥩  Steakaway  |  Waiter Panel  |  {SessionManager.FullName}",
                new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, 16)));
            var btnLogout = MkBtn("Logout", new Point(hdr.Width - 120, 10), 100, 32, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += (s, e) => this.Close();
            hdr.Controls.Add(btnLogout);

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(180, 38);
            tabs.DrawItem += (s, e) => {
                var tb = tabs.TabPages[e.Index]; var bnd = tabs.GetTabRect(e.Index);
                bool sel = e.Index == tabs.SelectedIndex;
                e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(28, 28, 38) : Color.FromArgb(18, 18, 24)), bnd);
                if (sel) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), bnd.Left, bnd.Top, bnd.Width, 3);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(tb.Text, new Font("Segoe UI", 10, sel ? FontStyle.Bold : FontStyle.Regular),
                    new SolidBrush(sel ? Color.FromArgb(255, 140, 30) : Color.FromArgb(130, 130, 150)), bnd, sf);
            };

            tabs.TabPages.Add(BuildActiveOrdersTab());
            tabs.TabPages.Add(BuildNewOrderTab());

            this.Controls.Add(tabs);
            this.Controls.Add(hdr);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVE ORDERS TAB
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildActiveOrdersTab()
        {
            var tab = DarkTab("📋 Active Orders");

            var btnRef = MkBtn("↻ Refresh", new Point(16, 16), 120, 32, Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180));
            btnRef.Click += (s, e) => LoadActiveOrders();

            var btnCancel = MkBtn("✕ Cancel Order", new Point(148, 16), 150, 32, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnCancel.Click += BtnCancel_Click;

            var btnBill = MkBtn("💳 Request Bill", new Point(308, 16), 150, 32, Color.FromArgb(50, 40, 10), Color.FromArgb(255, 190, 40));
            btnBill.Click += BtnBill_Click;

            var btnReceipt = MkBtn("🖨 View Receipt", new Point(468, 16), 150, 32, Color.FromArgb(30, 50, 30), Color.FromArgb(50, 200, 120));
            btnReceipt.Click += BtnReceipt_Click;

            lblOrderDetail = new Label
            {
                Location = new Point(630, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                BackColor = Color.Transparent
            };

            dgvOrders = StyledGrid(new Point(16, 58), new Size(500, 580));
            dgvOrders.SelectionChanged += (s, e) => LoadOrderItems();

            var lblItems = Lbl("Order Items:", new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(530, 58));
            dgvOrderItems = StyledGrid(new Point(530, 82), new Size(580, 556));

            tab.Controls.AddRange(new Control[] { btnRef, btnCancel, btnBill, btnReceipt, lblOrderDetail, dgvOrders, lblItems, dgvOrderItems });
            return tab;
        }

        private void LoadActiveOrders()
        {
            using (var ordersData = DB.Query(
                @"SELECT OrderId AS [#],CustomerName AS Customer,OrderType AS Type,
                  TableNumber AS [Table],Status,TotalAmount AS [Rs],
                  datetime(CreatedAt,'localtime') AS Time
                  FROM Orders WHERE Status NOT IN('Closed','Cancelled')
                  ORDER BY CreatedAt DESC"))
            {
                dgvOrders.DataSource = ordersData;
            }
        }

        private void LoadOrderItems()
        {
            if (dgvOrders.CurrentRow == null) return;
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);

            using (var itemsData = DB.Query(
                @"SELECT OrderItemId AS [#],ItemName AS Item,Quantity AS Qty,
                  UnitPrice AS [Price Rs],ROUND(Quantity*UnitPrice,0) AS [Subtotal Rs],
                  Instructions AS Note,Status
                  FROM OrderItems WHERE OrderId=@id",
                DB.P("@id", oid)))
            {
                dgvOrderItems.DataSource = itemsData;
            }

            lblOrderDetail.Text = $"Order #{oid}  |  {dgvOrders.CurrentRow.Cells["Customer"].Value}  |  {dgvOrders.CurrentRow.Cells["Status"].Value}";
        }

        private void BtnCancel_Click(object s, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            string status = dgvOrders.CurrentRow.Cells["Status"].Value.ToString();
            if (status == "SentToKitchen" || status == "InPreparation")
            { MessageBox.Show("Order already in kitchen — cannot cancel.", "Warning"); return; }
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            if (MessageBox.Show($"Cancel order #{oid}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { DB.NonQuery("UPDATE Orders SET Status='Cancelled',UpdatedAt=CURRENT_TIMESTAMP WHERE OrderId=@id", DB.P("@id", oid)); LoadActiveOrders(); }
        }

        private void BtnBill_Click(object s, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            DB.NonQuery("UPDATE Orders SET Status='BillRequested',UpdatedAt=CURRENT_TIMESTAMP WHERE OrderId=@id", DB.P("@id", oid));
            MessageBox.Show($"Bill requested for order #{oid}.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadActiveOrders();
        }

        private void BtnReceipt_Click(object s, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            ShowReceiptDialog(oid);
        }

        private void ShowReceiptDialog(int orderId)
        {
            DataTable orderDt = null;
            DataTable items = null;

            using (var orderData = DB.Query("SELECT * FROM Orders WHERE OrderId=@id", DB.P("@id", orderId)))
            {
                orderDt = orderData;
                if (orderDt.Rows.Count == 0) return;

                using (var itemsData = DB.Query("SELECT ItemName,Quantity,UnitPrice,ROUND(Quantity*UnitPrice,0) AS Subtotal FROM OrderItems WHERE OrderId=@id", DB.P("@id", orderId)))
                {
                    items = itemsData;
                    var o = orderDt.Rows[0];

                    var dlg = new Form
                    {
                        Text = $"Receipt — Order #{orderId}",
                        Size = new Size(440, 560),
                        StartPosition = FormStartPosition.CenterParent,
                        BackColor = Color.FromArgb(28, 28, 38),
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false
                    };
                    var rtb = new RichTextBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(28, 28, 38),
                        ForeColor = Color.FromArgb(220, 220, 232),
                        Font = new Font("Consolas", 10),
                        ReadOnly = true,
                        BorderStyle = BorderStyle.None
                    };

                    string receipt = BuildReceiptText(orderId, o, items);
                    rtb.Text = receipt;
                    var btnPrint = new Button
                    {
                        Text = "🖨 Print",
                        Dock = DockStyle.Bottom,
                        Height = 40,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(255, 140, 30),
                        ForeColor = Color.FromArgb(18, 18, 24),
                        Font = new Font("Segoe UI", 11, FontStyle.Bold)
                    };
                    btnPrint.FlatAppearance.BorderSize = 0;
                    btnPrint.Click += (s, e) => PrintReceipt(receipt);
                    dlg.Controls.AddRange(new Control[] { rtb, btnPrint });
                    dlg.ShowDialog(this);
                }
            }
        }

        private static string BuildReceiptText(int orderId, DataRow o, DataTable items)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("================================");
            sb.AppendLine("       STEAKAWAY RESTAURANT     ");
            sb.AppendLine("================================");
            sb.AppendLine($"Order #  : {orderId}");
            sb.AppendLine($"Customer : {o["CustomerName"]}");
            sb.AppendLine($"Type     : {o["OrderType"]}");
            if (o["TableNumber"].ToString() != "") sb.AppendLine($"Table    : {o["TableNumber"]}");
            sb.AppendLine($"Date     : {DateTime.Now:dd MMM yyyy  HH:mm}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"{"Item",-20}{"Qty",4}{"Price",10}{"Sub",10}");
            sb.AppendLine("--------------------------------");
            decimal total = 0;
            foreach (DataRow r in items.Rows)
            {
                decimal sub = Convert.ToDecimal(r["Subtotal"]);
                sb.AppendLine($"{r["ItemName"].ToString(),-20}{Convert.ToInt32(r["Quantity"]),4}{Convert.ToDecimal(r["UnitPrice"]),10:F0}{sub,10:F0}");
                total += sub;
            }
            sb.AppendLine("--------------------------------");
            decimal disc = Convert.ToDecimal(o["Discount"]);
            decimal tax = Convert.ToDecimal(o["Tax"]);
            decimal final = Convert.ToDecimal(o["FinalAmount"]) == 0 ? total : Convert.ToDecimal(o["FinalAmount"]);
            sb.AppendLine($"{"Subtotal",-30}{total,10:F0}");
            if (disc > 0) sb.AppendLine($"{"Discount",-30}{-disc,10:F0}");
            if (tax > 0) sb.AppendLine($"{"Tax",-30}{tax,10:F1}%");
            sb.AppendLine($"{"TOTAL",-30}{final,10:F0}");
            sb.AppendLine("================================");
            sb.AppendLine("  Thank you for dining with us! ");
            sb.AppendLine("================================");
            return sb.ToString();
        }

        private static void PrintReceipt(string text)
        {
            var pd = new PrintDocument();
            pd.PrintPage += (s, e) =>
            {
                e.Graphics.DrawString(text, new Font("Courier New", 9), Brushes.Black,
                    new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width, e.MarginBounds.Height));
            };
            var dlg2 = new PrintDialog { Document = pd };
            if (dlg2.ShowDialog() == DialogResult.OK) pd.Print();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  NEW ORDER TAB
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildNewOrderTab()
        {
            var tab = DarkTab("➕ New Order");

            // Customer info card
            var infoCard = Card(16, 16, 700, 100, "Customer & Order Info");
            SmallLbl(infoCard, "Customer Name", new Point(14, 40)); txtCustName = Input(infoCard, new Point(14, 58), 180);
            SmallLbl(infoCard, "Order Type", new Point(210, 40));
            cmbOrderType = new ComboBox
            {
                Location = new Point(210, 58),
                Size = new Size(130, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbOrderType.Items.AddRange(new[] { "DineIn", "Takeaway" }); cmbOrderType.SelectedIndex = 0;
            cmbOrderType.SelectedIndexChanged += (s, e) => { txtTableNum.Visible = cmbOrderType.SelectedItem.ToString() == "DineIn"; };
            infoCard.Controls.Add(cmbOrderType);
            SmallLbl(infoCard, "Table #", new Point(360, 40)); txtTableNum = Input(infoCard, new Point(360, 58), 80);
            SmallLbl(infoCard, "Special Notes", new Point(460, 40)); txtOrderNotes = Input(infoCard, new Point(460, 58), 220);
            tab.Controls.Add(infoCard);

            // Menu panel
            var menuCard = Card(16, 130, 500, 515, "Menu");
            SmallLbl(menuCard, "Search:", new Point(14, 40)); txtSearchMenu = Input(menuCard, new Point(70, 38), 180);
            txtSearchMenu.TextChanged += (s, e) => RefreshMenuGrid();
            SmallLbl(menuCard, "Category:", new Point(265, 40));
            cmbMenuCat = new ComboBox { Location = new Point(335, 38), Size = new Size(150, 26), BackColor = Color.FromArgb(35, 35, 48), ForeColor = Color.FromArgb(220, 220, 232), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };

            using (var categories = DB.Query("SELECT Name FROM Categories ORDER BY Name"))
            {
                cmbMenuCat.Items.Add("All");
                foreach (DataRow r in categories.Rows)
                    cmbMenuCat.Items.Add(r["Name"].ToString());
            }

            cmbMenuCat.SelectedIndex = 0; cmbMenuCat.SelectedIndexChanged += (s, e) => RefreshMenuGrid();
            menuCard.Controls.Add(cmbMenuCat);

            dgvMenu = StyledGrid(new Point(0, 70), new Size(498, 340));
            RefreshMenuGrid();
            menuCard.Controls.Add(dgvMenu);

            SmallLbl(menuCard, "Qty:", new Point(14, 422));
            nudQty = new NumericUpDown { Location = new Point(48, 420), Size = new Size(70, 26), Minimum = 1, Maximum = 99, Value = 1, BackColor = Color.FromArgb(35, 35, 48), ForeColor = Color.FromArgb(220, 220, 232), Font = new Font("Segoe UI", 9) };
            menuCard.Controls.Add(nudQty);
            SmallLbl(menuCard, "Instruction:", new Point(130, 422)); txtItemInstr = Input(menuCard, new Point(220, 420), 260);

            var btnAddToCart = MkBtn("Add to Cart →", new Point(14, 456), 180, 36, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnAddToCart.Click += BtnAddToCart_Click;
            menuCard.Controls.Add(btnAddToCart);
            tab.Controls.Add(menuCard);

            // Cart panel
            var cartCard = Card(530, 130, 580, 515, "Order Cart");
            dgvCart = StyledGrid(new Point(0, 40), new Size(578, 340));
            cartCard.Controls.Add(dgvCart);

            lblCartTotal = new Label
            {
                Location = new Point(14, 390),
                AutoSize = true,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                BackColor = Color.Transparent
            };

            var btnRemove = MkBtn("✕ Remove Item", new Point(14, 418), 160, 34, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnRemove.Click += (s, e) => {
                if (dgvCart.CurrentRow == null || _cart.Count == 0) return;
                int idx = dgvCart.CurrentRow.Index;
                if (idx >= 0 && idx < _cart.Count) { _cart.RemoveAt(idx); RefreshCart(); }
            };

            var btnClearCart = MkBtn("🗑 Clear", new Point(184, 418), 100, 34, Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180));
            btnClearCart.Click += (s, e) => { _cart.Clear(); RefreshCart(); };

            var btnPlaceOrder = new Button
            {
                Text = "✔ PLACE ORDER",
                Location = new Point(14, 462),
                Size = new Size(540, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPlaceOrder.FlatAppearance.BorderSize = 0;
            btnPlaceOrder.Click += BtnPlaceOrder_Click;

            cartCard.Controls.AddRange(new Control[] { lblCartTotal, btnRemove, btnClearCart, btnPlaceOrder });
            tab.Controls.Add(cartCard);
            return tab;
        }

        private void RefreshMenuGrid()
        {
            string srch = txtSearchMenu.Text.Trim().ToLower();
            string cat = cmbMenuCat.SelectedIndex > 0 ? cmbMenuCat.SelectedItem.ToString() : null;
            string sql = @"SELECT m.ItemId,m.Name,c.Name AS Cat,m.Price,m.Description
                            FROM MenuItems m JOIN Categories c ON m.CategoryId=c.CategoryId
                            WHERE m.IsAvailable=1";
            if (cat != null) sql += " AND c.Name=@cat";
            var p = cat != null ? new[] { DB.P("@cat", cat) } : Array.Empty<System.Data.SQLite.SQLiteParameter>();

            using (var dt = DB.Query(sql, p))
            {
                if (!string.IsNullOrEmpty(srch))
                {
                    var filtered = dt.Clone();
                    foreach (DataRow r in dt.Rows)
                        if (r["Name"].ToString().ToLower().Contains(srch) || r["Cat"].ToString().ToLower().Contains(srch))
                            filtered.ImportRow(r);
                    dgvMenu.DataSource = filtered;
                }
                else dgvMenu.DataSource = dt;
            }
        }

        private void BtnAddToCart_Click(object s, EventArgs e)
        {
            if (dgvMenu.CurrentRow == null) return;
            var row = (DataRowView)dgvMenu.CurrentRow.DataBoundItem;
            _cart.Add(new CartRow
            {
                ItemId = (int)(long)row["ItemId"],
                Name = row["Name"].ToString(),
                Price = Convert.ToDecimal(row["Price"]),
                Qty = (int)nudQty.Value,
                Note = txtItemInstr.Text.Trim()
            });
            txtItemInstr.Clear(); nudQty.Value = 1;
            RefreshCart();
        }

        private void RefreshCart()
        {
            var dt = new DataTable();
            dt.Columns.Add("Item"); dt.Columns.Add("Qty"); dt.Columns.Add("Price Rs"); dt.Columns.Add("Subtotal Rs"); dt.Columns.Add("Note");
            decimal total = 0;
            foreach (var c in _cart)
            {
                decimal sub = c.Price * c.Qty;
                dt.Rows.Add(c.Name, c.Qty, $"{c.Price:F0}", $"{sub:F0}", c.Note);
                total += sub;
            }
            dgvCart.DataSource = dt;
            lblCartTotal.Text = $"Total: Rs {total:F0}";
        }

        private void BtnPlaceOrder_Click(object s, EventArgs e)
        {
            if (_cart.Count == 0) { MessageBox.Show("Cart is empty.", "Info"); return; }
            string type = cmbOrderType.SelectedItem.ToString();
            string table = txtTableNum.Text.Trim();
            if (type == "DineIn" && string.IsNullOrWhiteSpace(table))
            { MessageBox.Show("Please enter table number for dine-in.", "Validation"); return; }

            long oid = DB.Insert(
                @"INSERT INTO Orders(CustomerName,OrderType,TableNumber,SpecialNotes,WaiterId,Status)
                  VALUES(@cn,@ot,@tn,@sn,@wid,'Pending')",
                DB.P("@cn", string.IsNullOrWhiteSpace(txtCustName.Text) ? "Guest" : txtCustName.Text.Trim()),
                DB.P("@ot", type),
                DB.P("@tn", table),
                DB.P("@sn", txtOrderNotes.Text.Trim()),
                DB.P("@wid", SessionManager.UserId));

            foreach (var item in _cart)
                DB.NonQuery("INSERT INTO OrderItems(OrderId,ItemId,ItemName,Quantity,UnitPrice,Instructions) VALUES(@oid,@iid,@n,@q,@p,@ins)",
                    DB.P("@oid", (int)oid), DB.P("@iid", item.ItemId), DB.P("@n", item.Name),
                    DB.P("@q", item.Qty), DB.P("@p", (double)item.Price), DB.P("@ins", item.Note));

            DB.NonQuery("UPDATE Orders SET TotalAmount=(SELECT COALESCE(SUM(Quantity*UnitPrice),0) FROM OrderItems WHERE OrderId=@id),FinalAmount=(SELECT COALESCE(SUM(Quantity*UnitPrice),0) FROM OrderItems WHERE OrderId=@id),Status='SentToKitchen' WHERE OrderId=@id",
                DB.P("@id", (int)oid));

            MessageBox.Show($"✅  Order #{oid} placed and sent to kitchen!", "Order Placed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _cart.Clear(); RefreshCart();
            txtCustName.Clear(); txtTableNum.Clear(); txtOrderNotes.Clear();
            tabs.SelectedIndex = 0; LoadActiveOrders();
        }

        // Helpers
        private static TabPage DarkTab(string t) { var p = new TabPage(t) { BackColor = Color.FromArgb(18, 18, 24), UseVisualStyleBackColor = false }; return p; }

        private static Panel Card(int x, int y, int w, int h, string title)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.FromArgb(28, 28, 38) };
            p.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(50, 50, 68), 1)) e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
            if (!string.IsNullOrEmpty(title)) p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 30), AutoSize = true, BackColor = Color.Transparent, Location = new Point(14, 10) });
            return p;
        }

        private static DataGridView StyledGrid(Point loc, Size sz)
        {
            var g = new DataGridView { Location = loc, Size = sz, BackgroundColor = Color.FromArgb(22, 22, 32), GridColor = Color.FromArgb(40, 40, 56), BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, EnableHeadersVisualStyles = false };
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

        private static TextBox Input(Control parent, Point p, int w, bool pwd = false)
        {
            var tb = new TextBox { Location = p, Size = new Size(w, 26), BackColor = Color.FromArgb(35, 35, 48), ForeColor = Color.FromArgb(220, 220, 232), Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle, PasswordChar = pwd ? '●' : '\0' };
            parent.Controls.Add(tb);
            return tb;
        }

        private static void SmallLbl(Control parent, string t, Point p) => parent.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(110, 110, 130), AutoSize = true, BackColor = Color.Transparent, Location = new Point(p.X, p.Y + 4) });

        private static Label Lbl(string t, Font f, Color c, Point p) => new Label { Text = t, Font = f, ForeColor = c, AutoSize = true, BackColor = Color.Transparent, Location = p };

        private static Button MkBtn(string t, Point p, int w, int h, Color bg, Color fg)
        {
            var b = new Button { Text = t, Location = p, Size = new Size(w, h), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = fg, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}