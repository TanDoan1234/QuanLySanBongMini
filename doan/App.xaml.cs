using System;
using System.Configuration;
using System.Data;
using System.Windows;
using doan.Database;

namespace doan
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Tự động kiểm tra và tạo cơ sở dữ liệu cùng với dữ liệu mẫu (Seed Data)
                using (var context = new QLSanBongDbContext())
                {
                    context.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo cơ sở dữ liệu SQL Server: {ex.Message}\n\nHướng dẫn: Vui lòng đảm bảo rằng Microsoft SQL Server (SQLEXPRESS) đang chạy trên máy tính của bạn và tài khoản hiện tại có quyền tạo cơ sở dữ liệu.", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

