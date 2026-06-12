using SteakawayRestaurant.Database;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace SteakawayRestaurant.Forms
{
    // ══════════════════════════════════════════════════════════════════════════
    //  REGISTER FORM
    // ══════════════════════════════════════════════════════════════════════════
    public class RegisterForm : Form
    {
        private TextBox txtName, txtEmail, txtPhone, txtUser, txtPass, txtConfirm, txtAddress;
        private Label lblError;
        private Button btnRegister;

        public RegisterForm()
        {
            this.Text = "Create Account";
            this.Size = new Size(500, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
        }

        private void BuildUI()
        {
            // Top orange bar
            var bar = new Panel { Location = new Point(0, 0), Size = new Size(500, 5), BackColor = Color.FromArgb(255, 140, 30) };

            var lblTitle = Lbl("Create Customer Account", new Font("Segoe UI", 16, FontStyle.Bold),
                Color.FromArgb(240, 240, 248), new Point(30, 22));

            int y = 65;
            txtName = AddField("Full Name *", ref y, false);
            txtEmail = AddField("Email Address *", ref y, false);
            txtPhone = AddField("Phone Number *", ref y, false);
            txtUser = AddField("Username *", ref y, false);
            txtPass = AddField("Password *", ref y, true);
            txtConfirm = AddField("Confirm Password *", ref y, true);
            txtAddress = AddField("Default Address", ref y, false);

            lblError = new Label
            {
                Location = new Point(30, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(230, 70, 70),
                BackColor = Color.Transparent
            };

            btnRegister = MkBtn("CREATE ACCOUNT", new Point(30, y + 22), 420, 42,
                Color.FromArgb(255, 140, 30), Color.FromArgb(18, 18, 24));
            btnRegister.Click += BtnRegister_Click;

            this.Controls.AddRange(new Control[] { bar, lblTitle, btnRegister, lblError });
        }

        private void BtnRegister_Click(object s, EventArgs e)
        {
            lblError.Text = "";
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) || string.IsNullOrWhiteSpace(txtUser.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Text))
            { lblError.Text = "⚠  All starred fields are required."; return; }

            if (txtPass.Text != txtConfirm.Text)
            { lblError.Text = "⚠  Passwords do not match."; return; }

            if (txtPass.Text.Length < 6)
            { lblError.Text = "⚠  Password must be at least 6 characters."; return; }

            // Check username unique
            var cnt = DB.Scalar("SELECT COUNT(*) FROM Users WHERE Username=@u", DB.P("@u", txtUser.Text.Trim()));
            if (Convert.ToInt64(cnt) > 0)
            { lblError.Text = "⚠  Username already taken."; return; }

            long uid = DB.Insert(
                @"INSERT INTO Users(Username,Password,Role,FullName,Phone,Email,Address)
                  VALUES(@u,@p,'Customer',@fn,@ph,@em,@addr)",
                DB.P("@u", txtUser.Text.Trim()),
                DB.P("@p", txtPass.Text),
                DB.P("@fn", txtName.Text.Trim()),
                DB.P("@ph", txtPhone.Text.Trim()),
                DB.P("@em", txtEmail.Text.Trim()),
                DB.P("@addr", txtAddress.Text.Trim()));

            if (!string.IsNullOrWhiteSpace(txtAddress.Text))
                DB.NonQuery("INSERT INTO CustomerAddresses(CustomerId,Label,Address,IsDefault) VALUES(@c,'Home',@a,1)",
                    DB.P("@c", uid), DB.P("@a", txtAddress.Text.Trim()));

            MessageBox.Show("Account created! You can now log in.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private TextBox AddField(string label, ref int y, bool pwd)
        {
            this.Controls.Add(Lbl(label, new Font("Segoe UI", 8), Color.FromArgb(110, 110, 130), new Point(30, y)));
            y += 18;
            var tb = new TextBox
            {
                Location = new Point(30, y),
                Size = new Size(420, 30),
                BackColor = Color.FromArgb(28, 28, 38),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = pwd ? '●' : '\0'
            };
            this.Controls.Add(tb);
            y += 42;
            return tb;
        }

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
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  FORGOT PASSWORD FORM
    // ══════════════════════════════════════════════════════════════════════════
    public class ForgotPasswordForm : Form
    {
        private TextBox txtEmail, txtUser, txtNew, txtConfirm;
        private Label lblError, lblSuccess;
        private Button btnReset;

        public ForgotPasswordForm()
        {
            this.Text = "Reset Password";
            this.Size = new Size(440, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 18, 24);
            BuildUI();
        }

        private void BuildUI()
        {
            var bar = new Panel { Location = new Point(0, 0), Size = new Size(440, 5), BackColor = Color.FromArgb(255, 140, 30) };
            var lbl = new Label
            {
                Text = "Reset Your Password",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 248),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(30, 22)
            };
            var info = new Label
            {
                Text = "Enter your username and registered email to reset your password.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize = false,
                Size = new Size(380, 30),
                BackColor = Color.Transparent,
                Location = new Point(30, 58)
            };

            int y = 96;
            txtUser = AddField("Username", ref y);
            txtEmail = AddField("Registered Email", ref y);
            txtNew = AddField("New Password", ref y, true);
            txtConfirm = AddField("Confirm Password", ref y, true);

            lblError = new Label { Location = new Point(30, y), AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(230, 70, 70), BackColor = Color.Transparent };
            lblSuccess = new Label { Location = new Point(30, y), AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(50, 200, 120), BackColor = Color.Transparent };

            btnReset = new Button
            {
                Text = "RESET PASSWORD",
                Location = new Point(30, y + 20),
                Size = new Size(380, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 140, 30),
                ForeColor = Color.FromArgb(18, 18, 24),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += BtnReset_Click;

            this.Controls.AddRange(new Control[] { bar, lbl, info, btnReset, lblError, lblSuccess });
        }

        private void BtnReset_Click(object s, EventArgs e)
        {
            lblError.Text = ""; lblSuccess.Text = "";
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtNew.Text))
            { lblError.Text = "⚠  All fields required."; return; }

            if (txtNew.Text != txtConfirm.Text)
            { lblError.Text = "⚠  Passwords do not match."; return; }

            DataTable dt = null;
            using (var userData = DB.Query("SELECT UserId FROM Users WHERE Username=@u AND Email=@e AND Role='Customer'",
                DB.P("@u", txtUser.Text.Trim()), DB.P("@e", txtEmail.Text.Trim())))
            {
                dt = userData;
                if (dt.Rows.Count == 0)
                { lblError.Text = "⚠  No account found with those details."; return; }
            }

            DB.NonQuery("UPDATE Users SET Password=@p WHERE Username=@u",
                DB.P("@p", txtNew.Text), DB.P("@u", txtUser.Text.Trim()));
            lblSuccess.Text = "✓  Password reset! You can now log in.";
            btnReset.Enabled = false;
        }

        private TextBox AddField(string label, ref int y, bool pwd = false)
        {
            this.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(110, 110, 130), AutoSize = true, BackColor = Color.Transparent, Location = new Point(30, y) });
            y += 18;
            var tb = new TextBox
            {
                Location = new Point(30, y),
                Size = new Size(380, 30),
                BackColor = Color.FromArgb(28, 28, 38),
                ForeColor = Color.FromArgb(240, 240, 248),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = pwd ? '●' : '\0'
            };
            this.Controls.Add(tb);
            y += 44;
            return tb;
        }
    }
}