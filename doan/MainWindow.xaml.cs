using System;
using System.Windows;
using System.Windows.Threading;
using doan.Helpers;
using doan.Views;
using doan.Views.UserControls;

namespace doan
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? timerClock;

        public MainWindow()
        {
            InitializeComponent();
            StartClock();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra session đăng nhập bảo mật
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Phiên đăng nhập hết hạn hoặc chưa đăng nhập. Vui lòng đăng nhập lại.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                RedoLogin();
                return;
            }

            // 2. Điền thông tin tài khoản đang đăng nhập
            lblLoggedUserFullName.Text = Session.CurrentUser.HoTen;
            lblLoggedUserRole.Text = Session.CurrentUser.VaiTro == "Admin" ? "Quản trị viên" : "Nhân viên";

            // 3. Phân quyền Người dùng (Authorization - Yêu cầu STT 8 phân quyền truy cập)
            if (Session.CurrentUser.VaiTro != "Admin")
            {
                // Nhân viên chỉ được xem Sơ đồ sân, Quản lý Khách hàng, Nhập kho hàng hóa.
                // Ẩn các tính năng quản trị hệ thống và báo cáo doanh thu nhạy cảm.
                btnMenuSanBong.Visibility = Visibility.Collapsed;
                btnMenuSanPham.Visibility = Visibility.Collapsed;
                btnMenuThongKe.Visibility = Visibility.Collapsed;
                btnMenuAI.Visibility = Visibility.Collapsed;
            }

            // Hiển thị màn hình Dashboard đầu tiên
            if (lblCurrentViewTitle != null)
            {
                lblCurrentViewTitle.Text = "Sơ Đồ Trạng Thái Sân Bóng";
            }
            ShowUserControl(new UC_Dashboard());
        }

        private void StartClock()
        {
            timerClock = new DispatcherTimer();
            timerClock.Interval = TimeSpan.FromSeconds(1);
            timerClock.Tick += (s, e) =>
            {
                lblSystemClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            };
            timerClock.Start();
        }

        private void ShowUserControl(object uc)
        {
            if (contentArea == null) return;
            contentArea.Content = uc;
        }

        #region ĐỊNH TUYẾN MENU ĐIỀU HƯỚNG

        private void btnMenuDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Sơ Đồ Trạng Thái Sân Bóng";
            ShowUserControl(new UC_Dashboard());
        }

        private void btnMenuKhachHang_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Quản Lý Danh Sách Khách Hàng";
            ShowUserControl(new UC_KhachHang());
        }

        private void btnMenuNhapHang_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Quản Lý Nhập Kho Hàng Hóa";
            ShowUserControl(new UC_HoaDonNhap());
        }

        private void btnMenuSanBong_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Cấu Hình & Quản Lý Sân Bóng";
            ShowUserControl(new UC_SanBong());
        }

        private void btnMenuSanPham_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Quản Lý Danh Mục & Sản Phẩm Dịch Vụ";
            ShowUserControl(new UC_SanPham());
        }

        private void btnMenuThongKe_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Báo Cáo Thống Kê Doanh Thu";
            ShowUserControl(new UC_ThongKe());
        }

        private void btnMenuAI_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCurrentViewTitle == null) return;
            lblCurrentViewTitle.Text = "Trợ Lý Phân Tích & Tối Ưu Doanh Thu";
            ShowUserControl(new UC_PhanTichAI());
        }

        #endregion

        #region NGHIỆP VỤ ĐĂNG XUẤT

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?", "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Session.CurrentUser = null; // Xóa session tài khoản hiện tại
                RedoLogin();
            }
        }

        private void RedoLogin()
        {
            if (timerClock != null)
            {
                timerClock.Stop();
            }

            // Mở lại màn hình đăng nhập
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            
            // Đóng cửa sổ chính MainWindow
            this.Close();
        }

        #endregion
    }
}