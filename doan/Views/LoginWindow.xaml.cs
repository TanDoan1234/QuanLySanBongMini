using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using doan.Database;
using doan.Helpers;

namespace doan.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            // Validation đầu vào
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Tên đăng nhập không được để trống.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Mật khẩu không được để trống.");
                txtPassword.Focus();
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Mã hóa mật khẩu đầu vào để so sánh
                    string hashedPassword = QLSanBongDbContext.HashPassword(password);

                    // Tìm kiếm tài khoản trong CSDL
                    var user = context.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username && u.MatKhau == hashedPassword);

                    if (user != null)
                    {
                        if (!user.TrangThai)
                        {
                            ShowError("Tài khoản của bạn đã bị khóa.");
                            return;
                        }

                        // Đăng nhập thành công, lưu session
                        Session.CurrentUser = user;

                        // Chuyển hướng sang MainWindow
                        MainWindow main = new MainWindow();
                        main.Show();
                        
                        this.Close();
                    }
                    else
                    {
                        ShowError("Tên đăng nhập hoặc mật khẩu không chính xác.");
                        txtPassword.Clear();
                        txtPassword.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống chi tiết:\n{ex}", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }
    }
}
