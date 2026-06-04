using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.EntityFrameworkCore;
using doan.Database;
using doan.Models;

namespace doan.Views
{
    public partial class InvoicePrintWindow : Window
    {
        private int billId;

        public InvoicePrintWindow(int billId)
        {
            InitializeComponent();
            this.billId = billId;
            LoadInvoiceData();
        }

        private void LoadInvoiceData()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Lấy thông tin hóa đơn cùng các bảng liên quan
                    var bill = context.HoaDonThanhToans
                        .Include(h => h.NguoiDung)
                        .Include(h => h.PhieuDatSan)
                            .ThenInclude(p => p!.KhachHang)
                        .Include(h => h.PhieuDatSan)
                            .ThenInclude(p => p!.SanBong)
                        .FirstOrDefault(h => h.MaHoaDon == billId);

                    if (bill == null)
                    {
                        MessageBox.Show("Không tìm thấy dữ liệu hóa đơn này.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Điền thông tin chung
                    lblInvoiceId.Text = bill.MaHoaDon.ToString("D5");
                    lblInvoiceDate.Text = bill.NgayThanhToan.ToString("dd/MM/yyyy HH:mm");
                    
                    var booking = bill.PhieuDatSan;
                    if (booking != null)
                    {
                        lblCustomerName.Text = booking.KhachHang?.TenKhachHang ?? "Khách vãng lai";
                        lblCustomerPhone.Text = booking.KhachHang?.SoDienThoai ?? "N/A";
                        lblPitchName.Text = booking.SanBong?.TenSan ?? "Sân không rõ";
                        
                        TimeSpan duration = booking.GioKetThuc - booking.GioBatDau;
                        lblTimeSlot.Text = $"{booking.GioBatDau.ToString(@"hh\:mm")} ‒ {booking.GioKetThuc.ToString(@"hh\:mm")} ({duration.TotalHours:0.0}h)";
                        
                        // Đổ chi phí tổng
                        lblTienSan.Text = $"{bill.TienSan:#,##0} đ";
                        lblTienDichVu.Text = $"{bill.TienDichVu:#,##0} đ";
                        lblPhuThu.Text = $"{bill.PhuThu:#,##0} đ";
                        lblGiamGia.Text = $"-{bill.GiamGia:#,##0} đ";
                        lblTongTien.Text = $"{bill.TongTien:#,##0} đ";

                        // Clear dòng cũ
                        panelBillRows.Children.Clear();

                        // 1. Thêm dòng Tiền Sân Bóng đầu tiên vào bảng chi tiết
                        AddBillRow("Tiền thuê sân bóng", 1, bill.TienSan, bill.TienSan);

                        // 2. Thêm các dòng dịch vụ phát sinh
                        var services = context.ChiTietHoaDonDichVus
                            .Where(c => c.MaHoaDon == bill.MaHoaDon)
                            .Include(c => c.SanPham)
                            .ToList();

                        foreach (var item in services)
                        {
                            AddBillRow(item.SanPham?.TenSanPham ?? "Dịch vụ", item.SoLuong, item.DonGiaBan, item.ThanhTien);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void AddBillRow(string itemName, int qty, decimal price, decimal total)
        {
            // Tạo một Grid con cho từng dòng chi tiết để khớp với các cột tiêu đề
            Grid rowGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            // Tên mặt hàng
            TextBlock txtName = new TextBlock
            {
                Text = itemName,
                FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(txtName, 0);
            rowGrid.Children.Add(txtName);

            // Số lượng
            TextBlock txtQty = new TextBlock
            {
                Text = qty.ToString(),
                FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(txtQty, 1);
            rowGrid.Children.Add(txtQty);

            // Đơn giá
            TextBlock txtPrice = new TextBlock
            {
                Text = $"{price:#,##0}",
                FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(txtPrice, 2);
            rowGrid.Children.Add(txtPrice);

            // Thành tiền
            TextBlock txtTotal = new TextBlock
            {
                Text = $"{total:#,##0}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(txtTotal, 3);
            rowGrid.Children.Add(txtTotal);

            // Add vào StackPanel dòng chi tiết
            panelBillRows.Children.Add(rowGrid);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Lấy lề in và in thẳng Grid
                    printDialog.PrintVisual(printArea, $"In Hoa Don {billId}");
                    MessageBox.Show("Gửi lệnh in hóa đơn thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình in ấn: {ex.Message}", "Lỗi máy in", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
