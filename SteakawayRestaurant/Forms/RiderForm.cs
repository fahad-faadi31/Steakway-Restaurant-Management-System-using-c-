using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;

namespace SteakawayRestaurant.Forms
{
    public class RiderForm : Form
    {
        private DataGridView dgvReadyOrders, dgvRiders, dgvActiveDeliveries;
        private Label lblRiderStatus;
        private Timer refreshTimer;
        private int _selectedOrderId = -1;
        private int _selectedRiderId = -1;

        public RiderForm()
        {
            this.Text = $"Rider In-Charge Panel — {SessionManager.FullName}";
            this.Size = new Size(1200, 720);
            this.MinimumSize = new Size(1000, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
            LoadData();

            refreshTimer = new Timer { Interval = 15000 };
            refreshTimer.Tick += (s, e) => LoadData();
            refreshTimer.Start();
        }

        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(24, 24, 36) };
            hdr.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, hdr.Width, 4);
            hdr.Controls.Add(Lbl($"🛵  Steakaway  |  Rider In-Charge Panel  |  {SessionManager.FullName}",
                new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(255, 140, 30), new Point(16, 16)));

            var btnLogout = MkBtn("Logout", Color.FromArgb(80, 25, 25), Color.FromArgb(230, 70, 70), 100, 32);
            btnLogout.Location = new Point(hdr.Width - 120, 10);
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += (s, e) => { refreshTimer.Stop(); this.Close(); };
            hdr.Controls.Add(btnLogout);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(22, 22, 32) };
            var btnRefresh = MkBtn("↻ Refresh", Color.FromArgb(40, 40, 56), Color.FromArgb(160, 160, 180), 120, 36);
            var btnAssign = MkBtn("🎯 Assign to Rider", Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24), 160, 36);
            var btnMarkPicked = MkBtn("📦 Mark Picked Up", Color.FromArgb(50, 40, 10), Color.FromArgb(255, 190, 40), 160, 36);
            var btnMarkDelivered = MkBtn("✅ Mark Delivered", Color.FromArgb(20, 50, 30), Color.FromArgb(50, 200, 120), 160, 36);

            btnRefresh.Location = new Point(8, 8);
            btnAssign.Location = new Point(138, 8);
            btnMarkPicked.Location = new Point(308, 8);
            btnMarkDelivered.Location = new Point(478, 8);

            btnRefresh.Click += (s, e) => LoadData();
            btnAssign.Click += BtnAssign_Click;
            btnMarkPicked.Click += BtnMarkPicked_Click;
            btnMarkDelivered.Click += BtnMarkDelivered_Click;

            toolbar.Controls.AddRange(new Control[] { btnRefresh, btnAssign, btnMarkPicked, btnMarkDelivered });

            // Main layout
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 24), Padding = new Padding(12) };

            // Ready Orders Panel
            var ordersCard = Card(12, 12, 450, 320, "🔄 Ready for Delivery");
            dgvReadyOrders = StyledGrid(new Point(0, 38), new Size(448, 280));
            dgvReadyOrders.SelectionChanged += (s, e) => {
                if (dgvReadyOrders.CurrentRow != null)
                    _selectedOrderId = Convert.ToInt32(dgvReadyOrders.CurrentRow.Cells["OrderId"].Value);
            };
            ordersCard.Controls.Add(dgvReadyOrders);
            body.Controls.Add(ordersCard);

            // Riders Panel
            var ridersCard = Card(470, 12, 450, 320, "👥 Available Riders");
            dgvRiders = StyledGrid(new Point(0, 38), new Size(448, 280));
            dgvRiders.SelectionChanged += (s, e) => {
                if (dgvRiders.CurrentRow != null)
                {
                    _selectedRiderId = Convert.ToInt32(dgvRiders.CurrentRow.Cells["RiderId"].Value);
                    bool isBusy = Convert.ToBoolean(dgvRiders.CurrentRow.Cells["IsBusy"].Value);
                    if (isBusy)
                        lblRiderStatus.Text = "⚠️ Selected rider is BUSY! Choose another rider.";
                    else
                        lblRiderStatus.Text = "✅ Rider available for delivery";
                }
            };
            ridersCard.Controls.Add(dgvRiders);

            lblRiderStatus = new Label
            {
                Text = "Select a rider",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 190, 40),
                AutoSize = false,
                Size = new Size(448, 24),
                Location = new Point(0, 322),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            ridersCard.Controls.Add(lblRiderStatus);
            body.Controls.Add(ridersCard);

            // Active Deliveries Panel
            var deliveriesCard = Card(12, 345, 908, 290, "📋 Active Deliveries");
            dgvActiveDeliveries = StyledGrid(new Point(0, 38), new Size(906, 250));
            deliveriesCard.Controls.Add(dgvActiveDeliveries);
            body.Controls.Add(deliveriesCard);

            this.Controls.Add(body);
            this.Controls.Add(toolbar);
            this.Controls.Add(hdr);
        }

        private void LoadData()
        {
            // Load ready orders (orders that are ready for delivery and not yet assigned)
            using (var orders = DB.Query(
                @"SELECT OrderId, CustomerName, Address, Phone, 
                         OrderType, FinalAmount AS [Amount Rs],
                         datetime(CreatedAt,'localtime') AS OrderTime
                  FROM Orders 
                  WHERE Status = 'ReadyForDelivery' 
                    AND OrderId NOT IN (SELECT OrderId FROM Deliveries WHERE Status IN ('Pending', 'Assigned', 'PickedUp'))
                  ORDER BY CreatedAt ASC"))
            {
                dgvReadyOrders.DataSource = orders;
            }

            // Load riders with their status
            using (var riders = DB.Query(
                @"SELECT RiderId, Name, Phone, VehicleType, 
                         CASE WHEN IsBusy = 1 THEN 'Busy' ELSE 'Available' END AS Status,
                         IsBusy
                  FROM Riders 
                  WHERE IsActive = 1
                  ORDER BY IsBusy ASC, Name ASC"))
            {
                dgvRiders.DataSource = riders;

                // Color busy rows
                foreach (DataGridViewRow row in dgvRiders.Rows)
                {
                    if (row.Cells["IsBusy"].Value != null && Convert.ToBoolean(row.Cells["IsBusy"].Value))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(80, 25, 25);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(230, 70, 70);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(20, 50, 30);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(50, 200, 120);
                    }
                }
            }

            // Load active deliveries - FIXED: Added RiderName and proper column names
            using (var deliveries = DB.Query(
                @"SELECT d.DeliveryId, d.OrderId, o.CustomerName, o.Address,
                         r.Name AS RiderName, d.Status,
                         CASE d.Status
                             WHEN 'Pending' THEN '⏳ Awaiting Assignment'
                             WHEN 'Assigned' THEN '🛵 Rider Assigned'
                             WHEN 'PickedUp' THEN '📦 On Delivery'
                             WHEN 'Delivered' THEN '✅ Completed'
                         END AS StatusText,
                         datetime(d.AssignedAt,'localtime') AS AssignedTime
                  FROM Deliveries d
                  JOIN Orders o ON d.OrderId = o.OrderId
                  LEFT JOIN Riders r ON d.RiderId = r.RiderId
                  WHERE d.Status IN ('Pending', 'Assigned', 'PickedUp')
                  ORDER BY d.AssignedAt DESC"))
            {
                dgvActiveDeliveries.DataSource = deliveries;

                // Store RiderId in a separate invisible column if needed
                if (!dgvActiveDeliveries.Columns.Contains("RiderId"))
                {
                    // Add a hidden column for RiderId by re-querying with RiderId
                    using (var deliveriesWithRiderId = DB.Query(
                        @"SELECT d.DeliveryId, d.OrderId, d.RiderId, o.CustomerName, o.Address,
                                 r.Name AS RiderName, d.Status
                          FROM Deliveries d
                          JOIN Orders o ON d.OrderId = o.OrderId
                          LEFT JOIN Riders r ON d.RiderId = r.RiderId
                          WHERE d.Status IN ('Pending', 'Assigned', 'PickedUp')"))
                    {
                        // Store RiderIds in a separate list or as tag
                        foreach (DataGridViewRow row in dgvActiveDeliveries.Rows)
                        {
                            if (row.Index < deliveriesWithRiderId.Rows.Count)
                            {
                                row.Tag = deliveriesWithRiderId.Rows[row.Index]["RiderId"];
                            }
                        }
                    }
                }
            }
        }

        private void BtnAssign_Click(object sender, EventArgs e)
        {
            if (_selectedOrderId == -1)
            {
                MessageBox.Show("Please select an order first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_selectedRiderId == -1)
            {
                MessageBox.Show("Please select a rider first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if rider is busy
            var riderCheck = DB.Query("SELECT IsBusy FROM Riders WHERE RiderId = @id", DB.P("@id", _selectedRiderId));
            if (riderCheck.Rows.Count > 0 && Convert.ToBoolean(riderCheck.Rows[0]["IsBusy"]))
            {
                MessageBox.Show("This rider is currently busy! Please select another rider.", "Rider Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Assign delivery
            DB.NonQuery(@"INSERT INTO Deliveries(OrderId, RiderId, AssignedBy, Status, AssignedAt) 
                          VALUES(@oid, @rid, @aid, 'Assigned', CURRENT_TIMESTAMP)",
                DB.P("@oid", _selectedOrderId),
                DB.P("@rid", _selectedRiderId),
                DB.P("@aid", SessionManager.UserId));

            // Update rider status to busy
            DB.NonQuery("UPDATE Riders SET IsBusy = 1, CurrentOrderId = @oid WHERE RiderId = @rid",
                DB.P("@oid", _selectedOrderId),
                DB.P("@rid", _selectedRiderId));

            // Update order status
            DB.NonQuery("UPDATE Orders SET Status = 'OutForDelivery', RiderId = @rid WHERE OrderId = @id",
                DB.P("@rid", _selectedRiderId),
                DB.P("@id", _selectedOrderId));

            MessageBox.Show($"✅ Order #{_selectedOrderId} assigned to rider!", "Assignment Successful",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadData();
            _selectedOrderId = -1;
            _selectedRiderId = -1;
        }

        private void BtnMarkPicked_Click(object sender, EventArgs e)
        {
            if (dgvActiveDeliveries.CurrentRow == null)
            {
                MessageBox.Show("Please select a delivery first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int deliveryId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Cells["DeliveryId"].Value);
            int orderId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Cells["OrderId"].Value);

            // Get RiderId from the row's Tag or query it
            int riderId = 0;
            if (dgvActiveDeliveries.CurrentRow.Tag != null)
            {
                riderId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Tag);
            }
            else
            {
                // Query to get RiderId
                var riderData = DB.Query("SELECT RiderId FROM Deliveries WHERE DeliveryId = @id", DB.P("@id", deliveryId));
                if (riderData.Rows.Count > 0)
                    riderId = Convert.ToInt32(riderData.Rows[0]["RiderId"]);
            }

            DB.NonQuery("UPDATE Deliveries SET Status = 'PickedUp', PickedUpAt = CURRENT_TIMESTAMP WHERE DeliveryId = @id",
                DB.P("@id", deliveryId));
            DB.NonQuery("UPDATE Orders SET Status = 'OutForDelivery' WHERE OrderId = @id", DB.P("@id", orderId));

            MessageBox.Show("📦 Order marked as picked up!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        private void BtnMarkDelivered_Click(object sender, EventArgs e)
        {
            if (dgvActiveDeliveries.CurrentRow == null)
            {
                MessageBox.Show("Please select a delivery first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int deliveryId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Cells["DeliveryId"].Value);
            int orderId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Cells["OrderId"].Value);

            // Get RiderId from the row's Tag or query it
            int riderId = 0;
            if (dgvActiveDeliveries.CurrentRow.Tag != null)
            {
                riderId = Convert.ToInt32(dgvActiveDeliveries.CurrentRow.Tag);
            }
            else
            {
                // Query to get RiderId
                var riderData = DB.Query("SELECT RiderId FROM Deliveries WHERE DeliveryId = @id", DB.P("@id", deliveryId));
                if (riderData.Rows.Count > 0)
                    riderId = Convert.ToInt32(riderData.Rows[0]["RiderId"]);
            }

            // Update delivery status
            DB.NonQuery("UPDATE Deliveries SET Status = 'Delivered', DeliveredAt = CURRENT_TIMESTAMP WHERE DeliveryId = @id",
                DB.P("@id", deliveryId));

            // Update order status
            DB.NonQuery("UPDATE Orders SET Status = 'Delivered' WHERE OrderId = @id", DB.P("@id", orderId));

            // Update rider status to available (not busy)
            DB.NonQuery("UPDATE Riders SET IsBusy = 0, CurrentOrderId = 0 WHERE RiderId = @id", DB.P("@id", riderId));

            MessageBox.Show("✅ Order delivered successfully!", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            base.OnFormClosed(e);
        }

        // Helper methods
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
    }
}