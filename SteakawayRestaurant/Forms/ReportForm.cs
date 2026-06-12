using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SteakawayRestaurant.Database;

namespace SteakawayRestaurant.Forms
{
    /// <summary>
    /// Standalone report window – can be launched from ManagerForm or independently.
    /// Covers: Sales by date range, Top menu items, Orders by type, Staff performance.
    /// </summary>
    public class ReportForm : Form
    {
        private TabControl tabs;

        // Sales tab
        private DataGridView dgvSales;
        private DateTimePicker dtpFrom, dtpTo;
        private Label lblSalesTotal, lblSalesCount;

        // Top items tab
        private DataGridView dgvTopItems;
        private DateTimePicker dtpItemsFrom, dtpItemsTo;

        // Order types tab
        private DataGridView dgvTypes;
        private Label lblTypeSummary;

        // Staff tab
        private DataGridView dgvStaff;

        public ReportForm()
        {
            this.Text = "Reports & Analytics";
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(24, 24, 36) };
            hdr.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, hdr.Width, 4);
            hdr.Controls.Add(new Label
            {
                Text = "📊  Steakaway  |  Reports & Analytics",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 16)
            });

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(200, 36);
            tabs.DrawItem += DrawTab;

            tabs.TabPages.Add(BuildSalesTab());
            tabs.TabPages.Add(BuildTopItemsTab());
            tabs.TabPages.Add(BuildOrderTypesTab());
            tabs.TabPages.Add(BuildStaffTab());

            this.Controls.Add(tabs);
            this.Controls.Add(hdr);
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var t = tabs.TabPages[e.Index]; var b = tabs.GetTabRect(e.Index);
            bool sel = e.Index == tabs.SelectedIndex;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(28, 28, 38) : Color.FromArgb(18, 18, 24)), b);
            if (sel) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), b.Left, b.Top, b.Width, 3);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(t.Text, new Font("Segoe UI", 10, sel ? FontStyle.Bold : FontStyle.Regular),
                new SolidBrush(sel ? Color.FromArgb(255, 140, 30) : Color.FromArgb(130, 130, 150)), b, sf);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SALES BY DATE RANGE
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildSalesTab()
        {
            var tab = DarkTab("💰 Sales Report");

            var bar = new Panel { Location = new Point(12, 12), Size = new Size(1050, 50), BackColor = Color.Transparent };
            SL(bar, "From:", new Point(0, 14));
            dtpFrom = DTP(bar, new Point(42, 10));
            SL(bar, "To:", new Point(218, 14));
            dtpTo = DTP(bar, new Point(245, 10));
            var btn = Btn("Load Sales", new Point(420, 10), 130, 30);
            btn.Click += (s, e) => LoadSales();
            bar.Controls.Add(btn);

            lblSalesCount = KpiLbl("Orders: 0", new Point(570, 14));
            lblSalesTotal = KpiLbl("Revenue: Rs 0", new Point(720, 14));
            bar.Controls.AddRange(new Control[] { lblSalesCount, lblSalesTotal });
            tab.Controls.Add(bar);

            dgvSales = Grid(new Point(12, 70), new Size(1050, 546));
            tab.Controls.Add(dgvSales);
            LoadSales();
            return tab;
        }

        private void LoadSales()
        {
            string f = dtpFrom.Value.Date.ToString("yyyy-MM-dd");
            string t = dtpTo.Value.Date.ToString("yyyy-MM-dd");

            DataTable dt = null;
            using (var salesData = DB.Query(
                @"SELECT o.OrderId AS [#], o.CustomerName AS Customer,
                         o.OrderType AS Type, o.TableNumber AS Table,
                         o.TotalAmount AS [Subtotal Rs], o.Discount AS [Disc Rs],
                         o.FinalAmount AS [Final Rs], o.PaymentMethod AS Payment,
                         o.Rating AS Stars,
                         datetime(o.CreatedAt,'localtime') AS DateTime
                  FROM Orders o
                  WHERE DATE(o.CreatedAt)>=DATE(@f) AND DATE(o.CreatedAt)<=DATE(@t)
                    AND o.Status='Closed'
                  ORDER BY o.CreatedAt DESC",
                DB.P("@f", f), DB.P("@t", t)))
            {
                dt = salesData;
                dgvSales.DataSource = dt;
                double rev = 0;
                foreach (DataRow r in dt.Rows) rev += Convert.ToDouble(r["Final Rs"]);
                lblSalesCount.Text = $"Orders: {dt.Rows.Count}";
                lblSalesTotal.Text = $"Revenue: Rs {rev:F2}";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TOP MENU ITEMS
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildTopItemsTab()
        {
            var tab = DarkTab("🍽 Top Menu Items");

            var bar = new Panel { Location = new Point(12, 12), Size = new Size(1050, 50), BackColor = Color.Transparent };
            SL(bar, "From:", new Point(0, 14));
            dtpItemsFrom = DTP(bar, new Point(42, 10));
            SL(bar, "To:", new Point(218, 14));
            dtpItemsTo = DTP(bar, new Point(245, 10));
            var btn = Btn("Load", new Point(420, 10), 100, 30);
            btn.Click += (s, e) => LoadTopItems();
            bar.Controls.Add(btn);
            tab.Controls.Add(bar);

            dgvTopItems = Grid(new Point(12, 70), new Size(1050, 546));
            tab.Controls.Add(dgvTopItems);
            LoadTopItems();
            return tab;
        }

        private void LoadTopItems()
        {
            string f = dtpItemsFrom.Value.Date.ToString("yyyy-MM-dd");
            string t = dtpItemsTo.Value.Date.ToString("yyyy-MM-dd");

            using (var itemsData = DB.Query(
                @"SELECT oi.ItemName AS [Menu Item],
                         SUM(oi.Quantity) AS [Total Qty Sold],
                         COUNT(DISTINCT oi.OrderId) AS [In # Orders],
                         ROUND(SUM(oi.Quantity*oi.UnitPrice),0) AS [Revenue Rs]
                  FROM OrderItems oi
                  JOIN Orders o ON oi.OrderId=o.OrderId
                  WHERE DATE(o.CreatedAt)>=DATE(@f) AND DATE(o.CreatedAt)<=DATE(@t)
                    AND o.Status='Closed'
                  GROUP BY oi.ItemName
                  ORDER BY [Total Qty Sold] DESC",
                DB.P("@f", f), DB.P("@t", t)))
            {
                dgvTopItems.DataSource = itemsData;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ORDERS BY TYPE
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildOrderTypesTab()
        {
            var tab = DarkTab("📋 Orders by Type");

            var btn = Btn("Refresh", new Point(12, 12), 120, 32);
            btn.Click += (s, e) => LoadOrderTypes();
            lblTypeSummary = KpiLbl("", new Point(150, 18));
            tab.Controls.AddRange(new Control[] { btn, lblTypeSummary });

            dgvTypes = Grid(new Point(12, 58), new Size(1050, 556));
            tab.Controls.Add(dgvTypes);
            LoadOrderTypes();
            return tab;
        }

        private void LoadOrderTypes()
        {
            DataTable dt = null;
            using (var typesData = DB.Query(
                @"SELECT OrderType AS Type,
                         COUNT(*) AS [Total Orders],
                         SUM(CASE WHEN Status='Closed' THEN 1 ELSE 0 END) AS Completed,
                         SUM(CASE WHEN Status='Cancelled' THEN 1 ELSE 0 END) AS Cancelled,
                         ROUND(SUM(CASE WHEN Status='Closed' THEN FinalAmount ELSE 0 END),0) AS [Revenue Rs]
                  FROM Orders
                  GROUP BY OrderType"))
            {
                dt = typesData;
                dgvTypes.DataSource = dt;
                double total = 0;
                foreach (DataRow r in dt.Rows) total += Convert.ToDouble(r["Revenue Rs"]);
                lblTypeSummary.Text = $"Total All-time Revenue: Rs {total:F2}";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STAFF PERFORMANCE
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildStaffTab()
        {
            var tab = DarkTab("👥 Staff Performance");
            var btn = Btn("Refresh", new Point(12, 12), 120, 32);
            btn.Click += (s, e) => LoadStaffPerf();
            tab.Controls.Add(btn);

            dgvStaff = Grid(new Point(12, 58), new Size(1050, 556));
            tab.Controls.Add(dgvStaff);
            LoadStaffPerf();
            return tab;
        }

        private void LoadStaffPerf()
        {
            using (var staffData = DB.Query(
                @"SELECT u.FullName AS [Staff Name], u.Role,
                         COUNT(o.OrderId) AS [Orders Handled],
                         ROUND(COALESCE(SUM(o.FinalAmount),0),0) AS [Revenue Rs],
                         ROUND(COALESCE(AVG(o.Rating),0),1) AS [Avg Rating]
                  FROM Users u
                  LEFT JOIN Orders o ON o.WaiterId=u.UserId AND o.Status='Closed'
                  WHERE u.Role IN ('Waiter','Cashier','XP') AND u.IsActive=1
                  GROUP BY u.UserId
                  ORDER BY [Orders Handled] DESC"))
            {
                dgvStaff.DataSource = staffData;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static TabPage DarkTab(string t) =>
            new TabPage(t) { BackColor = Color.FromArgb(18, 18, 24), UseVisualStyleBackColor = false };

        private static DataGridView Grid(Point loc, Size sz)
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
            g.RowTemplate.Height = 32;
            return g;
        }

        private static DateTimePicker DTP(Control parent, Point p)
        {
            var d = new DateTimePicker
            {
                Location = p,
                Size = new Size(170, 28),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232)
            };
            parent.Controls.Add(d); return d;
        }

        private static Button Btn(string text, Point p, int w, int h)
        {
            var b = new Button
            {
                Text = text,
                Location = p,
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private static Label KpiLbl(string text, Point p) =>
            new Label { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 30), AutoSize = true, BackColor = Color.Transparent, Location = p };

        private static void SL(Control parent, string t, Point p) =>
            parent.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(110, 110, 130), AutoSize = true, BackColor = Color.Transparent, Location = p });
    }
}