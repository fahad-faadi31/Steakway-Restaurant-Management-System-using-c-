using SteakawayRestaurant.Database;
using SteakawayRestaurant.Helpers;
using SteakawayRestaurant.Models;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SteakawayRestaurant.Forms
{
    public class LoginForm : Form
    {
        private Panel pnlLeft, pnlRight;
        private TextBox txtUsername, txtPassword;
        private Label lblError;
        private Button btnLogin, btnRegister, btnForgot;
        private CheckBox chkShow;

        public LoginForm()
        {
            this.Text = "Steakaway Restaurant — Login";
            this.Size = new Size(860, 540);
            this.MinimumSize = new Size(860, 540);
            this.MaximumSize = new Size(860, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
        }

        private void BuildUI()
        {
            // Left branding panel
            pnlLeft = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(380, 540),
                BackColor = Color.FromArgb(24, 24, 36)
            };
            pnlLeft.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 30)), 0, 0, pnlLeft.Width, 5);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, 255, 140, 30)), 140, 80, 100, 100);
                using (var f = new Font("Segoe UI", 36))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("🥩", f, Brushes.White, new RectangleF(140, 80, 100, 100), sf);
                }
            };

            var lblBrand = new Label
            {
                Text = "STEAKAWAY",
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 30),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(75, 200)
            };
            var lblSub = new Label
            {
                Text = "Restaurant Management System",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 120, 140),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(68, 240)
            };

            // Default credentials hint
            var pnlHint = new Panel { Location = new Point(20, 310), Size = new Size(340, 180), BackColor = Color.FromArgb(30, 30, 44) };
            pnlHint.Paint += (s, e) => {
                using (var p = new Pen(Color.FromArgb(50, 50, 68), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlHint.Width - 1, pnlHint.Height - 1);
                }
            };
            var lblHintTitle = new Label { Text = "Default Credentials", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 30), AutoSize = true, BackColor = Color.Transparent, Location = new Point(12, 10) };
            string hints = "Manager  : admin / Admin@123\nWaiter   : waiter1 / Pass@123\nXP       : xp1 / Pass@123\nCashier  : cashier1 / Pass@123\nRider    : rider / Rider@123\nCustomer : Register below";
            var lblHints = new Label { Text = hints, Font = new Font("Consolas", 8), ForeColor = Color.FromArgb(160, 160, 180), AutoSize = true, BackColor = Color.Transparent, Location = new Point(12, 30) };
            pnlHint.Controls.AddRange(new Control[] { lblHintTitle, lblHints });

            pnlLeft.Controls.AddRange(new Control[] { lblBrand, lblSub, pnlHint });

            // Right login panel
            pnlRight = new Panel
            {
                Location = new Point(380, 0),
                Size = new Size(480, 540),
                BackColor = Color.FromArgb(18, 18, 24)
            };

            var lblTitle = new Label
            {
                Text = "Sign In",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 248),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(60, 60)
            };
            var lblWelcome = new Label
            {
                Text = "Welcome back! Please enter your details.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(60, 100)
            };

            // Username
            var lblU = MkLabel("Username", new Point(60, 140));
            txtUsername = MkInput(new Point(60, 162), 360);

            // Password
            var lblP = MkLabel("Password", new Point(60, 210));
            txtPassword = MkInput(new Point(60, 232), 360, true);

            chkShow = new CheckBox
            {
                Text = "Show password",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(60, 278),
                Checked = false
            };
            chkShow.CheckedChanged += (s, e) => txtPassword.PasswordChar = chkShow.Checked ? '\0' : '●';

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(230, 70, 70),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(60, 304)
            };

            btnLogin = MkButton("SIGN IN", new Point(60, 330), 360, 44,
                Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnLogin.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnLogin.Click += BtnLogin_Click;

            var sep = new Label
            {
                Text = "──────────────  or  ──────────────",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(60, 60, 80),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(60, 390)
            };

            btnRegister = MkButton("Create Customer Account", new Point(60, 415), 360, 38,
                Color.FromArgb(30, 30, 44), Color.FromArgb(240, 240, 248));
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 68);
            btnRegister.FlatAppearance.BorderSize = 1;
            btnRegister.Click += (s, e) => { new RegisterForm().ShowDialog(this); };

            btnForgot = new Button
            {
                Text = "Forgot password?",
                Font = new Font("Segoe UI", 8, FontStyle.Underline),
                ForeColor = Color.FromArgb(255, 140, 30),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Location = new Point(60, 462),
                Cursor = Cursors.Hand
            };
            btnForgot.FlatAppearance.BorderSize = 0;
            btnForgot.Click += (s, e) => { new ForgotPasswordForm().ShowDialog(this); };

            pnlRight.Controls.AddRange(new Control[]
            { lblTitle, lblWelcome, lblU, txtUsername, lblP, txtPassword,
              chkShow, lblError, btnLogin, sep, btnRegister, btnForgot });

            this.Controls.AddRange(new Control[] { pnlLeft, pnlRight });
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Please enter username and password.";
                return;
            }

            using (var queryResult = DB.Query(
                "SELECT UserId, Username, FullName, Role FROM Users WHERE Username = @u AND Password = @p AND IsActive = 1",
                DB.P("@u", user),
                DB.P("@p", pass)))
            {
                if (queryResult.Rows.Count == 0)
                {
                    lblError.Text = "Invalid credentials. Please try again.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                var row = queryResult.Rows[0];
                var u = new User
                {
                    UserId = Convert.ToInt32(row["UserId"]),
                    Username = row["Username"].ToString(),
                    FullName = row["FullName"].ToString(),
                    Role = row["Role"].ToString()
                };

                SessionManager.Login(u);

                Form dash = null;

                switch (u.Role)
                {
                    case "Manager":
                        dash = new ManagerForm();
                        break;
                    case "Waiter":
                        dash = new OrderForm();
                        break;
                    case "Cashier":
                        dash = new CashierForm();
                        break;
                    case "XP":
                        dash = new KitchenForm();
                        break;
                    case "Customer":
                        dash = new MainDashboard();
                        break;
                    case "Rider":
                        dash = new RiderForm();
                        break;
                    default:
                        dash = null;
                        break;
                }

                if (dash == null)
                {
                    lblError.Text = "Role not configured. Contact manager.";
                    SessionManager.Logout();
                    return;
                }

                this.Hide();
                dash.FormClosed += (s2, e2) =>
                {
                    SessionManager.Logout();
                    txtPassword.Clear();
                    this.Show();
                };
                dash.Show();
            }
        }

        private static Label MkLabel(string text, Point loc) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(120, 120, 140),
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = loc
        };

        private static TextBox MkInput(Point loc, int w, bool pwd = false) => new TextBox
        {
            Location = loc,
            Size = new Size(w, 34),
            BackColor = Color.FromArgb(28, 28, 38),
            ForeColor = Color.FromArgb(240, 240, 248),
            Font = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = pwd ? '●' : '\0'
        };

        private static Button MkButton(string text, Point loc, int w, int h, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}