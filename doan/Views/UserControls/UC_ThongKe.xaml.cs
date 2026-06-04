using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ClosedXML.Excel;
using doan.Database;
using doan.Models;

namespace doan.Views.UserControls
{
    public partial class UC_ThongKe : UserControl
    {
        public UC_ThongKe()
        {
            InitializeComponent();
            // Mặc định lọc từ đầu tháng hiện tại đến hôm nay
            DateTime today = DateTime.Today;
            dpFromDate.SelectedDate = new DateTime(today.Year, today.Month, 1);
            dpToDate.SelectedDate = today;

            LoadData();
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            DateTime fromDate = dpFromDate.SelectedDate ?? DateTime.Today.AddDays(-30);
            DateTime toDate = dpToDate.SelectedDate ?? DateTime.Today;

            // Đảm bảo toDate bao quát hết giờ trong ngày (đến 23:59:59)
            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // 1. Lọc hóa đơn đã thanh toán
                    var invoices = context.HoaDonThanhToans
                        .Include(h => h.PhieuDatSan)
                            .ThenInclude(p => p!.KhachHang)
                        .Include(h => h.PhieuDatSan)
                            .ThenInclude(p => p!.SanBong)
                        .Where(h => h.TrangThai == "Đã thanh toán" && h.NgayThanhToan >= fromDate && h.NgayThanhToan <= toDate)
                        .OrderByDescending(h => h.NgayThanhToan)
                        .ToList();

                    dgInvoices.ItemsSource = invoices;

                    // 2. Tính toán và cập nhật các thẻ KPI
                    decimal totalRev = invoices.Sum(h => h.TongTien);
                    decimal pitchRev = invoices.Sum(h => h.TienSan);
                    decimal serviceRev = invoices.Sum(h => h.TienDichVu);
                    int totalBookings = invoices.Count;

                    lblTotalRevenue.Text = $"{totalRev:#,##0} VNĐ";
                    lblPitchRevenue.Text = $"{pitchRev:#,##0} VNĐ";
                    lblServiceRevenue.Text = $"{serviceRev:#,##0} VNĐ";
                    lblTotalBookings.Text = $"{totalBookings} lượt";

                    // 3. Gom nhóm theo Sân bóng (Yêu cầu STT 10)
                    var groupedPitches = invoices
                        .GroupBy(h => h.PhieuDatSan!.SanBong!.TenSan)
                        .Select(g => new
                        {
                            TenSan = g.Key,
                            LuotDa = g.Count(),
                            DoanhThu = g.Sum(h => h.TienSan)
                        })
                        .OrderByDescending(g => g.DoanhThu)
                        .ToList();

                    dgGroupedPitches.ItemsSource = groupedPitches;

                    // 4. Gom nhóm theo Sản phẩm / Dịch vụ đã bán chạy (Yêu cầu STT 10)
                    var groupedProducts = context.ChiTietHoaDonDichVus
                        .Include(c => c.HoaDonThanhToan)
                        .Include(c => c.SanPham)
                        .Where(c => c.HoaDonThanhToan!.TrangThai == "Đã thanh toán" && c.HoaDonThanhToan.NgayThanhToan >= fromDate && c.HoaDonThanhToan.NgayThanhToan <= toDate)
                        .GroupBy(c => new { c.SanPham!.TenSanPham, c.SanPham.DonViTinh })
                        .Select(g => new
                        {
                            TenSanPham = g.Key.TenSanPham,
                            DonViTinh = g.Key.DonViTinh,
                            SoLuongBan = g.Sum(c => c.SoLuong),
                            DoanhThu = g.Sum(c => c.ThanhTien)
                        })
                        .OrderByDescending(g => g.SoLuongBan)
                        .ToList();

                    dgGroupedProducts.ItemsSource = groupedProducts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải báo cáo thống kê: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnViewInvoicePrint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HoaDonThanhToan invoice)
            {
                InvoicePrintWindow printWindow = new InvoicePrintWindow(invoice.MaHoaDon);
                printWindow.Owner = Window.GetWindow(this);
                printWindow.ShowDialog();
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var invoices = dgInvoices.ItemsSource as System.Collections.Generic.List<HoaDonThanhToan>;
            if (invoices == null || invoices.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu hóa đơn nào trong danh sách để xuất báo cáo.", "Lỗi nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hộp thoại chọn vị trí lưu file Excel
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Doanh thu");

                        // 1. Thiết kế Tiêu đề Excel
                        worksheet.Cell("A1").Value = "BÁO CÁO THỐNG KÊ DOANH THU SÂN BÓNG MINI";
                        var titleStyle = worksheet.Range("A1:G1").Merge().Style;
                        titleStyle.Font.Bold = true;
                        titleStyle.Font.FontSize = 16;
                        titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        DateTime fromDate = dpFromDate.SelectedDate ?? DateTime.Today.AddDays(-30);
                        DateTime toDate = dpToDate.SelectedDate ?? DateTime.Today;
                        worksheet.Cell("A2").Value = $"Khoảng thời gian: Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}";
                        var subTitleStyle = worksheet.Range("A2:G2").Merge().Style;
                        subTitleStyle.Font.Italic = true;
                        subTitleStyle.Font.FontSize = 11;
                        subTitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // 2. Thiết lập header cột bảng dữ liệu
                        string[] headers = { "Mã HD", "Ngày thanh toán", "Khách hàng", "Sân bóng", "Tiền sân (đ)", "Tiền dịch vụ (đ)", "Tổng tiền (đ)" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(4, i + 1);
                            cell.Value = headers[i];
                            var cellStyle = cell.Style;
                            cellStyle.Font.Bold = true;
                            cellStyle.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                            cellStyle.Font.FontColor = XLColor.White;
                            cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        // 3. Đổ dữ liệu chi tiết hóa đơn
                        int startRow = 5;
                        for (int i = 0; i < invoices.Count; i++)
                        {
                            var inv = invoices[i];
                            worksheet.Cell(startRow + i, 1).Value = inv.MaHoaDon.ToString("D5");
                            worksheet.Cell(startRow + i, 2).Value = inv.NgayThanhToan.ToString("dd/MM/yyyy HH:mm");
                            worksheet.Cell(startRow + i, 3).Value = inv.PhieuDatSan?.KhachHang?.TenKhachHang ?? "Khách vãng lai";
                            worksheet.Cell(startRow + i, 4).Value = inv.PhieuDatSan?.SanBong?.TenSan ?? "";
                            worksheet.Cell(startRow + i, 5).Value = inv.TienSan;
                            worksheet.Cell(startRow + i, 6).Value = inv.TienDichVu;
                            worksheet.Cell(startRow + i, 7).Value = inv.TongTien;

                            // Định dạng tiền tệ VNĐ cho các cột số
                            worksheet.Cell(startRow + i, 5).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(startRow + i, 6).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(startRow + i, 7).Style.NumberFormat.Format = "#,##0";
                        }

                        // 4. Dòng tổng kết Sum
                        int lastRow = startRow + invoices.Count;
                        worksheet.Cell(lastRow, 1).Value = "TỔNG CỘNG";
                        var sumLabelStyle = worksheet.Range(worksheet.Cell(lastRow, 1), worksheet.Cell(lastRow, 4)).Merge().Style;
                        sumLabelStyle.Font.Bold = true;
                        sumLabelStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        worksheet.Cell(lastRow, 5).FormulaA1 = $"=SUM(E{startRow}:E{lastRow - 1})";
                        worksheet.Cell(lastRow, 6).FormulaA1 = $"=SUM(F{startRow}:F{lastRow - 1})";
                        worksheet.Cell(lastRow, 7).FormulaA1 = $"=SUM(G{startRow}:G{lastRow - 1})";

                        worksheet.Cell(lastRow, 5).Style.Font.Bold = true;
                        worksheet.Cell(lastRow, 5).Style.NumberFormat.Format = "#,##0";
                        
                        worksheet.Cell(lastRow, 6).Style.Font.Bold = true;
                        worksheet.Cell(lastRow, 6).Style.NumberFormat.Format = "#,##0";
                        
                        worksheet.Cell(lastRow, 7).Style.Font.Bold = true;
                        worksheet.Cell(lastRow, 7).Style.NumberFormat.Format = "#,##0";

                        // Vẽ khung viền bảng cho tất cả dữ liệu
                        worksheet.Range(4, 1, lastRow, 7).Style
                            .Border.SetTopBorder(XLBorderStyleValues.Thin)
                            .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                            .Border.SetLeftBorder(XLBorderStyleValues.Thin)
                            .Border.SetRightBorder(XLBorderStyleValues.Thin);

                        // Tự co giãn cột
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show("Xuất báo cáo thống kê doanh thu ra Excel thành công!", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi trong quá trình xuất Excel: {ex.Message}", "Lỗi xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
