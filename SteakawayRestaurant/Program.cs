using System;
using System.Windows.Forms;
using SteakawayRestaurant.Database;
using SteakawayRestaurant.Forms;

namespace SteakawayRestaurant
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DatabaseHelper.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
