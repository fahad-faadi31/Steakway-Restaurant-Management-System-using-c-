using SteakawayRestaurant.Models;

namespace SteakawayRestaurant.Helpers
{
    public static class SessionManager
    {
        public static int UserId { get; private set; }
        public static string Username { get; private set; }
        public static string FullName { get; private set; }
        public static string Role { get; private set; }
        public static bool IsLoggedIn => UserId > 0;

        public static void Login(User u)
        {
            UserId = u.UserId;
            Username = u.Username;
            FullName = u.FullName;
            Role = u.Role;
        }

        public static void Logout()
        {
            UserId = 0;
            Username = null;
            FullName = null;
            Role = null;
        }
    }
}