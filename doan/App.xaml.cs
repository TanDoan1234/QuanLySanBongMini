using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using doan.Database;
using doan.Models;

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
                    SeedMockDataIfEmpty(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo cơ sở dữ liệu SQLite: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SeedMockDataIfEmpty(QLSanBongDbContext context)
        {
            // Nếu đã có dữ liệu hoá đơn thanh toán, không seed thêm nữa
            if (context.HoaDonThanhToans.Any())
            {
                return;
            }

            var random = new Random();
            var pitches = context.SanBongs.ToList();
            var customers = context.KhachHangs.ToList();
            var products = context.SanPhams.ToList();

            if (pitches.Count == 0 || customers.Count == 0 || products.Count == 0)
            {
                return; // Đảm bảo dữ liệu nền đã được seed từ DbContext.OnModelCreating
            }

            // Sinh dữ liệu trong vòng 30 ngày qua
            DateTime endDate = DateTime.Today;
            DateTime startDate = DateTime.Today.AddDays(-30);

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Mỗi ngày sinh từ 1 đến 4 hoá đơn
                int bookingCount = random.Next(1, 5);
                for (int i = 0; i < bookingCount; i++)
                {
                    // 1. Tạo ngẫu nhiên Phiếu Đặt Sân
                    var pitch = pitches[random.Next(pitches.Count)];
                    var customer = customers[random.Next(customers.Count)];
                    
                    // Giờ bắt đầu ngẫu nhiên từ 6:00 đến 21:00
                    int startHour = random.Next(6, 22);
                    int startMinute = random.Next(0, 2) * 30; // 0 hoặc 30 phút
                    var startTime = new TimeSpan(startHour, startMinute, 0);
                    
                    // Thời gian thuê ngẫu nhiên 1 tiếng, 1.5 tiếng hoặc 2 tiếng
                    double durationHours = random.Next(2, 5) * 0.5; // 1.0, 1.5, 2.0
                    var endTime = startTime.Add(TimeSpan.FromHours(durationHours));
                    
                    if (endTime.Days > 0) continue; // Tránh tràn sang ngày hôm sau

                    var reservation = new PhieuDatSan
                    {
                        MaSan = pitch.MaSan,
                        MaKhachHang = customer.MaKhachHang,
                        MaNguoiDung = random.Next(1, 3), // Admin (1) hoặc Nhân viên (2)
                        NgayDat = date,
                        GioBatDau = startTime,
                        GioKetThuc = endTime,
                        NgayTao = date.Add(startTime).AddMinutes(-random.Next(30, 120)), // đặt trước 30-120 phút
                        TrangThai = "Đã thanh toán"
                    };

                    context.PhieuDatSans.Add(reservation);
                    context.SaveChanges(); // Lưu để lấy MaPhieuDat

                    // 2. Tính tiền sân
                    decimal pitchCost = (decimal)durationHours * pitch.DonGiaTheoGio;

                    // 3. Tạo Hoá Đơn Thanh Toán
                    decimal discount = random.Next(0, 5) == 0 ? random.Next(1, 6) * 10000 : 0; // 20% cơ hội được giảm 10k-50k
                    decimal surcharge = random.Next(0, 8) == 0 ? random.Next(1, 4) * 10000 : 0; // 12% cơ hội phụ thu nước sôi/đá 10k-30k

                    var invoice = new HoaDonThanhToan
                    {
                        MaPhieuDat = reservation.MaPhieuDat,
                        NgayThanhToan = date.Add(endTime),
                        MaNguoiDung = reservation.MaNguoiDung,
                        TienSan = pitchCost,
                        TienDichVu = 0, // Sẽ cộng dồn sau
                        GiamGia = discount,
                        PhuThu = surcharge,
                        TongTien = 0, // Sẽ tính sau
                        TrangThai = "Đã thanh toán",
                        GhiChu = $"Dữ liệu mẫu - Thanh toán tự động ngày {date:dd/MM}"
                    };

                    context.HoaDonThanhToans.Add(invoice);
                    context.SaveChanges(); // Lưu để lấy MaHoaDon

                    // 4. Tạo chi tiết dịch vụ ngẫu nhiên
                    decimal serviceCost = 0;
                    int serviceCount = random.Next(1, 4); // mua từ 1 đến 3 loại sản phẩm/dịch vụ
                    
                    // Trộn ngẫu nhiên danh sách sản phẩm để không trùng lặp trong một hóa đơn
                    var selectedProducts = products.OrderBy(x => random.Next()).Take(serviceCount).ToList();

                    foreach (var prod in selectedProducts)
                    {
                        int quantity = random.Next(1, 6); // số lượng từ 1 đến 5
                        decimal lineTotal = quantity * prod.GiaBan;
                        serviceCost += lineTotal;

                        var detail = new ChiTietHoaDonDichVu
                        {
                            MaHoaDon = invoice.MaHoaDon,
                            MaSanPham = prod.MaSanPham,
                            SoLuong = quantity,
                            DonGiaBan = prod.GiaBan,
                            ThanhTien = lineTotal
                        };
                        context.ChiTietHoaDonDichVus.Add(detail);
                    }

                    // 5. Cập nhật lại tổng tiền và tiền dịch vụ của hoá đơn
                    invoice.TienDichVu = serviceCost;
                    invoice.TongTien = invoice.TienSan + invoice.TienDichVu + invoice.PhuThu - invoice.GiamGia;
                    context.SaveChanges();
                }
            }
        }
    }
}

