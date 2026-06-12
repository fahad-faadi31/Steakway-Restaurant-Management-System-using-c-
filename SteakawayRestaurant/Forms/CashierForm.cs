using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;

namespace SteakawayRestaurant.Forms
{
    public class CashierForm : Form
    {
        private TabControl tabs;

        // Billing tab
        private DataGridView dgvPendingOrders, dgvBillItems;
        private Label lblSubtotal, lblDiscount, lblTax, lblFinalTotal, lblOrderInfo;
        private TextBox txtDiscount, txtTax, txtPayNotes;
        private ComboBox cmbPayMethod;
        private Button btnProcessPayment;
        private int _selectedOrderId = -1;
        private decimal _baseTotal = 0;

        // Transactions tab
        private DataGridView dgvTransactions;
        private DateTimePicker dtpTxDate;

        // Daily Summary tab
        private DataGridView dgvSummary;
        private Label lblSummaryTotal, lblSummaryOrders, lblSummaryAvg;
        private DateTimePicker dtpSummaryDate;

        public CashierForm()
        {
            this.Text = $"Cashier Panel — {SessionManager.FullName}";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(1000, 640);
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
            hdr.Controls.Add(Lbl($"🥩  Steakaway  |  Cashier Panel  |  {SessionManager.FullName}",
                new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, 16)));
            var btnOut = MkBtn("Logout", Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70), 100, 32);
            btnOut.Location = new Point(hdr.Width - 120, 10);
            btnOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOut.Click += (s, e) => this.Close();
            hdr.Controls.Add(btnOut);

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(190, 38);
            tabs.DrawItem += DrawTab;

            tabs.TabPages.Add(BuildBillingTab());
            tabs.TabPages.Add(BuildTransactionsTab());
            tabs.TabPages.Add(BuildDailySummaryTab());

            this.Controls.Add(tabs);
            this.Controls.Add(hdr);
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var t = tabs.TabPages[e.Index];
            var b = tabs.GetTabRect(e.Index);
            bool sel = e.Index == tabs.SelectedIndex;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(28, 28, 38) : Color.FromArgb(18, 18, 24)), b);
            if (sel) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), b.Left, b.Top, b.Width, 3);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(t.Text, new Font("Segoe UI", 10, sel ? FontStyle.Bold : FontStyle.Regular),
                new SolidBrush(sel ? Color.FromArgb(255, 140, 30) : Color.FromArgb(130, 130, 150)), b, sf);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BILLING TAB
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildBillingTab()
        {
            var tab = DarkTab("💳 Billing & Payment");

            // Pending orders list
            var leftCard = Card(12, 12, 420, 588, "Pending Orders");
            var btnRef = MkBtn("↻ Refresh", Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180), 110, 28);
            btnRef.Location = new Point(300, 8);
            btnRef.Click += (s, e) => LoadPendingOrders();
            leftCard.Controls.Add(btnRef);

            dgvPendingOrders = StyledGrid(new Point(0, 40), new Size(418, 548));
            dgvPendingOrders.SelectionChanged += DgvPendingOrders_SelectionChanged;
            leftCard.Controls.Add(dgvPendingOrders);
            tab.Controls.Add(leftCard);
            LoadPendingOrders();

            // Bill card
            var billCard = Card(446, 12, 670, 588, "Generate Bill");

            lblOrderInfo = new Label
            {
                Text = "← Select an order to generate bill",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = false,
                Size = new Size(650, 24),
                Location = new Point(8, 40),
                BackColor = Color.Transparent
            };
            billCard.Controls.Add(lblOrderInfo);

            // Items grid
            dgvBillItems = StyledGrid(new Point(0, 68), new Size(668, 200));
            billCard.Controls.Add(dgvBillItems);

            // Totals panel
            var totCard = Card(0, 276, 668, 180, "");
            totCard.BackColor = Color.FromArgb(22, 22, 32);
            totCard.Paint += null;

            lblSubtotal = TotalLbl("Subtotal:      Rs 0.00", new Point(14, 10), Color.FromArgb(200, 200, 220));
            lblDiscount = TotalLbl("Discount:      Rs 0.00", new Point(14, 36), Color.FromArgb(230, 100, 100));
            lblTax = TotalLbl("Tax:           Rs 0.00", new Point(14, 62), Color.FromArgb(255, 190, 40));
            lblFinalTotal = TotalLbl("FINAL TOTAL:   Rs 0.00", new Point(14, 92), Color.FromArgb(50, 200, 120));
            lblFinalTotal.Font = new Font("Segoe UI", 13, FontStyle.Bold);

            SmallLbl(totCard, "Discount (Rs):", new Point(350, 6));
            txtDiscount = Input(totCard, new Point(350, 24), 110);
            txtDiscount.Text = "0";
            txtDiscount.TextChanged += (s, e) => RecalcTotals();

            SmallLbl(totCard, "Tax (%):", new Point(480, 6));
            txtTax = Input(totCard, new Point(480, 24), 80);
            txtTax.Text = "0";
            txtTax.TextChanged += (s, e) => RecalcTotals();

            totCard.Controls.AddRange(new Control[] { lblSubtotal, lblDiscount, lblTax, lblFinalTotal });
            billCard.Controls.Add(totCard);

            // Payment section
            var payCard = Card(0, 464, 668, 120, "");
            payCard.BackColor = Color.FromArgb(24, 24, 36);

            SmallLbl(payCard, "Payment Method:", new Point(14, 8));
            cmbPayMethod = new ComboBox
            {
                Location = new Point(14, 26),
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPayMethod.Items.AddRange(new[] { "Cash", "Card", "JazzCash", "EasyPaisa", "Bank Transfer" });
            cmbPayMethod.SelectedIndex = 0;
            payCard.Controls.Add(cmbPayMethod);

            SmallLbl(payCard, "Notes:", new Point(210, 8));
            txtPayNotes = Input(payCard, new Point(210, 26), 240);

            btnProcessPayment = MkBtn("✔  PROCESS PAYMENT", Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24), 200, 44);
            btnProcessPayment.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnProcessPayment.Location = new Point(460, 20);
            btnProcessPayment.Enabled = false;
            btnProcessPayment.Click += BtnProcessPayment_Click;
            payCard.Controls.Add(btnProcessPayment);

            billCard.Controls.Add(payCard);
            tab.Controls.Add(billCard);
            return tab;
        }

        private void LoadPendingOrders()
        {
            dgvPendingOrders.DataSource = DB.Query(
                @"SELECT OrderId AS [#], CustomerName AS Customer,
                         OrderType AS Type, TableNumber AS [Table],
                         Status, TotalAmount AS [Rs],
                         datetime(CreatedAt,'localtime') AS Time
                  FROM Orders
                  WHERE Status IN ('BillRequested','ReadyForDelivery','SentToKitchen',
                                   'InPreparation','Pending','Delivered')
                  ORDER BY CreatedAt ASC");
        }

        private void DgvPendingOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPendingOrders.CurrentRow == null) return;
            _selectedOrderId = Convert.ToInt32(dgvPendingOrders.CurrentRow.Cells["#"].Value);
            string cust = dgvPendingOrders.CurrentRow.Cells["Customer"].Value.ToString();
            string type = dgvPendingOrders.CurrentRow.Cells["Type"].Value.ToString();
            string table = dgvPendingOrders.CurrentRow.Cells["Table"].Value.ToString();

            lblOrderInfo.Text = $"Order #{_selectedOrderId}  ·  {cust}  ·  {type}" +
                                (string.IsNullOrWhiteSpace(table) ? "" : $"  ·  Table {table}");

            using (var items = DB.Query(
                @"SELECT ItemName AS Item, Quantity AS Qty,
                         UnitPrice AS [Price Rs],
                         ROUND(Quantity*UnitPrice,0) AS [Subtotal Rs]
                  FROM OrderItems WHERE OrderId=@id",
                DB.P("@id", _selectedOrderId)))
            {
                dgvBillItems.DataSource = items;

                _baseTotal = 0;
                foreach (DataRow r in items.Rows)
                    _baseTotal += Convert.ToDecimal(r["Subtotal Rs"]);
            }

            txtDiscount.Text = "0";
            txtTax.Text = "0";
            RecalcTotals();
            btnProcessPayment.Enabled = true;
        }

        private void RecalcTotals()
        {
            decimal.TryParse(txtDiscount.Text, out decimal disc);
            decimal.TryParse(txtTax.Text, out decimal taxPct);
            decimal afterDisc = _baseTotal - disc;
            if (afterDisc < 0) afterDisc = 0;
            decimal taxAmt = afterDisc * taxPct / 100;
            decimal finalAmt = afterDisc + taxAmt;

            lblSubtotal.Text = $"Subtotal:      Rs {_baseTotal:F2}";
            lblDiscount.Text = $"Discount:      Rs {disc:F2}";
            lblTax.Text = $"Tax ({taxPct}%):   Rs {taxAmt:F2}";
            lblFinalTotal.Text = $"FINAL TOTAL:   Rs {finalAmt:F2}";
        }

        private void BtnProcessPayment_Click(object sender, EventArgs e)
        {
            if (_selectedOrderId < 0) return;
            decimal.TryParse(txtDiscount.Text, out decimal disc);
            decimal.TryParse(txtTax.Text, out decimal taxPct);
            decimal afterDisc = _baseTotal - disc;
            if (afterDisc < 0) afterDisc = 0;
            decimal taxAmt = afterDisc * taxPct / 100;
            decimal finalAmt = afterDisc + taxAmt;
            string method = cmbPayMethod.SelectedItem.ToString();

            var confirm = MessageBox.Show(
                $"Confirm payment of Rs {finalAmt:F2} via {method} for Order #{_selectedOrderId}?",
                "Confirm Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // Update order
            DB.NonQuery(
                @"UPDATE Orders SET
                    Discount=@d, Tax=@t, FinalAmount=@f,
                    PaymentMethod=@pm, PaymentStatus='Paid',
                    Status='Closed', UpdatedAt=CURRENT_TIMESTAMP
                  WHERE OrderId=@id",
                DB.P("@d", (double)disc),
                DB.P("@t", (double)taxPct),
                DB.P("@f", (double)finalAmt),
                DB.P("@pm", method),
                DB.P("@id", _selectedOrderId));

            // Record transaction
            DB.NonQuery(
                "INSERT INTO Transactions(OrderId,AmountPaid,Method,CashierId,Notes) VALUES(@oid,@a,@m,@cb,@n)",
                DB.P("@oid", _selectedOrderId),
                DB.P("@a", (double)finalAmt),
                DB.P("@m", method),
                DB.P("@cb", SessionManager.UserId),
                DB.P("@n", txtPayNotes.Text.Trim()));

            MessageBox.Show($"✅  Payment recorded!\nOrder #{_selectedOrderId} closed.\nAmount: Rs {finalAmt:F2}",
                "Payment Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Show receipt
            ShowReceiptDialog(_selectedOrderId, finalAmt, disc, taxPct, method);

            _selectedOrderId = -1; _baseTotal = 0;
            btnProcessPayment.Enabled = false;
            dgvBillItems.DataSource = null;
            lblOrderInfo.Text = "← Select an order to generate bill";
            lblSubtotal.Text = "Subtotal:      Rs 0.00";
            lblDiscount.Text = "Discount:      Rs 0.00";
            lblTax.Text = "Tax:           Rs 0.00";
            lblFinalTotal.Text = "FINAL TOTAL:   Rs 0.00";
            LoadPendingOrders();
        }

        private void ShowReceiptDialog(int orderId, decimal final, decimal disc, decimal taxPct, string method)
        {
            DataTable orderRow = null;
            DataTable items = null;

            using (var orderData = DB.Query("SELECT * FROM Orders WHERE OrderId=@id", DB.P("@id", orderId)))
            {
                orderRow = orderData;
                using (var itemsData = DB.Query(
                    "SELECT ItemName,Quantity,UnitPrice,ROUND(Quantity*UnitPrice,0) AS Sub FROM OrderItems WHERE OrderId=@id",
                    DB.P("@id", orderId)))
                {
                    items = itemsData;

                    var sb = new StringBuilder();
                    sb.AppendLine("======================================");
                    sb.AppendLine("         STEAKAWAY RESTAURANT        ");
                    sb.AppendLine("======================================");
                    sb.AppendLine($"Receipt #  : TXN-{orderId}-{DateTime.Now:yyyyMMddHHmm}");
                    sb.AppendLine($"Order #    : {orderId}");
                    sb.AppendLine($"Customer   : {orderRow.Rows[0]["CustomerName"]}");
                    sb.AppendLine($"Type       : {orderRow.Rows[0]["OrderType"]}");
                    if (!string.IsNullOrWhiteSpace(orderRow.Rows[0]["TableNumber"].ToString()))
                        sb.AppendLine($"Table      : {orderRow.Rows[0]["TableNumber"]}");
                    sb.AppendLine($"Date       : {DateTime.Now:dd MMM yyyy  HH:mm:ss}");
                    sb.AppendLine($"Cashier    : {SessionManager.FullName}");
                    sb.AppendLine("--------------------------------------");
                    sb.AppendLine($"{"Item",-22}{"Qty",4}{"Rate",8}{"Amount",10}");
                    sb.AppendLine("--------------------------------------");
                    decimal sub = 0;
                    foreach (DataRow r in items.Rows)
                    {
                        decimal s = Convert.ToDecimal(r["Sub"]);
                        sb.AppendLine($"{r["ItemName"].ToString(),-22}{Convert.ToInt32(r["Quantity"]),4}{Convert.ToDecimal(r["UnitPrice"]),8:F0}{s,10:F0}");
                        sub += s;
                    }
                    sb.AppendLine("--------------------------------------");
                    sb.AppendLine($"{"Subtotal",-32}{sub,8:F2}");
                    if (disc > 0) sb.AppendLine($"{"Discount",-32}{-disc,8:F2}");
                    if (taxPct > 0) sb.AppendLine($"{"Tax (" + taxPct + "%)",-32}{(sub - disc) * taxPct / 100,8:F2}");
                    sb.AppendLine($"{"TOTAL PAID",-32}{final,8:F2}");
                    sb.AppendLine($"{"Payment Method",-32}{method,8}");
                    sb.AppendLine("======================================");
                    sb.AppendLine("    Thank you for dining with us!    ");
                    sb.AppendLine("       Please visit again soon!      ");
                    sb.AppendLine("======================================");
                    string receipt = sb.ToString();

                    var dlg = new Form
                    {
                        Text = $"Receipt — Order #{orderId}",
                        Size = new Size(480, 600),
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
                        Font = new Font("Consolas", 9),
                        ReadOnly = true,
                        BorderStyle = BorderStyle.None,
                        Text = receipt
                    };
                    var btnPrint = new Button
                    {
                        Text = "🖨  Print Receipt",
                        Dock = DockStyle.Bottom,
                        Height = 44,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(255, 140, 30),
                        ForeColor = Color.FromArgb(18, 18, 24),
                        Font = new Font("Segoe UI", 11, FontStyle.Bold)
                    };
                    btnPrint.FlatAppearance.BorderSize = 0;
                    btnPrint.Click += (s, ev) =>
                    {
                        var pd = new PrintDocument();
                        pd.PrintPage += (ps, pe) =>
                            pe.Graphics.DrawString(receipt, new Font("Courier New", 8), Brushes.Black,
                                new RectangleF(pe.MarginBounds.Left, pe.MarginBounds.Top, pe.MarginBounds.Width, pe.MarginBounds.Height));
                        var pDlg = new PrintDialog { Document = pd };
                        if (pDlg.ShowDialog() == DialogResult.OK) pd.Print();
                    };
                    dlg.Controls.AddRange(new Control[] { rtb, btnPrint });
                    dlg.ShowDialog(this);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TRANSACTIONS TAB
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildTransactionsTab()
        {
            var tab = DarkTab("🧾 Transactions");

            var bar = new Panel { Location = new Point(12, 12), Size = new Size(1080, 46), BackColor = Color.Transparent };
            SmallLbl(bar, "Date:", new Point(0, 12));
            dtpTxDate = new DateTimePicker
            {
                Location = new Point(40, 8),
                Size = new Size(160, 28),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232)
            };
            var btnLoad = MkBtn("Load", Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24), 90, 30);
            btnLoad.Location = new Point(210, 8);
            btnLoad.Click += (s, e) => LoadTransactions();
            bar.Controls.AddRange(new Control[] { dtpTxDate, btnLoad });
            tab.Controls.Add(bar);

            dgvTransactions = StyledGrid(new Point(12, 66), new Size(1080, 560));
            tab.Controls.Add(dgvTransactions);
            LoadTransactions();
            return tab;
        }

        private void LoadTransactions()
        {
            string date = dtpTxDate.Value.Date.ToString("yyyy-MM-dd");
            dgvTransactions.DataSource = DB.Query(
                @"SELECT t.TxId AS [#], t.OrderId AS [Order#],
                         o.CustomerName AS Customer,
                         o.OrderType AS Type,
                         t.AmountPaid AS [Amount Rs],
                         t.Method AS [Payment],
                         u.FullName AS Cashier,
                         t.Notes,
                         datetime(t.CreatedAt,'localtime') AS DateTime
                  FROM Transactions t
                  JOIN Orders o ON t.OrderId=o.OrderId
                  LEFT JOIN Users u ON t.CashierId=u.UserId
                  WHERE DATE(t.CreatedAt)=DATE(@d)
                  ORDER BY t.CreatedAt DESC",
                DB.P("@d", date));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DAILY SUMMARY TAB
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildDailySummaryTab()
        {
            var tab = DarkTab("📊 Daily Summary");

            // Controls
            var bar = new Panel { Location = new Point(12, 12), Size = new Size(1080, 48), BackColor = Color.Transparent };
            SmallLbl(bar, "Date:", new Point(0, 14));
            dtpSummaryDate = new DateTimePicker
            {
                Location = new Point(40, 10),
                Size = new Size(160, 28),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232)
            };
            var btnLoadSum = MkBtn("Generate Summary", Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24), 180, 32);
            btnLoadSum.Location = new Point(210, 8);
            btnLoadSum.Click += (s, e) => LoadDailySummary();
            bar.Controls.AddRange(new Control[] { dtpSummaryDate, btnLoadSum });
            tab.Controls.Add(bar);

            // KPI cards row
            var row = new Panel { Location = new Point(12, 72), Size = new Size(1080, 110), BackColor = Color.Transparent };
            lblSummaryOrders = SumLbl("Orders: 0", new Point(0, 0));
            lblSummaryTotal = SumLbl("Revenue: Rs 0", new Point(270, 0));
            lblSummaryAvg = SumLbl("Avg Order: Rs 0", new Point(540, 0));
            row.Controls.AddRange(new Control[] { lblSummaryOrders, lblSummaryTotal, lblSummaryAvg });
            tab.Controls.Add(row);

            dgvSummary = StyledGrid(new Point(12, 192), new Size(1080, 436));
            tab.Controls.Add(dgvSummary);

            LoadDailySummary();
            return tab;
        }

        private void LoadDailySummary()
        {
            string date = dtpSummaryDate.Value.Date.ToString("yyyy-MM-dd");
            using (var dt = DB.Query(
                @"SELECT o.OrderId AS [#], o.CustomerName AS Customer,
                 o.OrderType AS Type, o.TableNumber AS [Table#],
                 o.TotalAmount AS [Subtotal Rs],
                 o.Discount AS [Disc Rs], o.Tax AS [Tax%],
                 o.FinalAmount AS [Final Rs],
                 o.PaymentMethod AS Payment,
                 datetime(o.CreatedAt,'localtime') AS Time
          FROM Orders o
          WHERE DATE(o.CreatedAt)=DATE(@d) AND o.Status='Closed'
          ORDER BY o.CreatedAt ASC",
                DB.P("@d", date)))
            {
                dgvSummary.DataSource = dt;

                double total = 0, count = dt.Rows.Count;
                foreach (DataRow r in dt.Rows) total += Convert.ToDouble(r["Final Rs"]);
                double avg = count > 0 ? total / count : 0;

                lblSummaryOrders.Text = $"Orders: {count:F0}";
                lblSummaryTotal.Text = $"Revenue: Rs {total:F2}";
                lblSummaryAvg.Text = $"Avg Order: Rs {avg:F2}";
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Label SumLbl(string text, Point loc) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 140, 30),
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = loc
        };

        private static Label TotalLbl(string text, Point loc, Color color) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = color,
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = loc
        };

        private static TabPage DarkTab(string t)
        {
            var p = new TabPage(t) { BackColor = Color.FromArgb(18, 18, 24), UseVisualStyleBackColor = false };
            return p;
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
            g.RowTemplate.Height = 32;
            return g;
        }

        private static TextBox Input(Control parent, Point p, int w)
        {
            var tb = new TextBox
            {
                Location = p,
                Size = new Size(w, 26),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(tb); return tb;
        }

        private static void SmallLbl(Control parent, string t, Point p) =>
            parent.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(110, 110, 130), AutoSize = true, BackColor = Color.Transparent, Location = new Point(p.X, p.Y + 4) });

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
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }
}