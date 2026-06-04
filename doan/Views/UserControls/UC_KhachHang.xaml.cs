using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using doan.Database;
using doan.Models;

namespace doan.Views.UserControls
{
    public partial class UC_KhachHang : UserControl
    {
        private int selectedCustomerId = 0;

        public UC_KhachHang()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers(string? keyword = null)
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    var query = context.KhachHangs.AsQueryable();

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(k => k.TenKhachHang.Contains(keyword) || k.SoDienThoai.Contains(keyword));
                    }

                    dgCustomers.ItemsSource = query.OrderBy(k => k.TenKhachHang).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách khách hàng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCustomers.SelectedItem is KhachHang cust)
            {
                selectedCustomerId = cust.MaKhachHang;
                txtCustomerName.Text = cust.TenKhachHang;
                txtCustomerPhone.Text = cust.SoDienThoai;
                txtCustomerEmail.Text = cust.Email;
            }
        }

        private void btnNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            selectedCustomerId = 0;
            txtCustomerName.Clear();
            txtCustomerPhone.Clear();
            txtCustomerEmail.Clear();
            dgCustomers.SelectedIndex = -1;
        }

        private void btnSaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            string name = txtCustomerName.Text.Trim();
            string phone = txtCustomerPhone.Text.Trim();
            string email = txtCustomerEmail.Text.Trim();

            // 1. Validation rỗng
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Tên khách hàng không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCustomerName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Số điện thoại không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCustomerPhone.Focus();
                return;
            }

            // 2. Validation định dạng SĐT
            if (!Regex.IsMatch(phone, @"^[0-9]{9,11}$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ. Số điện thoại chỉ chứa các chữ số từ 9 đến 11 ký tự.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCustomerPhone.Focus();
                return;
            }

            // 3. Validation Email (nếu có nhập)
            if (!string.IsNullOrEmpty(email))
            {
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Địa chỉ Email không đúng định dạng.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCustomerEmail.Focus();
                    return;
                }
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Kiểm tra trùng Số điện thoại
                    bool isDuplicate = context.KhachHangs.Any(k => k.SoDienThoai == phone && k.MaKhachHang != selectedCustomerId);
                    if (isDuplicate)
                    {
                        MessageBox.Show("Số điện thoại này đã được đăng ký bởi khách hàng khác.", "Trùng lặp số điện thoại", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (selectedCustomerId == 0) // Thêm mới
                    {
                        var newCust = new KhachHang
                        {
                            TenKhachHang = name,
                            SoDienThoai = phone,
                            Email = string.IsNullOrEmpty(email) ? null : email
                        };
                        context.KhachHangs.Add(newCust);
                        MessageBox.Show("Đăng ký thông tin khách hàng mới thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // Cập nhật
                    {
                        var editCust = context.KhachHangs.Find(selectedCustomerId);
                        if (editCust != null)
                        {
                            editCust.TenKhachHang = name;
                            editCust.SoDienThoai = phone;
                            editCust.Email = string.IsNullOrEmpty(email) ? null : email;
                            MessageBox.Show("Cập nhật thông tin khách hàng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    context.SaveChanges();
                }

                ClearForm();
                LoadCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu thông tin khách hàng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCustomerId == 0) return;

            // Không cho phép xóa khách hàng mặc định "Khách vãng lai"
            if (selectedCustomerId == 3)
            {
                MessageBox.Show("Không được xóa tài khoản Khách vãng lai mặc định.", "Cảnh báo bảo mật", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa khách hàng này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // Kiểm tra xem khách hàng đã từng đặt sân bóng nào chưa
                        bool hasBookings = context.PhieuDatSans.Any(p => p.MaKhachHang == selectedCustomerId);
                        if (hasBookings)
                        {
                            MessageBox.Show("Không thể xóa khách hàng này vì đã có dữ liệu đặt sân trước đó.", "Lỗi ràng buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var cust = context.KhachHangs.Find(selectedCustomerId);
                        if (cust != null)
                        {
                            context.KhachHangs.Remove(cust);
                            context.SaveChanges();
                            MessageBox.Show("Xóa thông tin khách hàng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    ClearForm();
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa dữ liệu khách hàng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtSearchCustomer_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearchCustomer.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadCustomers();
            }
        }

        private void btnSearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearchCustomer.Text.Trim();
            LoadCustomers(keyword);
        }
    }
}
