using doan.Models;

namespace doan.Helpers
{
    public static class Session
    {
        public static NguoiDung? CurrentUser { get; set; }

        public static bool IsAdmin => CurrentUser?.VaiTro == "Admin";

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
