using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using doan.Database;
using doan.Models;

namespace doan.Views.UserControls
{
    public partial class UC_SanPham : UserControl
    {
        private int selectedCategoryId = 0;
        private int selectedProductId = 0;
        private string? selectedImagePath = null;

        public UC_SanPham()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        #region LOADS DỮ LIỆU

        private void LoadCategories()
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    var categories = context.DanhMucSanPhams.OrderBy(d => d.TenDanhMuc).ToList();
                    lstCategories.ItemsSource = categories;

                    // Load lên combobox sản phẩm
                    cboProductCategory.ItemsSource = categories;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts(string? keyword = null)
        {
            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    var query = context.SanPhams.Include(s => s.DanhMucSanPham).AsQueryable();

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(s => s.TenSanPham.Contains(keyword) || 
                                                 s.DanhMucSanPham!.TenDanhMuc.Contains(keyword));
                    }
                    else if (lstCategories.SelectedItem is DanhMucSanPham selectedCat)
                    {
                        query = query.Where(s => s.MaDanhMuc == selectedCat.MaDanhMuc);
                    }

                    dgProducts.ItemsSource = query.OrderBy(s => s.TenSanPham).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region XỬ LÝ DANH MỤC

        private void lstCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCategories.SelectedItem is DanhMucSanPham cat)
            {
                selectedCategoryId = cat.MaDanhMuc;
                txtCategoryName.Text = cat.TenDanhMuc;
                btnDeleteCategory.IsEnabled = true;
                LoadProducts(); // Lọc sản phẩm theo danh mục
            }
            else
            {
                selectedCategoryId = 0;
                txtCategoryName.Clear();
                btnDeleteCategory.IsEnabled = false;
            }
        }

        private void btnSaveCategory_Click(object sender, RoutedEventArgs e)
        {
            string catName = txtCategoryName.Text.Trim();

            // Validation danh mục
            if (string.IsNullOrEmpty(catName))
            {
                MessageBox.Show("Tên danh mục không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCategoryName.Focus();
                return;
            }

            try
            {
                using (var context = new QLSanBongDbContext())
                {
                    // Kiểm tra trùng tên danh mục
                    var isDuplicate = context.DanhMucSanPhams.Any(d => d.TenDanhMuc == catName && d.MaDanhMuc != selectedCategoryId);
                    if (isDuplicate)
                    {
                        MessageBox.Show("Tên danh mục này đã tồn tại.", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (selectedCategoryId == 0) // Thêm mới
                    {
                        var newCat = new DanhMucSanPham { TenDanhMuc = catName };
                        context.DanhMucSanPhams.Add(newCat);
                        MessageBox.Show("Tạo danh mục mới thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // Cập nhật
                    {
                        var editCat = context.DanhMucSanPhams.Find(selectedCategoryId);
                        if (editCat != null)
                        {
                            editCat.TenDanhMuc = catName;
                            MessageBox.Show("Cập nhật danh mục thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    context.SaveChanges();
                }

                txtCategoryName.Clear();
                selectedCategoryId = 0;
                LoadCategories();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCategoryId == 0) return;

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa danh mục này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // Kiểm tra xem có sản phẩm thuộc danh mục này không
                        bool hasProducts = context.SanPhams.Any(s => s.MaDanhMuc == selectedCategoryId);
                        if (hasProducts)
                        {
                            MessageBox.Show("Không thể xóa danh mục này vì đã có sản phẩm thuộc danh mục.", "Lỗi ràng buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var cat = context.DanhMucSanPhams.Find(selectedCategoryId);
                        if (cat != null)
                        {
                            context.DanhMucSanPhams.Remove(cat);
                            context.SaveChanges();
                            MessageBox.Show("Xóa danh mục thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    txtCategoryName.Clear();
                    selectedCategoryId = 0;
                    LoadCategories();
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region XỬ LÝ SẢN PHẨM

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProducts.SelectedItem is SanPham prod)
            {
                selectedProductId = prod.MaSanPham;
                txtProductName.Text = prod.TenSanPham;
                cboProductCategory.SelectedValue = prod.MaDanhMuc;
                txtProductUnit.Text = prod.DonViTinh;
                txtProductPrice.Text = prod.GiaBan.ToString("F0");
                txtProductStock.Text = prod.SoLuongTon.ToString();
                
                // Hiển thị ảnh
                selectedImagePath = prod.HinhAnh;
                DisplayProductImage(prod.HinhAnh);
            }
        }

        private void DisplayProductImage(string? imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    imgProduct.Source = null;
                    panelUploadHint.Visibility = Visibility.Visible;
                }
                else
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();
                    imgProduct.Source = bitmap;
                    panelUploadHint.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                imgProduct.Source = null;
                panelUploadHint.Visibility = Visibility.Visible;
            }
        }

        private void panelUploadHint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.jpg; *.jpeg; *.png; *.gif)|*.jpg;*.jpeg;*.png;*.gif";
            if (ofd.ShowDialog() == true)
            {
                selectedImagePath = ofd.FileName;
                DisplayProductImage(selectedImagePath);
            }
        }

        private void btnNewProduct_Click(object sender, RoutedEventArgs e)
        {
            ClearProductForm();
        }

        private void ClearProductForm()
        {
            selectedProductId = 0;
            txtProductName.Clear();
            cboProductCategory.SelectedIndex = -1;
            txtProductUnit.Text = "Chai";
            txtProductPrice.Clear();
            txtProductStock.Text = "0";
            selectedImagePath = null;
            imgProduct.Source = null;
            panelUploadHint.Visibility = Visibility.Visible;
            dgProducts.SelectedIndex = -1;
        }

        private void btnSaveProduct_Click(object sender, RoutedEventArgs e)
        {
            string prodName = txtProductName.Text.Trim();
            string prodUnit = txtProductUnit.Text.Trim();
            string priceStr = txtProductPrice.Text.Trim();

            // 1. Validation rỗng
            if (string.IsNullOrEmpty(prodName))
            {
                MessageBox.Show("Tên sản phẩm không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProductName.Focus();
                return;
            }

            if (cboProductCategory.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục sản phẩm.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                cboProductCategory.Focus();
                return;
            }

            if (string.IsNullOrEmpty(prodUnit))
            {
                MessageBox.Show("Đơn vị tính không được để trống.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProductUnit.Focus();
                return;
            }

            // 2. Validation giá trị số
            if (!decimal.TryParse(priceStr, out decimal price) || price < 0)
            {
                MessageBox.Show("Đơn giá bán phải là số hợp lệ và lớn hơn hoặc bằng 0.", "Ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProductPrice.Focus();
                return;
            }

            try
            {
                // Xử lý hình ảnh (Copy ảnh vào thư mục dự án)
                string? finalImagePath = null;
                if (!string.IsNullOrEmpty(selectedImagePath) && File.Exists(selectedImagePath))
                {
                    // Nếu là file bên ngoài dự án, copy vào thư mục debug/images
                    string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    if (!selectedImagePath.Contains(targetFolder))
                    {
                        string ext = Path.GetExtension(selectedImagePath);
                        string fileName = $"prod_{Guid.NewGuid()}{ext}";
                        string destPath = Path.Combine(targetFolder, fileName);
                        File.Copy(selectedImagePath, destPath, true);
                        finalImagePath = destPath;
                    }
                    else
                    {
                        finalImagePath = selectedImagePath;
                    }
                }

                using (var context = new QLSanBongDbContext())
                {
                    int catId = (int)cboProductCategory.SelectedValue;

                    if (selectedProductId == 0) // Thêm mới
                    {
                        var newProd = new SanPham
                        {
                            TenSanPham = prodName,
                            MaDanhMuc = catId,
                            DonViTinh = prodUnit,
                            GiaBan = price,
                            SoLuongTon = 0, // Mặc định = 0, sẽ cộng dồn khi nhập hàng
                            HinhAnh = finalImagePath
                        };
                        context.SanPhams.Add(newProd);
                        MessageBox.Show("Thêm mới sản phẩm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // Cập nhật
                    {
                        var editProd = context.SanPhams.Find(selectedProductId);
                        if (editProd != null)
                        {
                            editProd.TenSanPham = prodName;
                            editProd.MaDanhMuc = catId;
                            editProd.DonViTinh = prodUnit;
                            editProd.GiaBan = price;
                            if (finalImagePath != null)
                            {
                                editProd.HinhAnh = finalImagePath;
                            }
                            MessageBox.Show("Cập nhật thông tin sản phẩm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    context.SaveChanges();
                }

                ClearProductForm();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProductId == 0) return;

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa sản phẩm này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new QLSanBongDbContext())
                    {
                        // Kiểm tra xem sản phẩm có được liên kết với chi tiết hóa đơn nhập/bán nào không
                        bool isLinkedToSale = context.ChiTietHoaDonDichVus.Any(c => c.MaSanPham == selectedProductId);
                        bool isLinkedToImport = context.ChiTietHoaDonNhaps.Any(c => c.MaSanPham == selectedProductId);

                        if (isLinkedToSale || isLinkedToImport)
                        {
                            MessageBox.Show("Không thể xóa sản phẩm này vì đã phát sinh lịch sử nhập/bán trong hóa đơn.", "Lỗi ràng buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var prod = context.SanPhams.Find(selectedProductId);
                        if (prod != null)
                        {
                            context.SanPhams.Remove(prod);
                            context.SaveChanges();
                            MessageBox.Show("Xóa sản phẩm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    ClearProductForm();
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region TÌM KIẾM SẢN PHẨM

        private void txtSearchProduct_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Tìm kiếm trực tiếp khi gõ chữ (Yêu cầu xử lý rỗng: nếu ô rỗng, load lại toàn bộ)
            string keyword = txtSearchProduct.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadProducts();
            }
        }

        private void btnSearchProduct_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearchProduct.Text.Trim();
            LoadProducts(keyword);
        }

        #endregion
    }
}
