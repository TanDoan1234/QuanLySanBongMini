using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using doan.Database;
using doan.Models;

namespace doan.Views.UserControls
{
    public partial class UC_SanBong : UserControl
    {
        private int selectedPitchId = 0;

        public UC_SanBong()
        {
            InitializeComponent();
            LoadPitches();
        }

        private void LoadPitches()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    dgPitches.ItemsSource = context.SanBongs.OrderBy(s => s.TenSan).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sân bóng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgPitches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPitches.SelectedItem is SanBong pitch)
            {
                selectedPitchId = pitch.MaSan;
                txtPitchName.Text = pitch.TenSan;
                txtPitchPrice.Text = pitch.DonGiaTheoGio.ToString("F0");
                
                // Chọn ComboBox Loại sân
                foreach (ComboBoxItem item in cboPitchType.Items)
                {
                    if (item.Content.ToString() == pitch.LoaiSan)
                    {
                        cboPitchType.SelectedItem = item;
                        break;
                    }
                }

                // Chọn ComboBox Trạng thái
                foreach (ComboBoxItem item in cboPitchStatus.Items)
                {
                    if (item.Content.ToString() == pitch.TrangThai)
                    {
                        cboPitchStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void btnNewPitch_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            selectedPitchId = 0;
            txtPitchName.Clear();
            txtPitchPrice.Clear();
            cboPitchType.SelectedIndex = 0; // Mặc định Sân 5
            cboPitchStatus.SelectedIndex = 0; // Mặc định Trống
            dgPitches.SelectedIndex = -1;
        }

        private void btnSavePitch_Click(object sender, RoutedEventArgs e)
        {
            string name = txtPitchName.Text.Trim();
            string priceStr = txtPitchPrice.Text.Trim();

            // 1. Validation rỗng
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Tên sân bóng không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPitchName.Focus();
                return;
            }

            if (cboPitchType.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại sân bóng.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cboPitchStatus.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn trạng thái sân bóng.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validation đơn giá
            if (!decimal.TryParse(priceStr, out decimal price) || price < 0)
            {
                MessageBox.Show("Đơn giá giờ chơi phải là số hợp lệ và lớn hơn hoặc bằng 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPitchPrice.Focus();
                return;
            }

            string type = ((ComboBoxItem)cboPitchType.SelectedItem).Content.ToString()!;
            string status = ((ComboBoxItem)cboPitchStatus.SelectedItem).Content.ToString()!;

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Kiểm tra trùng tên sân
                    bool isDuplicate = context.SanBongs.Any(s => s.TenSan == name && s.MaSan != selectedPitchId);
                    if (isDuplicate)
                    {
                        MessageBox.Show("Tên sân bóng này đã tồn tại trong hệ thống.", "Trùng lặp dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (selectedPitchId == 0) // Thêm mới
                    {
                        var newPitch = new SanBong
                        {
                            TenSan = name,
                            LoaiSan = type,
                            DonGiaTheoGio = price,
                            TrangThai = status
                        };
                        context.SanBongs.Add(newPitch);
                        MessageBox.Show("Thêm mới sân bóng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // Cập nhật
                    {
                        var editPitch = context.SanBongs.Find(selectedPitchId);
                        if (editPitch != null)
                        {
                            editPitch.TenSan = name;
                            editPitch.LoaiSan = type;
                            editPitch.DonGiaTheoGio = price;
                            editPitch.TrangThai = status;
                            MessageBox.Show("Cập nhật thông tin sân bóng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    context.SaveChanges();
                }

                ClearForm();
                LoadPitches();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeletePitch_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPitchId == 0) return;

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa sân bóng này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // Kiểm tra ràng buộc khoá ngoại trước khi xóa
                        bool hasBookings = context.PhieuDatSans.Any(p => p.MaSan == selectedPitchId);
                        if (hasBookings)
                        {
                            MessageBox.Show("Không thể xóa sân bóng này vì đang có lịch sử đặt sân liên quan.", "Lỗi ràng buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var pitch = context.SanBongs.Find(selectedPitchId);
                        if (pitch != null)
                        {
                            context.SanBongs.Remove(pitch);
                            context.SaveChanges();
                            MessageBox.Show("Xóa sân bóng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    ClearForm();
                    LoadPitches();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
