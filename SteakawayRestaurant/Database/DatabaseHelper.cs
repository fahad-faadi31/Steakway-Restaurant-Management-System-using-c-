using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace SteakawayRestaurant.Database
{
    public static class DatabaseHelper
    {
        private static readonly string DbFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steakaway.db");

        public static string ConnStr => $"Data Source={DbFile};Version=3;";

        // ── Bootstrap ──────────────────────────────────────────────────────────
        public static void Initialize()
        {
            if (!File.Exists(DbFile)) SQLiteConnection.CreateFile(DbFile);

            using (var cn = Open())
            {
                Run(cn, "PRAGMA journal_mode=WAL;");

                // Users
                Run(cn, @"CREATE TABLE IF NOT EXISTS Users(
                    UserId    INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username  TEXT NOT NULL UNIQUE,
                    Password  TEXT NOT NULL,
                    Role      TEXT NOT NULL,
                    FullName  TEXT DEFAULT '',
                    Phone     TEXT DEFAULT '',
                    Email     TEXT DEFAULT '',
                    Address   TEXT DEFAULT '',
                    IsActive  INTEGER DEFAULT 1
                );");

                // Customer delivery addresses
                Run(cn, @"CREATE TABLE IF NOT EXISTS CustomerAddresses(
                    AddressId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId  INTEGER NOT NULL,
                    Label       TEXT DEFAULT 'Home',
                    Address     TEXT NOT NULL,
                    IsDefault   INTEGER DEFAULT 0,
                    FOREIGN KEY(CustomerId) REFERENCES Users(UserId)
                );");

                // Menu categories
                Run(cn, @"CREATE TABLE IF NOT EXISTS Categories(
                    CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name       TEXT NOT NULL UNIQUE
                );");

                // Menu items
                Run(cn, @"CREATE TABLE IF NOT EXISTS MenuItems(
                    ItemId      INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name        TEXT NOT NULL,
                    CategoryId  INTEGER,
                    Price       REAL NOT NULL DEFAULT 0,
                    Description TEXT DEFAULT '',
                    IsAvailable INTEGER DEFAULT 1,
                    FOREIGN KEY(CategoryId) REFERENCES Categories(CategoryId)
                );");

                // Orders
                Run(cn, @"CREATE TABLE IF NOT EXISTS Orders(
                    OrderId       INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId    INTEGER,
                    CustomerName  TEXT DEFAULT 'Guest',
                    Phone         TEXT DEFAULT '',
                    OrderType     TEXT DEFAULT 'DineIn',
                    TableNumber   TEXT DEFAULT '',
                    Address       TEXT DEFAULT '',
                    Status        TEXT DEFAULT 'Pending',
                    TotalAmount   REAL DEFAULT 0,
                    Discount      REAL DEFAULT 0,
                    Tax           REAL DEFAULT 0,
                    FinalAmount   REAL DEFAULT 0,
                    PaymentMethod TEXT DEFAULT 'Cash',
                    PaymentStatus TEXT DEFAULT 'Unpaid',
                    SpecialNotes  TEXT DEFAULT '',
                    WaiterId      INTEGER,
                    RiderId       INTEGER,
                    Rating        REAL DEFAULT 0,
                    CreatedAt     DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt     DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(CustomerId) REFERENCES Users(UserId)
                );");

                // Order items
                Run(cn, @"CREATE TABLE IF NOT EXISTS OrderItems(
                    OrderItemId  INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId      INTEGER NOT NULL,
                    ItemId       INTEGER NOT NULL,
                    ItemName     TEXT DEFAULT '',
                    Quantity     INTEGER DEFAULT 1,
                    UnitPrice    REAL DEFAULT 0,
                    Instructions TEXT DEFAULT '',
                    Status       TEXT DEFAULT 'Pending',
                    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId),
                    FOREIGN KEY(ItemId)  REFERENCES MenuItems(ItemId)
                );");

                // Cart
                Run(cn, @"CREATE TABLE IF NOT EXISTS Cart(
                    CartId       INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId   INTEGER NOT NULL,
                    ItemId       INTEGER NOT NULL,
                    ItemName     TEXT DEFAULT '',
                    Quantity     INTEGER DEFAULT 1,
                    UnitPrice    REAL DEFAULT 0,
                    Instructions TEXT DEFAULT '',
                    FOREIGN KEY(CustomerId) REFERENCES Users(UserId)
                );");

                // Transactions
                Run(cn, @"CREATE TABLE IF NOT EXISTS Transactions(
                    TxId        INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId     INTEGER NOT NULL,
                    AmountPaid  REAL DEFAULT 0,
                    Method      TEXT DEFAULT 'Cash',
                    CashierId   INTEGER,
                    Notes       TEXT DEFAULT '',
                    CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId)
                );");

                // Expenses
                Run(cn, @"CREATE TABLE IF NOT EXISTS Expenses(
                    ExpenseId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title       TEXT NOT NULL,
                    Amount      REAL DEFAULT 0,
                    Category    TEXT DEFAULT 'General',
                    Notes       TEXT DEFAULT '',
                    AddedBy     INTEGER,
                    CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP
                );");

                // Riders table
                Run(cn, @"CREATE TABLE IF NOT EXISTS Riders(
                    RiderId        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name           TEXT NOT NULL,
                    Phone          TEXT NOT NULL,
                    VehicleType    TEXT DEFAULT 'Bike',
                    IsActive       INTEGER DEFAULT 1,
                    IsBusy         INTEGER DEFAULT 0,
                    CurrentOrderId INTEGER DEFAULT 0,
                    CreatedAt      DATETIME DEFAULT CURRENT_TIMESTAMP
                );");

                // Delivery Assignments table
                Run(cn, @"CREATE TABLE IF NOT EXISTS Deliveries(
                    DeliveryId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId      INTEGER NOT NULL,
                    RiderId      INTEGER,
                    AssignedBy   INTEGER,
                    Status       TEXT DEFAULT 'Pending',
                    AssignedAt   DATETIME,
                    PickedUpAt   DATETIME,
                    DeliveredAt  DATETIME,
                    Notes        TEXT DEFAULT '',
                    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId),
                    FOREIGN KEY(RiderId) REFERENCES Riders(RiderId)
                );");

                // ── Seed default data ──────────────────────────────────────────────
                SeedDefaultData(cn);
            }
        }

        private static void SeedDefaultData(SQLiteConnection cn)
        {
            // Seed Manager
            long cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Users WHERE Username='admin'");
            if (cnt == 0)
            {
                Run(cn, "INSERT INTO Users(Username, Password, Role, FullName, Email, IsActive) VALUES('admin', 'Admin@123', 'Manager', 'Restaurant Manager', 'admin@steakaway.com', 1)");
            }

            // Seed Cashier
            cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Users WHERE Username='cashier1'");
            if (cnt == 0)
            {
                Run(cn, "INSERT INTO Users(Username, Password, Role, FullName, Phone, IsActive) VALUES('cashier1', 'Pass@123', 'Cashier', 'Bilal Ahmed', '0333-3333333', 1)");
            }

            // Seed Waiter
            cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Users WHERE Username='waiter1'");
            if (cnt == 0)
            {
                Run(cn, "INSERT INTO Users(Username, Password, Role, FullName, Phone, IsActive) VALUES('waiter1', 'Pass@123', 'Waiter', 'Ali Hassan', '0311-1111111', 1)");
            }

            // Seed XP (Kitchen)
            cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Users WHERE Username='xp1'");
            if (cnt == 0)
            {
                Run(cn, "INSERT INTO Users(Username, Password, Role, FullName, Phone, IsActive) VALUES('xp1', 'Pass@123', 'XP', 'Sara Khan', '0322-2222222', 1)");
            }

            // Seed Rider In-Charge user
            cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Users WHERE Username='rider'");
            if (cnt == 0)
            {
                Run(cn, "INSERT INTO Users(Username, Password, Role, FullName, Phone, IsActive) VALUES('rider', 'Rider@123', 'Rider', 'Rider In-Charge', '0300-0000000', 1)");
            }

            // Seed Categories and Menu Items
            cnt = (long)Scalar(cn, "SELECT COUNT(*) FROM Categories");
            if (cnt == 0)
            {
                Run(cn, @"INSERT INTO Categories(Name) VALUES
                    ('Steaks'),('Burgers'),('Chicken'),('Drinks'),('Desserts'),('Sides')");

                Run(cn, @"INSERT INTO MenuItems(Name, CategoryId, Price, Description) VALUES
                    ('Ribeye Steak',     1, 1800, 'Juicy 300g prime ribeye grilled to perfection'),
                    ('T-Bone Steak',     1, 2200, 'Classic T-bone with garlic herb butter'),
                    ('Sirloin Steak',    1, 1600, 'Tender 250g sirloin with peppercorn sauce'),
                    ('Fillet Steak',     1, 2500, 'Premium 200g beef fillet, melt-in-mouth'),
                    ('Classic Burger',   2,  650, 'Beef patty, lettuce, tomato, special sauce'),
                    ('Zinger Burger',    2,  550, 'Crispy chicken, jalapeños, coleslaw'),
                    ('Mushroom Swiss',   2,  700, 'Beef patty, sautéed mushrooms, Swiss cheese'),
                    ('Double Smash',     2,  850, 'Double smashed patties, cheddar, pickles'),
                    ('Grilled Chicken',  3,  900, 'Herb-marinated grilled chicken breast'),
                    ('Chicken Tikka',    3,  950, 'Spicy tikka marinated chicken platter'),
                    ('Pepsi',            4,  100, '330ml chilled can'),
                    ('7-Up',             4,  100, '330ml chilled can'),
                    ('Lemonade',         4,  180, 'Fresh squeezed mint lemonade'),
                    ('Milkshake',        4,  380, 'Thick creamy milkshake - choose flavour'),
                    ('Chocolate Lava',   5,  480, 'Warm molten chocolate cake with ice cream'),
                    ('Cheesecake',       5,  420, 'New York style baked cheesecake'),
                    ('Tiramisu',         5,  450, 'Classic Italian tiramisu'),
                    ('Fries',            6,  220, 'Crispy seasoned golden fries'),
                    ('Onion Rings',      6,  250, 'Beer-battered crispy onion rings'),
                    ('Coleslaw',         6,  140, 'Creamy homemade coleslaw'),
                    ('Garlic Bread',     6,  180, 'Toasted garlic bread with herb butter')");
            }

            // Seed default riders (4 riders)
            long riderCount = (long)Scalar(cn, "SELECT COUNT(*) FROM Riders");
            if (riderCount == 0)
            {
                Run(cn, @"INSERT INTO Riders(Name, Phone, VehicleType, IsActive, IsBusy) VALUES
                    ('Ahmed Raza', '0300-1234567', 'Bike', 1, 0),
                    ('Bilal Khan', '0301-2345678', 'Bike', 1, 0),
                    ('Danish Ali', '0302-3456789', 'Bike', 1, 0),
                    ('Farhan Shah', '0303-4567890', 'Bike', 1, 0)");
            }
        }

        // ── Connection helpers ─────────────────────────────────────────────────
        public static SQLiteConnection Open()
        {
            var cn = new SQLiteConnection(ConnStr);
            cn.Open();
            return cn;
        }

        private static void Run(SQLiteConnection cn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static object Scalar(SQLiteConnection cn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                return cmd.ExecuteScalar();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public static DataTable Query(string sql, params SQLiteParameter[] p)
        {
            using (var cn = Open())
            {
                using (var cmd = new SQLiteCommand(sql, cn))
                {
                    if (p != null) cmd.Parameters.AddRange(p);
                    var dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public static int NonQuery(string sql, params SQLiteParameter[] p)
        {
            using (var cn = Open())
            {
                using (var cmd = new SQLiteCommand(sql, cn))
                {
                    if (p != null) cmd.Parameters.AddRange(p);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static object Scalar(string sql, params SQLiteParameter[] p)
        {
            using (var cn = Open())
            {
                using (var cmd = new SQLiteCommand(sql, cn))
                {
                    if (p != null) cmd.Parameters.AddRange(p);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static long Insert(string sql, params SQLiteParameter[] p)
        {
            using (var cn = Open())
            {
                using (var cmd = new SQLiteCommand(sql + "; SELECT last_insert_rowid();", cn))
                {
                    if (p != null) cmd.Parameters.AddRange(p);
                    return (long)cmd.ExecuteScalar();
                }
            }
        }

        public static SQLiteParameter P(string name, object value)
        {
            return new SQLiteParameter(name, value ?? DBNull.Value);
        }
    }

    public static class DB
    {
        public static DataTable Query(string sql, params SQLiteParameter[] p) => DatabaseHelper.Query(sql, p);
        public static int NonQuery(string sql, params SQLiteParameter[] p) => DatabaseHelper.NonQuery(sql, p);
        public static object Scalar(string sql, params SQLiteParameter[] p) => DatabaseHelper.Scalar(sql, p);
        public static long Insert(string sql, params SQLiteParameter[] p) => DatabaseHelper.Insert(sql, p);
        public static SQLiteParameter P(string n, object v) => DatabaseHelper.P(n, v);
    }
}