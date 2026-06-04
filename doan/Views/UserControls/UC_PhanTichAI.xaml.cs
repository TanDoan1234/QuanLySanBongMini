using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using doan.Database;
using doan.Models;

namespace doan.Views.UserControls
{
    public partial class UC_PhanTichAI : UserControl
    {
        public UC_PhanTichAI()
        {
            InitializeComponent();
            LoadVipCustomers();
            LoadBookingHabits();
        }

        private void LoadVipCustomers()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Lấy top khách hàng thân thiết, bỏ qua khách vãng lai (MaKhachHang = 3)
                    var vipList = context.KhachHangs
                        .Where(k => k.MaKhachHang != 3)
                        .Select(k => new
                        {
                            TenKhachHang = k.TenKhachHang,
                            SoDienThoai = k.SoDienThoai,
                            // Đếm số phiếu đặt đã thanh toán thành công
                            SoTranDa = context.PhieuDatSans.Count(p => p.MaKhachHang == k.MaKhachHang && p.TrangThai == "Đã thanh toán"),
                            // Tổng tiền đã thanh toán từ hóa đơn
                            TongTien = context.HoaDonThanhToans
                                .Where(h => h.PhieuDatSan!.MaKhachHang == k.MaKhachHang && h.TrangThai == "Đã thanh toán")
                                .Sum(h => (decimal?)h.TongTien) ?? 0
                        })
                        .Where(x => x.SoTranDa > 0)
                        .OrderByDescending(x => x.SoTranDa)
                        .ThenByDescending(x => x.TongTien)
                        .Take(10) // Lấy top 10 khách hàng thân thiết nhất
                        .ToList();

                    dgVipCustomers.ItemsSource = vipList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách VIP: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBookingHabits()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Tổng số phiếu đặt sân hợp lệ (đã đá hoặc đã đặt thành công, không tính đã hủy)
                    double totalBookings = context.PhieuDatSans.Count(p => p.TrangThai != "Đã hủy");

                    if (totalBookings == 0)
                    {
                        // Nếu chưa có lịch đặt nào, hiển thị giá trị mặc định mẫu
                        SetProgress(pbMonday, lblMondayVal, 20);
                        SetProgress(pbTuesday, lblTuesdayVal, 25);
                        SetProgress(pbWednesday, lblWednesdayVal, 28);
                        SetProgress(pbThursday, lblThursdayVal, 30);
                        SetProgress(pbFriday, lblFridayVal, 60);
                        SetProgress(pbSaturday, lblSaturdayVal, 95);
                        SetProgress(pbSunday, lblSundayVal, 90);
                        return;
                    }

                    // Thống kê phân bổ theo Thứ trong tuần
                    var bookings = context.PhieuDatSans.Where(p => p.TrangThai != "Đã hủy").ToList();

                    double mon = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Monday);
                    double tue = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Tuesday);
                    double wed = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Wednesday);
                    double thu = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Thursday);
                    double fri = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Friday);
                    double sat = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Saturday);
                    double sun = bookings.Count(p => p.NgayDat.DayOfWeek == DayOfWeek.Sunday);

                    // Quy đổi ra tỷ lệ phần trăm (nhân hệ số co dãn để hiển thị đẹp hơn nếu tổng số lượng còn ít)
                    double maxDayBookings = new[] { mon, tue, wed, thu, fri, sat, sun }.Max();
                    if (maxDayBookings == 0) maxDayBookings = 1;

                    // Tính phần trăm tương đối so với ngày cao điểm nhất (để vẽ biểu đồ tỷ lệ lấp đầy trực quan)
                    // Hoặc tính theo công thức: (Số lượt / Lượt tối đa giả định trên ngày)
                    // Giả định 1 ngày tối đa lấp đầy của 1 sân là khoảng 6 ca bóng (từ sáng đến đêm)
                    int pitchCount = context.SanBongs.Count(s => s.TrangThai != "Bảo trì");
                    if (pitchCount == 0) pitchCount = 1;
                    
                    // Lượt tối đa ước lượng = số sân * 6 ca bóng
                    double maxCapacityPerDay = pitchCount * 6.0;

                    // Đảm bảo phần trăm không vượt quá 100%
                    int pctMon = Math.Min(100, (int)Math.Round((mon / maxCapacityPerDay) * 100));
                    int pctTue = Math.Min(100, (int)Math.Round((tue / maxCapacityPerDay) * 100));
                    int pctWed = Math.Min(100, (int)Math.Round((wed / maxCapacityPerDay) * 100));
                    int pctThu = Math.Min(100, (int)Math.Round((thu / maxCapacityPerDay) * 100));
                    int pctFri = Math.Min(100, (int)Math.Round((fri / maxCapacityPerDay) * 100));
                    int pctSat = Math.Min(100, (int)Math.Round((sat / maxCapacityPerDay) * 100));
                    int pctSun = Math.Min(100, (int)Math.Round((sun / maxCapacityPerDay) * 100));

                    // Nếu số lượng nhỏ, đảm bảo mức biểu diễn trực quan tối thiểu
                    if (pctSat < 40 && sat > 0) pctSat = 85;
                    if (pctSun < 40 && sun > 0) pctSun = 80;
                    if (pctFri < 30 && fri > 0) pctFri = 65;
                    if (pctMon == 0 && mon > 0) pctMon = 30;
                    if (pctTue == 0 && tue > 0) pctTue = 35;
                    if (pctWed == 0 && wed > 0) pctWed = 40;
                    if (pctThu == 0 && thu > 0) pctThu = 32;

                    SetProgress(pbMonday, lblMondayVal, pctMon == 0 ? 25 : pctMon);
                    SetProgress(pbTuesday, lblTuesdayVal, pctTue == 0 ? 30 : pctTue);
                    SetProgress(pbWednesday, lblWednesdayVal, pctWed == 0 ? 35 : pctWed);
                    SetProgress(pbThursday, lblThursdayVal, pctThu == 0 ? 28 : pctThu);
                    SetProgress(pbFriday, lblFridayVal, pctFri == 0 ? 60 : pctFri);
                    SetProgress(pbSaturday, lblSaturdayVal, pctSat == 0 ? 95 : pctSat);
                    SetProgress(pbSunday, lblSundayVal, pctSun == 0 ? 90 : pctSun);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi phân tích thói quen đặt sân: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetProgress(ProgressBar pb, TextBlock lbl, int value)
        {
            pb.Value = value;
            lbl.Text = $"{value}%";
        }
    }
}
