using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;

namespace SteakawayRestaurant.Forms
{
    public class ManagerForm : Form
    {
        private TabControl tabs;

        // Staff tab
        private DataGridView dgvStaff;
        private TextBox txtSUser, txtSPass, txtSName, txtSPhone;
        private ComboBox cmbSRole;
        private CheckBox chkSActive;

        // Menu tab
        private DataGridView dgvMenu;
        private TextBox txtMName, txtMPrice, txtMDesc;
        private ComboBox cmbMCat;
        private CheckBox chkMAvail;
        private TextBox txtCatName;

        // Expense tab
        private DataGridView dgvExp;
        private TextBox txtExpTitle, txtExpAmt, txtExpNotes;
        private ComboBox cmbExpCat;
        private Label lblTotalExpenses;

        // Reports tab
        private DataGridView dgvReport;
        private DateTimePicker dtpFrom, dtpTo;
        private Label lblRevSummary;
        private Button btnExportReport;

        public ManagerForm()
        {
            this.Text = $"Manager Dashboard — {SessionManager.FullName}";
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(24, 24, 36) };
            header.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, header.Width, 4);
            var lblTitle = new Label
            {
                Text = $"🥩 Steakaway  |  Manager Panel  |  {SessionManager.FullName}",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 18)
            };
            var btnLogout = MkBtn("Logout", new Point(header.Width - 120, 12), 100, 32, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += (s, e) => this.Close();
            header.Controls.AddRange(new Control[] { lblTitle, btnLogout });

            // Tabs
            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(150, 38);
            tabs.DrawItem += Tabs_DrawItem;

            tabs.TabPages.Add(BuildOverviewTab());
            tabs.TabPages.Add(BuildStaffTab());
            tabs.TabPages.Add(BuildMenuTab());
            tabs.TabPages.Add(BuildExpenseTab());
            tabs.TabPages.Add(BuildReportsTab());

            this.Controls.Add(tabs);
            this.Controls.Add(header);
        }

        private void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = tabs.TabPages[e.Index];
            var bounds = tabs.GetTabRect(e.Index);
            bool sel = e.Index == tabs.SelectedIndex;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(28, 28, 38) : Color.FromArgb(18, 18, 24)), bounds);
            if (sel) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), bounds.Left, bounds.Top, bounds.Width, 3);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.Text, new Font("Segoe UI", 10, sel ? FontStyle.Bold : FontStyle.Regular),
                new SolidBrush(sel ? Color.FromArgb(255, 140, 30) : Color.FromArgb(130, 130, 150)), bounds, sf);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  OVERVIEW TAB - FIXED with proper data display
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildOverviewTab()
        {
            var tab = DarkTab("📊 Overview");
            int w = 230;

            // Statistics Cards
            var cards = new (string icon, string label, string query, string prefix)[]
            {
                ("📦","Total Orders",  "SELECT COUNT(*) FROM Orders WHERE DATE(CreatedAt)=DATE('now')", ""),
                ("🔥","Active Orders", "SELECT COUNT(*) FROM Orders WHERE Status NOT IN('Closed','Cancelled','Delivered')", ""),
                ("💰","Today's Revenue", "SELECT COALESCE(SUM(FinalAmount),0) FROM Orders WHERE Status='Closed' AND DATE(CreatedAt)=DATE('now')", "Rs "),
                ("🛵","Pending Deliveries", "SELECT COUNT(*) FROM Orders WHERE Status='ReadyForDelivery' OR Status='OutForDelivery'", ""),
                ("👥","Total Staff", "SELECT COUNT(*) FROM Users WHERE Role!='Customer' AND IsActive=1", ""),
                ("💸","Monthly Expenses", "SELECT COALESCE(SUM(Amount),0) FROM Expenses WHERE strftime('%Y-%m', CreatedAt)=strftime('%Y-%m', 'now')", "Rs ")
            };

            int x = 16, y = 16;
            int cardCount = 0;
            foreach (var (icon, label, query, prefix) in cards)
            {
                var val = DB.Scalar(query);
                double dv = Convert.ToDouble(val);
                string display = prefix + (label.Contains("Rs") ? $"{dv:F0}" : dv.ToString("F0"));
                var card = StatCard(icon, display, label, new Point(x, y));
                tab.Controls.Add(card);
                x += w + 12;
                cardCount++;
                if (cardCount % 3 == 0) { x = 16; y += 130; }
            }

            y += 20;

            // Today's Orders Section
            var lblTodayOrders = Lbl("Today's Orders", new Font("Segoe UI", 12, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, y));
            tab.Controls.Add(lblTodayOrders);
            y += 30;

            var dgvTodayOrders = StyledGrid(new Point(16, y), new Size(1040, 200));
            using (var ordersData = DB.Query(@"SELECT o.OrderId AS [#], 
                    o.CustomerName AS Customer, o.OrderType AS Type, 
                    o.TableNumber AS [Table#], o.Status, 
                    o.FinalAmount AS [Amount Rs], o.PaymentMethod AS Payment,
                    datetime(o.CreatedAt,'localtime') AS Time
                    FROM Orders o 
                    WHERE DATE(o.CreatedAt)=DATE('now')
                    ORDER BY o.CreatedAt DESC LIMIT 20"))
            {
                dgvTodayOrders.DataSource = ordersData;
            }
            tab.Controls.Add(dgvTodayOrders);
            y += 210;

            // Recent Expenses
            var lblExpenses = Lbl("Recent Expenses", new Font("Segoe UI", 12, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, y));
            tab.Controls.Add(lblExpenses);
            y += 30;

            var dgvExpenses = StyledGrid(new Point(16, y), new Size(1040, 180));
            using (var expData = DB.Query(@"SELECT Title, Amount AS [Rs], Category, 
                    datetime(CreatedAt,'localtime') AS Date
                    FROM Expenses ORDER BY CreatedAt DESC LIMIT 10"))
            {
                dgvExpenses.DataSource = expData;
            }
            tab.Controls.Add(dgvExpenses);

            return tab;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STAFF TAB (Keep your existing implementation)
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildStaffTab()
        {
            var tab = DarkTab("👥 Staff");

            // Form card
            var form = Card(16, 16, 480, 340, "Add / Edit Staff");

            SmallLbl(form, "Username *", new Point(16, 44)); txtSUser = Input(form, new Point(16, 62), 200);
            SmallLbl(form, "Password *", new Point(230, 44)); var txtSP2 = Input(form, new Point(230, 62), 230, true);
            SmallLbl(form, "Full Name", new Point(16, 104)); txtSName = Input(form, new Point(16, 122), 200);
            SmallLbl(form, "Phone", new Point(230, 104)); txtSPhone = Input(form, new Point(230, 122), 230);
            SmallLbl(form, "Role *", new Point(16, 162));
            cmbSRole = new ComboBox
            {
                Location = new Point(16, 180),
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSRole.Items.AddRange(new[] { "Manager", "Waiter", "Cashier", "XP", "Rider" });
            cmbSRole.SelectedIndex = 1;
            form.Controls.Add(cmbSRole);

            chkSActive = new CheckBox
            {
                Text = "Active",
                Checked = true,
                Location = new Point(220, 184),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 220),
                BackColor = Color.Transparent
            };
            form.Controls.Add(chkSActive);

            var btnAdd = MkBtn("＋ Add Staff", new Point(16, 220), 160, 36, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSUser.Text) || string.IsNullOrWhiteSpace(txtSP2.Text))
                { MessageBox.Show("Username and password required.", "Validation"); return; }
                DB.NonQuery("INSERT INTO Users(Username,Password,Role,FullName,Phone,IsActive) VALUES(@u,@p,@r,@fn,@ph,1)",
                    DB.P("@u", txtSUser.Text.Trim()), DB.P("@p", txtSP2.Text), DB.P("@r", cmbSRole.SelectedItem.ToString()),
                    DB.P("@fn", txtSName.Text.Trim()), DB.P("@ph", txtSPhone.Text.Trim()));
                RefreshStaff(); txtSUser.Clear(); txtSP2.Clear(); txtSName.Clear(); txtSPhone.Clear();
            };
            form.Controls.Add(btnAdd);

            var btnUpdate = MkBtn("✎ Update Selected", new Point(190, 220), 180, 36, Color.FromArgb(40, 60, 90), Color.FromArgb(100, 170, 255));
            btnUpdate.Click += (s, e) =>
            {
                if (dgvStaff.CurrentRow == null) return;
                int id = GetSelectedStaffId();
                DB.NonQuery("UPDATE Users SET FullName=@fn,Phone=@ph,Role=@r,IsActive=@a WHERE UserId=@id",
                    DB.P("@fn", txtSName.Text.Trim()), DB.P("@ph", txtSPhone.Text.Trim()),
                    DB.P("@r", cmbSRole.SelectedItem.ToString()), DB.P("@a", chkSActive.Checked ? 1 : 0), DB.P("@id", id));
                if (!string.IsNullOrWhiteSpace(txtSP2.Text))
                    DB.NonQuery("UPDATE Users SET Password=@p WHERE UserId=@id", DB.P("@p", txtSP2.Text), DB.P("@id", id));
                RefreshStaff();
            };
            form.Controls.Add(btnUpdate);

            tab.Controls.Add(form);

            var lblG = Lbl("Staff List", new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(510, 16));
            dgvStaff = StyledGrid(new Point(510, 42), new Size(560, 290));
            dgvStaff.SelectionChanged += (s, e) => PopulateStaffForm();
            RefreshStaff();

            var btnDel = MkBtn("✕ Deactivate", new Point(510, 345), 140, 32, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnDel.Click += (s, e) =>
            {
                if (dgvStaff.CurrentRow == null) return;
                int id = GetSelectedStaffId();
                if (MessageBox.Show("Deactivate this staff member?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { DB.NonQuery("UPDATE Users SET IsActive=0 WHERE UserId=@id", DB.P("@id", id)); RefreshStaff(); }
            };

            tab.Controls.AddRange(new Control[] { lblG, dgvStaff, btnDel });
            return tab;
        }

        private int GetSelectedStaffId() =>
            Convert.ToInt32(((DataRowView)dgvStaff.CurrentRow.DataBoundItem)["UserId"]);

        private void PopulateStaffForm()
        {
            if (dgvStaff.CurrentRow == null) return;
            var row = (DataRowView)dgvStaff.CurrentRow.DataBoundItem;
            txtSName.Text = row["FullName"].ToString();
            txtSPhone.Text = row["Phone"].ToString();
            var roleStr = row["Role"].ToString();
            if (cmbSRole.Items.Contains(roleStr)) cmbSRole.SelectedItem = roleStr;
            chkSActive.Checked = Convert.ToInt32(row["IsActive"]) == 1;
        }

        private void RefreshStaff()
        {
            using (var staffData = DB.Query("SELECT UserId,Username,Role,FullName,Phone,IsActive FROM Users WHERE Role!='Customer' ORDER BY Role,FullName"))
            {
                dgvStaff.DataSource = staffData;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MENU TAB (Keep your existing implementation)
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildMenuTab()
        {
            var tab = DarkTab("🍽 Menu");

            var catCard = Card(16, 16, 320, 110, "Add Category");
            SmallLbl(catCard, "Category Name", new Point(14, 44)); txtCatName = Input(catCard, new Point(14, 62), 200);
            var btnAddCat = MkBtn("Add Category", new Point(220, 60), 110, 30, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnAddCat.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtCatName.Text)) return;
                DB.NonQuery("INSERT OR IGNORE INTO Categories(Name) VALUES(@n)", DB.P("@n", txtCatName.Text.Trim()));
                RefreshCategoryCombo(); txtCatName.Clear();
            };
            catCard.Controls.Add(btnAddCat);
            tab.Controls.Add(catCard);

            var form = Card(16, 138, 620, 260, "Add / Edit Menu Item");
            SmallLbl(form, "Item Name *", new Point(14, 44)); txtMName = Input(form, new Point(14, 62), 200);
            SmallLbl(form, "Price (Rs) *", new Point(230, 44)); txtMPrice = Input(form, new Point(230, 62), 120);
            SmallLbl(form, "Category *", new Point(14, 104));
            cmbMCat = new ComboBox
            {
                Location = new Point(14, 122),
                Size = new Size(160, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            form.Controls.Add(cmbMCat);
            RefreshCategoryCombo();
            SmallLbl(form, "Description", new Point(190, 104)); txtMDesc = Input(form, new Point(190, 122), 300);
            chkMAvail = new CheckBox
            {
                Text = "Available",
                Checked = true,
                Location = new Point(14, 162),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 220),
                BackColor = Color.Transparent
            };
            form.Controls.Add(chkMAvail);

            var btnAddItem = MkBtn("＋ Add Item", new Point(14, 188), 130, 36, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnAddItem.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtMName.Text) || !decimal.TryParse(txtMPrice.Text, out decimal price) || cmbMCat.SelectedIndex < 0)
                { MessageBox.Show("Name, Price and Category are required.", "Validation"); return; }
                int catId = (int)(long)DB.Scalar("SELECT CategoryId FROM Categories WHERE Name=@n", DB.P("@n", cmbMCat.SelectedItem.ToString()));
                DB.NonQuery("INSERT INTO MenuItems(Name,CategoryId,Price,Description,IsAvailable) VALUES(@n,@c,@p,@d,@a)",
                    DB.P("@n", txtMName.Text.Trim()), DB.P("@c", catId), DB.P("@p", (double)price),
                    DB.P("@d", txtMDesc.Text.Trim()), DB.P("@a", chkMAvail.Checked ? 1 : 0));
                RefreshMenu(); ClearMenuForm();
            };

            var btnUpdItem = MkBtn("✎ Update", new Point(154, 188), 130, 36, Color.FromArgb(40, 60, 90), Color.FromArgb(100, 170, 255));
            btnUpdItem.Click += (s, e) => {
                if (dgvMenu.CurrentRow == null) return;
                int id = Convert.ToInt32(((DataRowView)dgvMenu.CurrentRow.DataBoundItem)["ItemId"]);
                if (!decimal.TryParse(txtMPrice.Text, out decimal price2)) return;
                int catId2 = (int)(long)DB.Scalar("SELECT CategoryId FROM Categories WHERE Name=@n", DB.P("@n", cmbMCat.SelectedItem?.ToString() ?? ""));
                DB.NonQuery("UPDATE MenuItems SET Name=@n,CategoryId=@c,Price=@p,Description=@d,IsAvailable=@a WHERE ItemId=@id",
                    DB.P("@n", txtMName.Text.Trim()), DB.P("@c", catId2), DB.P("@p", (double)price2),
                    DB.P("@d", txtMDesc.Text.Trim()), DB.P("@a", chkMAvail.Checked ? 1 : 0), DB.P("@id", id));
                RefreshMenu();
            };

            var btnDelItem = MkBtn("✕ Remove", new Point(294, 188), 130, 36, Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70));
            btnDelItem.Click += (s, e) => {
                if (dgvMenu.CurrentRow == null) return;
                int id = Convert.ToInt32(((DataRowView)dgvMenu.CurrentRow.DataBoundItem)["ItemId"]);
                if (MessageBox.Show("Remove this item?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { DB.NonQuery("UPDATE MenuItems SET IsAvailable=0 WHERE ItemId=@id", DB.P("@id", id)); RefreshMenu(); }
            };

            form.Controls.AddRange(new Control[] { btnAddItem, btnUpdItem, btnDelItem });
            tab.Controls.Add(form);

            var lblG = Lbl("Menu Items", new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(650, 16));
            dgvMenu = StyledGrid(new Point(650, 42), new Size(410, 350));
            dgvMenu.SelectionChanged += (s, e) => PopulateMenuForm();
            RefreshMenu();
            tab.Controls.AddRange(new Control[] { lblG, dgvMenu });
            return tab;
        }

        private void RefreshCategoryCombo()
        {
            cmbMCat.Items.Clear();
            using (var categories = DB.Query("SELECT Name FROM Categories ORDER BY Name"))
            {
                foreach (DataRow r in categories.Rows)
                    cmbMCat.Items.Add(r["Name"].ToString());
            }
            if (cmbMCat.Items.Count > 0) cmbMCat.SelectedIndex = 0;
        }

        private void RefreshMenu()
        {
            using (var menuData = DB.Query(@"SELECT m.ItemId,m.Name,c.Name AS Category,m.Price,m.Description,m.IsAvailable
                FROM MenuItems m JOIN Categories c ON m.CategoryId=c.CategoryId ORDER BY c.Name,m.Name"))
            {
                dgvMenu.DataSource = menuData;
            }
        }

        private void PopulateMenuForm()
        {
            if (dgvMenu.CurrentRow == null) return;
            var row = (DataRowView)dgvMenu.CurrentRow.DataBoundItem;
            txtMName.Text = row["Name"].ToString();
            txtMPrice.Text = Convert.ToDecimal(row["Price"]).ToString("F2");
            txtMDesc.Text = row["Description"].ToString();
            chkMAvail.Checked = Convert.ToInt32(row["IsAvailable"]) == 1;
            if (cmbMCat.Items.Contains(row["Category"].ToString()))
                cmbMCat.SelectedItem = row["Category"].ToString();
        }

        private void ClearMenuForm()
        { txtMName.Clear(); txtMPrice.Clear(); txtMDesc.Clear(); chkMAvail.Checked = true; }

        // ══════════════════════════════════════════════════════════════════════
        //  EXPENSES TAB - FIXED with totals
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildExpenseTab()
        {
            var tab = DarkTab("💼 Expenses");

            var form = Card(16, 16, 500, 240, "Add Expense");
            SmallLbl(form, "Title *", new Point(14, 44)); txtExpTitle = Input(form, new Point(14, 62), 280);
            SmallLbl(form, "Amount (Rs)*", new Point(310, 44)); txtExpAmt = Input(form, new Point(310, 62), 160);
            SmallLbl(form, "Category", new Point(14, 104));
            cmbExpCat = new ComboBox
            {
                Location = new Point(14, 122),
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbExpCat.Items.AddRange(new[] { "General", "Utilities", "Salaries", "Ingredients", "Maintenance", "Marketing", "Other" });
            cmbExpCat.SelectedIndex = 0;
            form.Controls.Add(cmbExpCat);
            SmallLbl(form, "Notes", new Point(14, 160)); txtExpNotes = Input(form, new Point(14, 178), 460);

            var btnAddExp = MkBtn("＋ Add Expense", new Point(14, 205), 160, 34, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnAddExp.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtExpTitle.Text) || !decimal.TryParse(txtExpAmt.Text, out decimal amt))
                { MessageBox.Show("Title and valid amount required.", "Validation"); return; }
                DB.NonQuery("INSERT INTO Expenses(Title,Amount,Category,Notes,AddedBy) VALUES(@t,@a,@c,@n,@by)",
                    DB.P("@t", txtExpTitle.Text.Trim()), DB.P("@a", (double)amt),
                    DB.P("@c", cmbExpCat.SelectedItem.ToString()), DB.P("@n", txtExpNotes.Text.Trim()),
                    DB.P("@by", SessionManager.UserId));
                txtExpTitle.Clear(); txtExpAmt.Clear(); txtExpNotes.Clear(); RefreshExpenses();
            };
            form.Controls.Add(btnAddExp);
            tab.Controls.Add(form);

            // Total Expenses Summary
            var totalExp = DB.Scalar("SELECT COALESCE(SUM(Amount),0) FROM Expenses WHERE strftime('%Y-%m', CreatedAt)=strftime('%Y-%m', 'now')");
            lblTotalExpenses = new Label
            {
                Text = $"💰 Monthly Total Expenses: Rs {Convert.ToDouble(totalExp):F2}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 270)
            };
            tab.Controls.Add(lblTotalExpenses);

            var lblG = Lbl("Expenses History", new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, 300));
            dgvExp = StyledGrid(new Point(16, 326), new Size(1040, 310));
            RefreshExpenses();
            tab.Controls.AddRange(new Control[] { lblG, dgvExp });
            return tab;
        }

        private void RefreshExpenses()
        {
            using (var expData = DB.Query("SELECT ExpenseId AS [#],Title,Amount AS [Rs],Category,Notes,datetime(CreatedAt,'localtime') AS Date FROM Expenses ORDER BY CreatedAt DESC"))
            {
                dgvExp.DataSource = expData;
                double total = 0;
                foreach (DataRow r in expData.Rows)
                    total += Convert.ToDouble(r["Rs"]);
                lblTotalExpenses.Text = $"💰 Monthly Total Expenses: Rs {total:F2}";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  REPORTS TAB - COMPLETE with full history
        // ══════════════════════════════════════════════════════════════════════
        private TabPage BuildReportsTab()
        {
            var tab = DarkTab("📋 Reports");

            var bar = Card(16, 16, 1040, 80, "Filter Reports");
            var lblFrom = Lbl("From Date:", new Font("Segoe UI", 9), Color.FromArgb(130, 130, 150), new Point(14, 30));
            dtpFrom = new DateTimePicker { Location = new Point(85, 26), Size = new Size(160, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(-30) };
            var lblTo = Lbl("To Date:", new Font("Segoe UI", 9), Color.FromArgb(130, 130, 150), new Point(260, 30));
            dtpTo = new DateTimePicker { Location = new Point(315, 26), Size = new Size(160, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            var btnLoad = MkBtn("Load Report", new Point(495, 22), 130, 34, Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnLoad.Click += (s, e) => LoadReport();

            btnExportReport = MkBtn("📎 Export to CSV", new Point(640, 22), 140, 34, Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180));
            btnExportReport.Click += (s, e) => ExportReportToCSV();

            lblRevSummary = new Label
            {
                Location = new Point(800, 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 200, 120),
                BackColor = Color.Transparent
            };
            bar.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnLoad, btnExportReport, lblRevSummary });
            tab.Controls.Add(bar);

            dgvReport = StyledGrid(new Point(16, 110), new Size(1040, 530));
            tab.Controls.Add(dgvReport);

            // Load initial report
            LoadReport();
            return tab;
        }

        private void LoadReport()
        {
            string from = dtpFrom.Value.Date.ToString("yyyy-MM-dd");
            string to = dtpTo.Value.Date.ToString("yyyy-MM-dd");

            using (var reportData = DB.Query(@"SELECT o.OrderId AS [#], 
                    o.CustomerName AS Customer, o.OrderType AS Type, 
                    o.TableNumber AS [Table#], o.Status, 
                    o.TotalAmount AS [Subtotal Rs], o.Discount AS [Disc Rs],
                    o.Tax AS [Tax%], o.FinalAmount AS [Final Rs],
                    o.PaymentMethod AS Payment, o.Rating AS Stars,
                    datetime(o.CreatedAt,'localtime') AS [Order Date],
                    CASE WHEN d.Status IS NOT NULL THEN d.Status ELSE 'N/A' END AS [Delivery Status],
                    r.Name AS [Rider Name]
                FROM Orders o
                LEFT JOIN Deliveries d ON o.OrderId = d.OrderId
                LEFT JOIN Riders r ON d.RiderId = r.RiderId
                WHERE DATE(o.CreatedAt) >= DATE(@f) AND DATE(o.CreatedAt) <= DATE(@t)
                ORDER BY o.CreatedAt DESC",
                DB.P("@f", from), DB.P("@t", to)))
            {
                dgvReport.DataSource = reportData;

                double revenue = 0, discount = 0, tax = 0;
                int completed = 0, cancelled = 0, pending = 0;

                foreach (DataRow r in reportData.Rows)
                {
                    revenue += Convert.ToDouble(r["Final Rs"]);
                    discount += Convert.ToDouble(r["Disc Rs"]);
                    tax += Convert.ToDouble(r["Tax%"]);

                    string status = r["Status"].ToString();
                    if (status == "Closed" || status == "Delivered")
                        completed++;
                    else if (status == "Cancelled")
                        cancelled++;
                    else
                        pending++;
                }

                lblRevSummary.Text = $"📊 Orders: {reportData.Rows.Count} | ✅ Completed: {completed} | ❌ Cancelled: {cancelled} | ⏳ Pending: {pending} | 💰 Revenue: Rs {revenue:F2} | 💸 Discounts: Rs {discount:F2}";
            }
        }

        private void ExportReportToCSV()
        {
            if (dgvReport.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV files (*.csv)|*.csv";
                saveDialog.FileName = $"Steakaway_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                saveDialog.Title = "Export Report to CSV";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new System.IO.StreamWriter(saveDialog.FileName))
                        {
                            // Write headers
                            for (int i = 0; i < dgvReport.Columns.Count; i++)
                            {
                                writer.Write(dgvReport.Columns[i].HeaderText);
                                if (i < dgvReport.Columns.Count - 1)
                                    writer.Write(",");
                            }
                            writer.WriteLine();

                            // Write data
                            foreach (DataGridViewRow row in dgvReport.Rows)
                            {
                                if (row.IsNewRow) continue;
                                for (int i = 0; i < dgvReport.Columns.Count; i++)
                                {
                                    string value = row.Cells[i].Value?.ToString() ?? "";
                                    // Escape quotes and handle commas
                                    if (value.Contains(",") || value.Contains("\""))
                                    {
                                        value = "\"" + value.Replace("\"", "\"\"") + "\"";
                                    }
                                    writer.Write(value);
                                    if (i < dgvReport.Columns.Count - 1)
                                        writer.Write(",");
                                }
                                writer.WriteLine();
                            }
                        }
                        MessageBox.Show($"Report exported successfully!\n{saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static TabPage DarkTab(string title)
        {
            var t = new TabPage(title) { BackColor = Color.FromArgb(18, 18, 24), UseVisualStyleBackColor = false };
            return t;
        }

        private static Panel Card(int x, int y, int w, int h, string title)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.FromArgb(28, 28, 38) };
            p.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(50, 50, 68), 1)) e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
            if (!string.IsNullOrEmpty(title))
                p.Controls.Add(new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 140, 30),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Location = new Point(14, 10)
                });
            return p;
        }

        private static Panel StatCard(string icon, string val, string label, Point loc)
        {
            var c = new Panel { Location = loc, Size = new Size(218, 105), BackColor = Color.FromArgb(28, 28, 38) };
            c.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, c.Width, 3);
                using (var pen = new Pen(Color.FromArgb(50, 50, 68), 1)) e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
                e.Graphics.DrawString(icon, new Font("Segoe UI", 22), new SolidBrush(Color.FromArgb(255, 140, 30)), new PointF(12, 14));
                e.Graphics.DrawString(val, new Font("Segoe UI", 16, FontStyle.Bold), new SolidBrush(Color.FromArgb(240, 240, 248)), new PointF(12, 52));
                e.Graphics.DrawString(label, new Font("Segoe UI", 8), new SolidBrush(Color.FromArgb(110, 110, 130)), new PointF(12, 85));
            };
            return c;
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

        private static TextBox Input(Control parent, Point p, int w, bool pwd = false)
        {
            var tb = new TextBox
            {
                Location = p,
                Size = new Size(w, 28),
                BackColor = Color.FromArgb(35, 35, 48),
                ForeColor = Color.FromArgb(220, 220, 232),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = pwd ? '●' : '\0'
            };
            parent.Controls.Add(tb); return tb;
        }

        private static void SmallLbl(Control parent, string t, Point p) =>
            parent.Controls.Add(new Label
            {
                Text = t,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(p.X, p.Y + 4)
            });

        private static Label Lbl(string t, Font f, Color c, Point p) =>
            new Label { Text = t, Font = f, ForeColor = c, AutoSize = true, BackColor = Color.Transparent, Location = p };

        private static Button MkBtn(string t, Point p, int w, int h, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = t,
                Location = p,
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