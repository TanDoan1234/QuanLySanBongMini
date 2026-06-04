using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using doan.Database;
using doan.Models;
using doan.Helpers;

namespace doan.Views.UserControls
{
    public partial class UC_Dashboard : UserControl
    {
        public class ActiveServiceView
        {
            public int MaChiTietDV { get; set; }
            public int MaSanPham { get; set; }
            public string TenSanPham { get; set; } = null!;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien { get; set; }
        }

        private SanBong? currentPitch = null;
        private PhieuDatSan? currentBooking = null;
        private HoaDonThanhToan? currentBill = null;
        private List<ActiveServiceView> activeServices = new List<ActiveServiceView>();

        public UC_Dashboard()
        {
            InitializeComponent();
            LoadPitches();
            LoadServicesComboBox();
            dpBookingDate.SelectedDate = DateTime.Today;
            
            // Set giờ chơi mặc định
            txtBookingStartTime.Text = DateTime.Now.ToString("HH:mm");
            txtBookingEndTime.Text = DateTime.Now.AddHours(1.5).ToString("HH:mm");
        }

        #region LOADS DỮ LIỆU

        public void LoadPitches()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Lấy tất cả sân bóng sắp xếp theo tên
                    var pitches = context.SanBongs.OrderBy(s => s.TenSan).ToList();
                    icPitches.ItemsSource = pitches;

                    // Nếu có sân đang được chọn, cập nhật lại trạng thái hiển thị của nó
                    if (currentPitch != null)
                    {
                        var updatedPitch = pitches.FirstOrDefault(s => s.MaSan == currentPitch.MaSan);
                        if (updatedPitch != null)
                        {
                            currentPitch = updatedPitch;
                            SelectPitch(updatedPitch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sơ đồ sân bóng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServicesComboBox()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    cboAddProduct.ItemsSource = context.SanPhams.OrderBy(s => s.TenSanPham).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh mục sản phẩm dịch vụ: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region SỰ KIỆN CLICK CHỌN SÂN

        private void btnPitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SanBong pitch)
            {
                SelectPitch(pitch);
            }
        }

        private void SelectPitch(SanBong pitch)
        {
            currentPitch = pitch;
            lblSelectedPitchName.Text = pitch.TenSan;
            lblSelectedPitchStatus.Text = pitch.TrangThai.ToUpper();

            // Đổi màu badge trạng thái
            if (pitch.TrangThai == "Trống")
            {
                borderSelectedPitchStatus.Background = (SolidColorBrush)FindResource("PrimaryBrush");
                gridBookingForm.Visibility = Visibility.Visible;
                gridActiveBill.Visibility = Visibility.Collapsed;
                panelMaintenance.Visibility = Visibility.Collapsed;
                panelNoSelection.Visibility = Visibility.Collapsed;
                
                // Reset form đặt sân
                txtBookingPhone.Clear();
                txtBookingCustomerName.Clear();
                dpBookingDate.SelectedDate = DateTime.Today;
                txtBookingStartTime.Text = DateTime.Now.ToString("HH:mm");
                txtBookingEndTime.Text = DateTime.Now.AddHours(1.5).ToString("HH:mm");
            }
            else if (pitch.TrangThai == "Đang đá")
            {
                borderSelectedPitchStatus.Background = (SolidColorBrush)FindResource("DangerBrush");
                gridBookingForm.Visibility = Visibility.Collapsed;
                gridActiveBill.Visibility = Visibility.Visible;
                panelMaintenance.Visibility = Visibility.Collapsed;
                panelNoSelection.Visibility = Visibility.Collapsed;

                LoadActiveBillForPitch(pitch.MaSan);
            }
            else if (pitch.TrangThai == "Bảo trì")
            {
                borderSelectedPitchStatus.Background = (SolidColorBrush)FindResource("WarningBrush");
                gridBookingForm.Visibility = Visibility.Collapsed;
                gridActiveBill.Visibility = Visibility.Collapsed;
                panelMaintenance.Visibility = Visibility.Visible;
                panelNoSelection.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadActiveBillForPitch(int pitchId)
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Tìm phiếu đặt sân đang đá trên sân này
                    currentBooking = context.PhieuDatSans
                        .Include(p => p.KhachHang)
                        .FirstOrDefault(p => p.MaSan == pitchId && p.TrangThai == "Đã nhận sân");

                    if (currentBooking != null)
                    {
                        lblBillCustomer.Text = currentBooking.KhachHang!.TenKhachHang;
                        lblBillStartTime.Text = currentBooking.GioBatDau.ToString(@"hh\:mm");
                        
                        // Tính số giờ tạm tính
                        TimeSpan duration = currentBooking.GioKetThuc - currentBooking.GioBatDau;
                        lblBillDuration.Text = $"{duration.TotalHours:0.0} giờ ({duration.TotalMinutes} phút)";

                        // Tìm hóa đơn chưa thanh toán tương ứng
                        currentBill = context.HoaDonThanhToans
                            .FirstOrDefault(h => h.MaPhieuDat == currentBooking.MaPhieuDat && h.TrangThai == "Chưa thanh toán");

                        if (currentBill == null)
                        {
                            // Nếu chưa có hóa đơn (phòng hờ lỗi hệ thống), tạo mới hóa đơn chưa thanh toán
                            currentBill = new HoaDonThanhToan
                            {
                                MaPhieuDat = currentBooking.MaPhieuDat,
                                NgayThanhToan = DateTime.Now,
                                MaNguoiDung = Session.CurrentUser?.MaNguoiDung ?? 1,
                                TienSan = (decimal)duration.TotalHours * currentPitch!.DonGiaTheoGio,
                                TienDichVu = 0,
                                TongTien = (decimal)duration.TotalHours * currentPitch!.DonGiaTheoGio,
                                TrangThai = "Chưa thanh toán"
                            };
                            context.HoaDonThanhToans.Add(currentBill);
                            context.SaveChanges();
                        }

                        // Load các chi tiết dịch vụ
                        LoadActiveServices(currentBill.MaHoaDon);
                    }
                    else
                    {
                        // Sân báo đang đá nhưng không tìm thấy phiếu đặt sân -> Đồng bộ lại trạng thái sân thành Trống
                        var pitch = context.SanPhams.Find(pitchId);
                        MessageBox.Show("Không tìm thấy thông tin đặt sân đang hoạt động cho sân này. Hệ thống sẽ tự động đưa trạng thái sân về Trống.", "Lỗi đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        var p = context.SanBongs.Find(pitchId);
                        if (p != null) p.TrangThai = "Trống";
                        context.SaveChanges();
                        LoadPitches();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin thanh toán: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActiveServices(int billId)
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    activeServices = context.ChiTietHoaDonDichVus
                        .Where(c => c.MaHoaDon == billId)
                        .Include(c => c.SanPham)
                        .Select(c => new ActiveServiceView
                        {
                            MaChiTietDV = c.MaChiTietDV,
                            MaSanPham = c.MaSanPham,
                            TenSanPham = c.SanPham!.TenSanPham,
                            SoLuong = c.SoLuong,
                            DonGia = c.DonGiaBan,
                            ThanhTien = c.ThanhTien
                        }).ToList();

                    dgActiveServices.ItemsSource = activeServices;
                    
                    // Cập nhật số tiền hiển thị
                    UpdateBillSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết dịch vụ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBillSummary()
        {
            if (currentBill == null || currentPitch == null || currentBooking == null) return;

            TimeSpan duration = currentBooking.GioKetThuc - currentBooking.GioBatDau;
            decimal tienSan = (decimal)duration.TotalHours * currentPitch.DonGiaTheoGio;
            decimal tienDV = activeServices.Sum(s => s.ThanhTien);

            lblBillPitchAmount.Text = $"{tienSan:#,##0} VNĐ";
            lblBillServiceAmount.Text = $"{tienDV:#,##0} VNĐ";

            decimal giamGia = 0;
            decimal phuThu = 0;
            decimal.TryParse(txtBillDiscount.Text, out giamGia);
            decimal.TryParse(txtBillSurcharge.Text, out phuThu);

            decimal tongTien = tienSan + tienDV - giamGia + phuThu;
            if (tongTien < 0) tongTien = 0;

            lblBillTotalAmount.Text = $"{tongTien:#,##0} VNĐ";
        }

        #endregion

        #region NGHIỆP VỤ TÌM KHÁCH HÀNG

        private void btnSearchBookingCust_Click(object sender, RoutedEventArgs e)
        {
            string phone = txtBookingPhone.Text.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại để tìm kiếm.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    var customer = context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phone);
                    if (customer != null)
                    {
                        txtBookingCustomerName.Text = customer.TenKhachHang;
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy thông tin khách hàng này. Bạn có thể nhập tên để hệ thống tự động đăng ký mới.", "Khách hàng mới", MessageBoxButton.OK, MessageBoxImage.Information);
                        txtBookingCustomerName.Clear();
                        txtBookingCustomerName.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBookingPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearchBookingCust_Click(sender, e);
            }
        }

        #endregion

        #region NGHIỆP VỤ ĐẶT SÂN & CHECKIN NGAY

        private void btnBookPitch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteBooking(false); // Đặt lịch trước
        }

        private void btnCheckInNow_Click(object sender, RoutedEventArgs e)
        {
            ExecuteBooking(true); // Nhận sân đá luôn
        }

        private void ExecuteBooking(bool isCheckInNow)
        {
            if (currentPitch == null) return;

            string phone = txtBookingPhone.Text.Trim();
            string name = txtBookingCustomerName.Text.Trim();
            DateTime? date = dpBookingDate.SelectedDate;
            string startStr = txtBookingStartTime.Text.Trim();
            string endStr = txtBookingEndTime.Text.Trim();

            // 1. Validation rỗng
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Số điện thoại khách hàng không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBookingPhone.Focus();
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Tên khách hàng không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBookingCustomerName.Focus();
                return;
            }

            if (date == null)
            {
                MessageBox.Show("Vui lòng chọn ngày đặt sân.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validation Định dạng giờ hh:mm
            if (!TimeSpan.TryParse(startStr, out TimeSpan startTime))
            {
                MessageBox.Show("Giờ bắt đầu không đúng định dạng (hh:mm). Ví dụ: 17:30", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBookingStartTime.Focus();
                return;
            }

            if (!TimeSpan.TryParse(endStr, out TimeSpan endTime))
            {
                MessageBox.Show("Giờ kết thúc không đúng định dạng (hh:mm). Ví dụ: 19:00", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBookingEndTime.Focus();
                return;
            }

            if (endTime <= startTime)
            {
                MessageBox.Show("Giờ kết thúc phải sau giờ bắt đầu chơi.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBookingEndTime.Focus();
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // 3. Thuật toán kiểm tra trùng lịch sân bóng (Ràng buộc nghiệp vụ quan trọng)
                    // Lọc các lịch đặt của sân này trong ngày được chọn về bộ nhớ trước để tránh lỗi dịch LINQ của SQLite
                    DateTime targetDate = date.Value.Date;
                    var activeBookings = context.PhieuDatSans
                        .Where(p => p.MaSan == currentPitch.MaSan && 
                                    p.NgayDat == targetDate && 
                                    p.TrangThai != "Đã hủy" && 
                                    p.TrangThai != "Đã thanh toán")
                        .ToList();

                    // Kiểm tra trùng lắp khoảng thời gian trên RAM
                    bool isOverlap = activeBookings.Any(p => 
                        startTime < p.GioKetThuc && 
                        endTime > p.GioBatDau);

                    if (isOverlap)
                    {
                        MessageBox.Show("Sân bóng này đã có lịch đặt trong khoảng thời gian bạn chọn.\nVui lòng chọn thời gian khác hoặc kiểm tra lại.", "Trùng lịch đặt sân", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 4. Tìm hoặc tự động tạo Khách hàng
                    var customer = context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phone);
                    if (customer == null)
                    {
                        customer = new KhachHang { TenKhachHang = name, SoDienThoai = phone };
                        context.KhachHangs.Add(customer);
                        context.SaveChanges(); // Lưu để sinh MaKhachHang
                    }

                    // 5. Tạo phiếu đặt sân
                    var booking = new PhieuDatSan
                    {
                        MaSan = currentPitch.MaSan,
                        MaKhachHang = customer.MaKhachHang,
                        MaNguoiDung = Session.CurrentUser?.MaNguoiDung ?? 1,
                        NgayDat = date.Value,
                        GioBatDau = startTime,
                        GioKetThuc = endTime,
                        NgayTao = DateTime.Now,
                        TrangThai = isCheckInNow ? "Đã nhận sân" : "Đã đặt"
                    };
                    context.PhieuDatSans.Add(booking);

                    // 6. Nếu nhận sân luôn, cập nhật trạng thái sân bóng thành "Đang đá"
                    if (isCheckInNow)
                    {
                        var pitchDb = context.SanBongs.Find(currentPitch.MaSan);
                        if (pitchDb != null)
                        {
                            pitchDb.TrangThai = "Đang đá";
                        }
                    }

                    context.SaveChanges(); // Lưu Booking và cập nhật trạng thái sân

                    // 7. Nếu nhận sân luôn, tự động tạo hóa đơn chưa thanh toán ban đầu
                    if (isCheckInNow)
                    {
                        TimeSpan duration = endTime - startTime;
                        decimal tienSan = (decimal)duration.TotalHours * currentPitch.DonGiaTheoGio;
                        var invoice = new HoaDonThanhToan
                        {
                            MaPhieuDat = booking.MaPhieuDat,
                            NgayThanhToan = DateTime.Now,
                            MaNguoiDung = Session.CurrentUser?.MaNguoiDung ?? 1,
                            TienSan = tienSan,
                            TienDichVu = 0,
                            TongTien = tienSan,
                            TrangThai = "Chưa thanh toán"
                        };
                        context.HoaDonThanhToans.Add(invoice);
                        context.SaveChanges();
                    }

                    MessageBox.Show(isCheckInNow ? "Khách hàng nhận sân bóng thành công!" : "Đặt lịch sân bóng trước thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh sơ đồ sân
                    LoadPitches();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lập lịch đặt sân: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region NGHIỆP VỤ BÁN DỊCH VỤ / SẢN PHẨM

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (currentBill == null) return;

            if (cboAddProduct.SelectedItem is not SanPham selectedProd)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm dịch vụ cần thêm.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string qtyStr = txtAddProductQty.Text.Trim();
            if (!int.TryParse(qtyStr, out int qty) || qty <= 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên lớn hơn 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAddProductQty.Focus();
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // 1. Kiểm tra tồn kho của sản phẩm
                    var prod = context.SanPhams.Find(selectedProd.MaSanPham);
                    if (prod == null) return;

                    // Nếu là mặt hàng giới hạn tồn kho (vd: nước ngọt, mì ly...), kiểm tra tồn
                    // Dịch vụ như thuê giày/áo có thể tồn kho lớn hoặc không ràng buộc khắt khe, nhưng ta check chung
                    if (prod.SoLuongTon < qty)
                    {
                        MessageBox.Show($"Sản phẩm '{prod.TenSanPham}' trong kho chỉ còn {prod.SoLuongTon} {prod.DonViTinh}. Không đủ để bán.", "Thiếu hàng tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 2. Thêm vào chi tiết hóa đơn dịch vụ
                    var existingDetail = context.ChiTietHoaDonDichVus
                        .FirstOrDefault(c => c.MaHoaDon == currentBill.MaHoaDon && c.MaSanPham == selectedProd.MaSanPham);

                    if (existingDetail != null)
                    {
                        existingDetail.SoLuong += qty;
                        existingDetail.ThanhTien = existingDetail.SoLuong * existingDetail.DonGiaBan;
                    }
                    else
                    {
                        var newDetail = new ChiTietHoaDonDichVu
                        {
                            MaHoaDon = currentBill.MaHoaDon,
                            MaSanPham = selectedProd.MaSanPham,
                            SoLuong = qty,
                            DonGiaBan = prod.GiaBan,
                            ThanhTien = qty * prod.GiaBan
                        };
                        context.ChiTietHoaDonDichVus.Add(newDetail);
                    }

                    // Trừ số lượng tồn kho trong kho thực tế
                    prod.SoLuongTon -= qty;

                    context.SaveChanges();

                    // Load lại bảng dịch vụ
                    LoadActiveServices(currentBill.MaHoaDon);
                    txtAddProductQty.Text = "1";
                    cboAddProduct.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm dịch vụ: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveService_Click(object sender, RoutedEventArgs e)
        {
            if (currentBill == null) return;

            if (sender is Button btn && btn.DataContext is ActiveServiceView item)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        var detail = context.ChiTietHoaDonDichVus.Find(item.MaChiTietDV);
                        if (detail != null)
                        {
                            // Trả lại số lượng tồn kho
                            var prod = context.SanPhams.Find(item.MaSanPham);
                            if (prod != null)
                            {
                                prod.SoLuongTon += detail.SoLuong;
                            }

                            context.ChiTietHoaDonDichVus.Remove(detail);
                            context.SaveChanges();
                        }

                        // Load lại
                        LoadActiveServices(currentBill.MaHoaDon);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi trả lại dịch vụ: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtBillDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBillSummary();
        }

        #endregion

        #region NGHIỆP VỤ HỦY SÂN / THANH TOÁN CHECKOUT

        private void btnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (currentBooking == null || currentPitch == null) return;

            var result = MessageBox.Show("Bạn có chắc chắn muốn hủy đặt sân và đưa trạng thái sân về Trống?", "Xác nhận hủy đặt sân", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // Cập nhật trạng thái phiếu đặt thành Đã hủy
                        var booking = context.PhieuDatSans.Find(currentBooking.MaPhieuDat);
                        if (booking != null)
                        {
                            booking.TrangThai = "Đã hủy";
                        }

                        // Hoàn trả lại toàn bộ sản phẩm tồn kho nếu đã gọi dịch vụ nhưng hủy
                        if (currentBill != null)
                        {
                            var details = context.ChiTietHoaDonDichVus.Where(c => c.MaHoaDon == currentBill.MaHoaDon).ToList();
                            foreach (var detail in details)
                            {
                                var prod = context.SanPhams.Find(detail.MaSanPham);
                                if (prod != null)
                                {
                                    prod.SoLuongTon += detail.SoLuong;
                                }
                                context.ChiTietHoaDonDichVus.Remove(detail);
                            }

                            var bill = context.HoaDonThanhToans.Find(currentBill.MaHoaDon);
                            if (bill != null)
                            {
                                context.HoaDonThanhToans.Remove(bill);
                            }
                        }

                        // Cập nhật trạng thái sân bóng thành Trống
                        var pitch = context.SanBongs.Find(currentPitch.MaSan);
                        if (pitch != null)
                        {
                            pitch.TrangThai = "Trống";
                        }

                        context.SaveChanges();
                        MessageBox.Show("Hủy đặt sân bóng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Reset selection
                        currentPitch = null;
                        currentBooking = null;
                        currentBill = null;
                        
                        panelNoSelection.Visibility = Visibility.Visible;
                        gridBookingForm.Visibility = Visibility.Collapsed;
                        gridActiveBill.Visibility = Visibility.Collapsed;
                        panelMaintenance.Visibility = Visibility.Collapsed;

                        LoadPitches();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi hủy đặt sân: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (currentBill == null || currentPitch == null || currentBooking == null) return;

            string discountStr = txtBillDiscount.Text.Trim();
            string surchargeStr = txtBillSurcharge.Text.Trim();

            decimal discount = 0;
            decimal surcharge = 0;

            if (!string.IsNullOrEmpty(discountStr) && (!decimal.TryParse(discountStr, out discount) || discount < 0))
            {
                MessageBox.Show("Giảm giá phải là số hợp lệ lớn hơn hoặc bằng 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBillDiscount.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(surchargeStr) && (!decimal.TryParse(surchargeStr, out surcharge) || surcharge < 0))
            {
                MessageBox.Show("Phụ thu phải là số hợp lệ lớn hơn hoặc bằng 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBillSurcharge.Focus();
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // 1. Tính toán số tiền
                    TimeSpan duration = currentBooking.GioKetThuc - currentBooking.GioBatDau;
                    decimal tienSan = (decimal)duration.TotalHours * currentPitch.DonGiaTheoGio;
                    decimal tienDV = context.ChiTietHoaDonDichVus.Where(c => c.MaHoaDon == currentBill.MaHoaDon).ToList().Sum(c => c.ThanhTien);
                    decimal tongTien = tienSan + tienDV - discount + surcharge;
                    if (tongTien < 0) tongTien = 0;

                    // 2. Cập nhật hóa đơn
                    var bill = context.HoaDonThanhToans.Find(currentBill.MaHoaDon);
                    if (bill != null)
                    {
                        bill.TienSan = tienSan;
                        bill.TienDichVu = tienDV;
                        bill.GiamGia = discount;
                        bill.PhuThu = surcharge;
                        bill.TongTien = tongTien;
                        bill.NgayThanhToan = DateTime.Now;
                        bill.TrangThai = "Đã thanh toán";
                        bill.MaNguoiDung = Session.CurrentUser?.MaNguoiDung ?? 1;
                    }

                    // 3. Cập nhật phiếu đặt sân thành Đã thanh toán
                    var booking = context.PhieuDatSans.Find(currentBooking.MaPhieuDat);
                    if (booking != null)
                    {
                        booking.TrangThai = "Đã thanh toán";
                    }

                    // 4. Cập nhật trạng thái sân bóng thành Trống
                    var pitch = context.SanBongs.Find(currentPitch.MaSan);
                    if (pitch != null)
                    {
                        pitch.TrangThai = "Trống";
                    }

                    context.SaveChanges();

                    MessageBox.Show($"Thanh toán hóa đơn thành công!\nTổng tiền: {tongTien:#,##0} VNĐ", "Thanh toán thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 5. Mở Print Window để in hóa đơn
                    int billId = currentBill.MaHoaDon;
                    PrintInvoice(billId);

                    // Reset panel
                    currentPitch = null;
                    currentBooking = null;
                    currentBill = null;

                    panelNoSelection.Visibility = Visibility.Visible;
                    gridBookingForm.Visibility = Visibility.Collapsed;
                    gridActiveBill.Visibility = Visibility.Collapsed;
                    panelMaintenance.Visibility = Visibility.Collapsed;

                    txtBillDiscount.Text = "0";
                    txtBillSurcharge.Text = "0";

                    LoadPitches();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình thanh toán: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintInvoice(int billId)
        {
            // Mở cửa sổ in ấn
            InvoicePrintWindow printWindow = new InvoicePrintWindow(billId);
            printWindow.Owner = Window.GetWindow(this);
            printWindow.ShowDialog();
        }

        #endregion

        #region BẢO TRÌ SÂN BÓNG

        private void btnEndMaintenance_Click(object sender, RoutedEventArgs e)
        {
            if (currentPitch == null) return;

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    var pitch = context.SanBongs.Find(currentPitch.MaSan);
                    if (pitch != null)
                    {
                        pitch.TrangThai = "Trống";
                        context.SaveChanges();
                        MessageBox.Show("Mở lại sân bóng hoạt động bình thường thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                LoadPitches();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở sân bảo trì: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
