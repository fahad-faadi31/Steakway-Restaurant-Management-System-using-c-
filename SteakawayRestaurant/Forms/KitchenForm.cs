using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;

namespace SteakawayRestaurant.Forms
{
    public class KitchenForm : Form
    {
        private DataGridView dgvOrders, dgvItems;
        private Label lblOrderInfo, lblSpecialNote;
        private Panel pnlStatus;
        private Timer refreshTimer;

        public KitchenForm()
        {
            this.Text = $"XP (Kitchen) Panel — {SessionManager.FullName}";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(1000, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
            LoadOrders();

            refreshTimer = new Timer { Interval = 30000 };
            refreshTimer.Tick += (s, e) => LoadOrders();
            refreshTimer.Start();
        }

        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(24, 24, 36) };
            hdr.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, hdr.Width, 4);
            hdr.Controls.Add(Lbl($"🥩  Steakaway  |  XP (Order Expeditor) Panel  |  {SessionManager.FullName}",
                new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, 16)));

            var btnLogout = MkBtn("Logout", Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70), 100, 32);
            btnLogout.Location = new Point(hdr.Width - 120, 10);
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += (s, e) => { refreshTimer.Stop(); this.Close(); };
            hdr.Controls.Add(btnLogout);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(22, 22, 32) };

            var btnRefresh = MkBtn("↻  Refresh", Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180), 120, 36);
            var btnPreparing = MkBtn("🔥  In Preparation", Color.FromArgb(50, 40, 10), Color.FromArgb(255, 190, 40), 160, 36);
            var btnPrepared = MkBtn("✅  Mark Prepared", Color.FromArgb(20, 50, 30), Color.FromArgb(50, 200, 120), 160, 36);
            var btnReady = MkBtn("🚀  Order Ready", Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24), 140, 36);

            btnRefresh.Location = new Point(8, 8);
            btnPreparing.Location = new Point(138, 8);
            btnPrepared.Location = new Point(308, 8);
            btnReady.Location = new Point(478, 8);

            btnRefresh.Click += (s, e) => LoadOrders();
            btnPreparing.Click += (s, e) => MarkItemStatus("InPreparation");
            btnPrepared.Click += (s, e) => MarkItemStatus("Prepared");
            btnReady.Click += BtnOrderReady_Click;

            var legend = new Label
            {
                Text = "⬛ Pending   🟡 InPreparation   🟢 Prepared",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 100, 120),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(640, 18)
            };
            toolbar.Controls.AddRange(new Control[] { btnRefresh, btnPreparing, btnPrepared, btnReady, legend });

            // Main layout
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 24), Padding = new Padding(12) };

            // LEFT: Orders list
            var leftCard = Card(12, 12, 430, 570, "Incoming Kitchen Orders");

            dgvOrders = StyledGrid(new Point(0, 38), new Size(428, 530));
            dgvOrders.SelectionChanged += (s, e) => LoadItemsForSelectedOrder();
            dgvOrders.RowPrePaint += DgvOrders_RowPrePaint;
            leftCard.Controls.Add(dgvOrders);
            body.Controls.Add(leftCard);

            // RIGHT: Items + info
            var rightCard = Card(458, 12, 650, 570, "Item-wise Breakdown");

            lblOrderInfo = new Label
            {
                Text = "Select an order to view its items.",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = false,
                Size = new Size(640, 22),
                Location = new Point(8, 40),
                BackColor = Color.Transparent
            };

            lblSpecialNote = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 190, 40),
                AutoSize = false,
                Size = new Size(640, 20),
                Location = new Point(8, 64),
                BackColor = Color.Transparent
            };

            dgvItems = StyledGrid(new Point(0, 88), new Size(648, 380));
            dgvItems.RowPrePaint += DgvItems_RowPrePaint;

            // Status bar at bottom of right card
            pnlStatus = new Panel
            {
                Location = new Point(0, 476),
                Size = new Size(648, 90),
                BackColor = Color.FromArgb(22, 22, 32)
            };
            pnlStatus.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 50, 68), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlStatus.Width - 1, pnlStatus.Height - 1);
                }
                e.Graphics.DrawString("Quick Status Legend:", new Font("Segoe UI", 8, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(160, 160, 180)), new PointF(10, 8));

                DrawBadge(e.Graphics, "Pending", Color.FromArgb(255, 190, 40), 10, 28);
                DrawBadge(e.Graphics, "InPreparation", Color.FromArgb(100, 160, 255), 130, 28);
                DrawBadge(e.Graphics, "Prepared", Color.FromArgb(50, 200, 120), 280, 28);

                e.Graphics.DrawString("Select item in grid, then click toolbar buttons to update status.",
                    new Font("Segoe UI", 8), new SolidBrush(Color.FromArgb(100, 100, 120)), new PointF(10, 58));
            };

            rightCard.Controls.AddRange(new Control[] { lblOrderInfo, lblSpecialNote, dgvItems, pnlStatus });
            body.Controls.Add(rightCard);

            this.Controls.Add(body);
            this.Controls.Add(toolbar);
            this.Controls.Add(hdr);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DATA LOADING - FIXED to show all orders that need kitchen attention
        // ══════════════════════════════════════════════════════════════════════
        private void LoadOrders()
        {
            int? selected = null;
            if (dgvOrders.CurrentRow != null)
                selected = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);

            // FIXED: Include more statuses that require kitchen attention
            // Now shows: SentToKitchen, InPreparation, Pending (for new orders), BillRequested (if not paid yet)
            using (var ordersData = DB.Query(
                @"SELECT o.OrderId AS [#],
                         o.CustomerName AS Customer,
                         o.OrderType AS Type,
                         o.TableNumber AS [Table#],
                         o.Status,
                         o.SpecialNotes AS [Special Note],
                         datetime(o.CreatedAt,'localtime') AS Time
                  FROM Orders o
                  WHERE o.Status IN ('SentToKitchen', 'InPreparation', 'Pending', 'BillRequested')
                  ORDER BY 
                      CASE o.Status
                          WHEN 'InPreparation' THEN 1
                          WHEN 'SentToKitchen' THEN 2
                          WHEN 'Pending' THEN 3
                          ELSE 4
                      END,
                      o.CreatedAt ASC"))
            {
                dgvOrders.DataSource = ordersData;
            }

            // Re-select previously selected row
            if (selected.HasValue)
            {
                foreach (DataGridViewRow row in dgvOrders.Rows)
                {
                    if (row.Cells["#"].Value != null && Convert.ToInt32(row.Cells["#"].Value) == selected.Value)
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
        }

        private void LoadItemsForSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null) return;

            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
            string cn = dgvOrders.CurrentRow.Cells["Customer"].Value?.ToString() ?? "";
            string st = dgvOrders.CurrentRow.Cells["Status"].Value?.ToString() ?? "";
            string sn = dgvOrders.CurrentRow.Cells["Special Note"].Value?.ToString() ?? "";
            string tp = dgvOrders.CurrentRow.Cells["Type"].Value?.ToString() ?? "";

            lblOrderInfo.Text = $"Order #{oid}  ·  {cn}  ·  {tp}  ·  Status: {st}";
            lblSpecialNote.Text = string.IsNullOrWhiteSpace(sn) ? "" : $"⚠  Special Note: {sn}";

            using (var itemsData = DB.Query(
                @"SELECT OrderItemId AS [ItemID],
                         ItemName AS [Item],
                         Quantity AS [Qty],
                         Instructions AS [Special Instruction],
                         Status
                  FROM OrderItems
                  WHERE OrderId = @id",
                DB.P("@id", oid)))
            {
                dgvItems.DataSource = itemsData;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIONS
        // ══════════════════════════════════════════════════════════════════════
        private void MarkItemStatus(string status)
        {
            if (dgvItems.CurrentRow == null)
            {
                MessageBox.Show("Select an item from the breakdown first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int itemId = Convert.ToInt32(dgvItems.CurrentRow.Cells["ItemID"].Value);
            DB.NonQuery("UPDATE OrderItems SET Status=@s WHERE OrderItemId=@id",
                DB.P("@s", status), DB.P("@id", itemId));

            // If any item is InPreparation, update parent order status too
            if (status == "InPreparation" && dgvOrders.CurrentRow != null)
            {
                int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);
                DB.NonQuery("UPDATE Orders SET Status='InPreparation', UpdatedAt=CURRENT_TIMESTAMP WHERE OrderId=@id AND Status IN ('SentToKitchen', 'Pending')",
                    DB.P("@id", oid));
            }

            LoadItemsForSelectedOrder();
            LoadOrders(); // Refresh the orders list to update status colors
        }

        private void BtnOrderReady_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Select an order first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int oid = Convert.ToInt32(dgvOrders.CurrentRow.Cells["#"].Value);

            // Verify ALL items are Prepared
            DataTable items = null;
            using (var itemsData = DB.Query(
                "SELECT Status FROM OrderItems WHERE OrderId=@id",
                DB.P("@id", oid)))
            {
                items = itemsData;

                bool allPrepared = true;
                foreach (DataRow r in items.Rows)
                {
                    if (r["Status"].ToString() != "Prepared")
                    {
                        allPrepared = false;
                        break;
                    }
                }

                if (!allPrepared)
                {
                    var res = MessageBox.Show(
                        "Not all items are marked Prepared yet.\nMark all items as Prepared and continue?",
                        "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.No) return;

                    // Auto-mark remaining items
                    DB.NonQuery("UPDATE OrderItems SET Status='Prepared' WHERE OrderId=@id AND Status!='Prepared'",
                        DB.P("@id", oid));
                }
            }

            // Get order details
            DataTable orderRow = null;
            using (var orderData = DB.Query("SELECT OrderType, CustomerName, TableNumber FROM Orders WHERE OrderId=@id", DB.P("@id", oid)))
            {
                orderRow = orderData;
                string type = orderRow.Rows[0]["OrderType"].ToString();
                string cname = orderRow.Rows[0]["CustomerName"].ToString();
                string table = orderRow.Rows[0]["TableNumber"].ToString();

                // Update order status based on type
                string nextStatus = (type == "Online") ? "ReadyForDelivery" : "ReadyForDelivery";
                DB.NonQuery("UPDATE Orders SET Status=@s, UpdatedAt=CURRENT_TIMESTAMP WHERE OrderId=@id",
                    DB.P("@s", nextStatus), DB.P("@id", oid));

                // Notification message
                string notify = type == "Online"
                    ? $"✅  Order #{oid} is ready for delivery!\n📦  Notify RIDER for: {cname}"
                    : $"✅  Order #{oid} is ready!\n🍽  Notify WAITER for Table: {(string.IsNullOrWhiteSpace(table) ? "Takeaway" : table)}, Customer: {cname}";

                MessageBox.Show(notify, "Order Ready!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            LoadOrders();
            dgvItems.DataSource = null;
            lblOrderInfo.Text = "Select an order to view its items.";
            lblSpecialNote.Text = "";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ROW COLORING
        // ══════════════════════════════════════════════════════════════════════
        private void DgvOrders_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || dgvOrders.Rows[e.RowIndex].DataBoundItem == null) return;
            string status = dgvOrders.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";
            Color bg;

            switch (status)
            {
                case "SentToKitchen":
                    bg = Color.FromArgb(30, 50, 30, 60);
                    break;
                case "InPreparation":
                    bg = Color.FromArgb(50, 40, 10, 80);
                    break;
                case "Pending":
                    bg = Color.FromArgb(40, 30, 20, 70);
                    break;
                default:
                    bg = Color.FromArgb(22, 22, 32);
                    break;
            }

            dgvOrders.Rows[e.RowIndex].DefaultCellStyle.BackColor = bg;
        }

        private void DgvItems_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || dgvItems.Rows[e.RowIndex].DataBoundItem == null) return;
            string status = dgvItems.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";
            Color fg;

            switch (status)
            {
                case "Prepared":
                    fg = Color.FromArgb(50, 200, 120);
                    break;
                case "InPreparation":
                    fg = Color.FromArgb(255, 190, 40);
                    break;
                default:
                    fg = Color.FromArgb(180, 180, 200);
                    break;
            }

            dgvItems.Rows[e.RowIndex].DefaultCellStyle.ForeColor = fg;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            base.OnFormClosed(e);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private static void DrawBadge(Graphics g, string text, Color color, int x, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var rect = new RectangleF(x, y, 110, 22);
            g.FillRectangle(new SolidBrush(Color.FromArgb(50, color.R, color.G, color.B)), rect);
            g.DrawRectangle(new Pen(color, 1), rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            g.DrawString(text, new Font("Segoe UI", 8), new SolidBrush(color), rect, sf);
        }

        private static Panel Card(int x, int y, int w, int h, string title)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.FromArgb(28, 28, 38) };
            p.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(50, 50, 68), 1)) e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
            if (!string.IsNullOrEmpty(title))
                p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 30), AutoSize = true, BackColor = Color.Transparent, Location = new Point(8, 10) });
            return p;
        }

        private static DataGridView StyledGrid(Point loc, Size sz)
        {
            var g = new DataGridView
            {
                Location = loc,
                Size = sz,
                BackgroundColor = Color.FromArgb(22, 22, 32),
                GridColor = Color.FromArgb(40, 40, 56),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
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
            g.RowTemplate.Height = 34;
            return g;
        }

        private static Label Lbl(string t, Font f, Color c, Point p) =>
            new Label { Text = t, Font = f, ForeColor = c, AutoSize = true, BackColor = Color.Transparent, Location = p };

        private static Button MkBtn(string text, Color bg, Color fg, int w, int h)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}