using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using doan.Database;
using doan.Models;
using doan.Helpers;

namespace doan.Views.UserControls
{
    public partial class UC_HoaDonNhap : UserControl
    {
        public class ImportDetailTemp
        {
            public int MaSanPham { get; set; }
            public string TenSanPham { get; set; } = null!;
            public int SoLuong { get; set; }
            public decimal DonGiaNhap { get; set; }
            public decimal ThanhTien => SoLuong * DonGiaNhap;
        }

        private ObservableCollection<ImportDetailTemp> importDetails = new ObservableCollection<ImportDetailTemp>();
        private decimal totalAmount = 0;

        public UC_HoaDonNhap()
        {
            InitializeComponent();
            dgImportDetails.ItemsSource = importDetails;
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    cboProducts.ItemsSource = context.SanPhams.OrderBy(s => s.TenSanPham).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sản phẩm: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddToInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (cboProducts.SelectedItem is not SanPham selectedProd)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm cần nhập.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                cboProducts.Focus();
                return;
            }

            string qtyStr = txtImportQty.Text.Trim();
            string priceStr = txtImportPrice.Text.Trim();

            // 1. Validation số lượng
            if (!int.TryParse(qtyStr, out int qty) || qty <= 0)
            {
                MessageBox.Show("Số lượng nhập phải là số nguyên dương lớn hơn 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtImportQty.Focus();
                return;
            }

            // 2. Validation đơn giá nhập
            if (!decimal.TryParse(priceStr, out decimal price) || price < 0)
            {
                MessageBox.Show("Đơn giá nhập phải là số hợp lệ và lớn hơn hoặc bằng 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtImportPrice.Focus();
                return;
            }

            // Kiểm tra xem sản phẩm đã có trong hóa đơn tạm thời chưa
            var existingItem = importDetails.FirstOrDefault(i => i.MaSanPham == selectedProd.MaSanPham);
            if (existingItem != null)
            {
                // Cập nhật số lượng và tính lại đơn giá trung bình hoặc giữ nguyên đơn giá mới
                existingItem.SoLuong += qty;
                existingItem.DonGiaNhap = price; // Cập nhật đơn giá nhập mới nhất
                
                // Refresh DataGrid
                dgImportDetails.Items.Refresh();
            }
            else
            {
                importDetails.Add(new ImportDetailTemp
                {
                    MaSanPham = selectedProd.MaSanPham,
                    TenSanPham = selectedProd.TenSanPham,
                    SoLuong = qty,
                    DonGiaNhap = price
                });
            }

            UpdateTotalAmount();
            
            // Reset input fields
            txtImportQty.Text = "1";
            txtImportPrice.Clear();
            cboProducts.SelectedIndex = -1;
        }

        private void btnRemoveDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ImportDetailTemp item)
            {
                importDetails.Remove(item);
                UpdateTotalAmount();
            }
        }

        private void UpdateTotalAmount()
        {
            totalAmount = importDetails.Sum(i => i.ThanhTien);
            lblTotalAmount.Text = $"{totalAmount:#,##0} VNĐ";
        }

        private void btnSaveImportInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (importDetails.Count == 0)
            {
                MessageBox.Show("Hóa đơn nhập trống. Vui lòng thêm ít nhất một sản phẩm.", "Lỗi nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Không xác định được nhân viên đang đăng nhập. Vui lòng đăng nhập lại.", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Xác nhận lưu hóa đơn nhập hàng và cập nhật số lượng tồn kho?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // 1. Tạo hóa đơn nhập
                        var invoice = new HoaDonNhap
                        {
                            NgayNhap = DateTime.Now,
                            MaNguoiDung = Session.CurrentUser.MaNguoiDung,
                            TongTien = totalAmount
                        };

                        context.HoaDonNhaps.Add(invoice);
                        context.SaveChanges(); // Lưu trước để sinh MaHoaDonNhap tự tăng

                        // 2. Lưu chi tiết hóa đơn nhập & cập nhật tồn kho
                        foreach (var item in importDetails)
                        {
                            // Tạo dòng chi tiết nhập
                            var detail = new ChiTietHoaDonNhap
                            {
                                MaHoaDonNhap = invoice.MaHoaDonNhap,
                                MaSanPham = item.MaSanPham,
                                SoLuong = item.SoLuong,
                                DonGiaNhap = item.DonGiaNhap,
                                ThanhTien = item.ThanhTien
                            };
                            context.ChiTietHoaDonNhaps.Add(detail);

                            // Cập nhật số lượng tồn kho của Sản phẩm
                            var prod = context.SanPhams.Find(item.MaSanPham);
                            if (prod != null)
                            {
                                prod.SoLuongTon += item.SoLuong;
                            }
                        }

                        context.SaveChanges(); // Lưu toàn bộ chi tiết và cập nhật tồn kho
                        
                        MessageBox.Show($"Lập hóa đơn nhập hàng thành công!\nMã hóa đơn: {invoice.MaHoaDonNhap}\nTổng tiền: {totalAmount:#,##0} VNĐ", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Reset Form
                        importDetails.Clear();
                        UpdateTotalAmount();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi lưu hóa đơn nhập hàng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
