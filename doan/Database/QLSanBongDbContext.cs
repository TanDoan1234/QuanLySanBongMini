using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using doan.Models;

namespace doan.Database
{
    public class QLSanBongDbContext : DbContext
    {
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<SanBong> SanBongs { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<PhieuDatSan> PhieuDatSans { get; set; }
        public DbSet<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<HoaDonNhap> HoaDonNhaps { get; set; }
        public DbSet<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; }
        public DbSet<HoaDonThanhToan> HoaDonThanhToans { get; set; }
        public DbSet<ChiTietHoaDonDichVu> ChiTietHoaDonDichVus { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Cấu hình kết nối SQLite cục bộ
                optionsBuilder.UseSqlite("Data Source=QLSanBongMini.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình khoá ngoại và các quy tắc cascade delete nếu cần thiết
            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(p => p.SanBong)
                .WithMany()
                .HasForeignKey(p => p.MaSan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(p => p.KhachHang)
                .WithMany()
                .HasForeignKey(p => p.MaKhachHang)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(p => p.NguoiDung)
                .WithMany()
                .HasForeignKey(p => p.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(h => h.PhieuDatSan)
                .WithMany()
                .HasForeignKey(h => h.MaPhieuDat)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDonThanhToan>()
                .HasOne(h => h.NguoiDung)
                .WithMany()
                .HasForeignKey(h => h.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietHoaDonDichVu>()
                .HasOne(c => c.HoaDonThanhToan)
                .WithMany()
                .HasForeignKey(c => c.MaHoaDon)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietHoaDonDichVu>()
                .HasOne(c => c.SanPham)
                .WithMany()
                .HasForeignKey(c => c.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietHoaDonNhap>()
                .HasOne(c => c.HoaDonNhap)
                .WithMany()
                .HasForeignKey(c => c.MaHoaDonNhap)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietHoaDonNhap>()
                .HasOne(c => c.SanPham)
                .WithMany()
                .HasForeignKey(c => c.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed dữ liệu mẫu
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Băm mật khẩu mặc định "admin123" và "staff123" bằng SHA256
            string adminPasswordHash = HashPassword("admin123");
            string staffPasswordHash = HashPassword("staff123");

            // Seed Người dùng
            modelBuilder.Entity<NguoiDung>().HasData(
                new NguoiDung { MaNguoiDung = 1, TenDangNhap = "admin", MatKhau = adminPasswordHash, HoTen = "Quản Trị Viên", SoDienThoai = "0987654321", VaiTro = "Admin", TrangThai = true },
                new NguoiDung { MaNguoiDung = 2, TenDangNhap = "nhanvien", MatKhau = staffPasswordHash, HoTen = "Nguyễn Văn Nhân Viên", SoDienThoai = "0123456789", VaiTro = "NhanVien", TrangThai = true }
            );

            // Seed Sân Bóng
            modelBuilder.Entity<SanBong>().HasData(
                new SanBong { MaSan = 1, TenSan = "Sân 5A", LoaiSan = "Sân 5", DonGiaTheoGio = 150000, TrangThai = "Trống" },
                new SanBong { MaSan = 2, TenSan = "Sân 5B", LoaiSan = "Sân 5", DonGiaTheoGio = 150000, TrangThai = "Trống" },
                new SanBong { MaSan = 3, TenSan = "Sân 7A", LoaiSan = "Sân 7", DonGiaTheoGio = 250000, TrangThai = "Trống" },
                new SanBong { MaSan = 4, TenSan = "Sân 7B", LoaiSan = "Sân 7", DonGiaTheoGio = 250000, TrangThai = "Trống" },
                new SanBong { MaSan = 5, TenSan = "Sân 11", LoaiSan = "Sân 11", DonGiaTheoGio = 500000, TrangThai = "Trống" }
            );

            // Seed Khách Hàng
            modelBuilder.Entity<KhachHang>().HasData(
                new KhachHang { MaKhachHang = 1, TenKhachHang = "Nguyễn Anh Tuấn", SoDienThoai = "0909123456", Email = "tuan.nguyen@gmail.com" },
                new KhachHang { MaKhachHang = 2, TenKhachHang = "Trần Thanh Sơn", SoDienThoai = "0918987654", Email = "son.tran@gmail.com" },
                new KhachHang { MaKhachHang = 3, TenKhachHang = "Khách vãng lai", SoDienThoai = "0999999999", Email = null }
            );

            // Seed Danh mục sản phẩm
            modelBuilder.Entity<DanhMucSanPham>().HasData(
                new DanhMucSanPham { MaDanhMuc = 1, TenDanhMuc = "Nước giải khát" },
                new DanhMucSanPham { MaDanhMuc = 2, TenDanhMuc = "Dịch vụ thuê đồ" },
                new DanhMucSanPham { MaDanhMuc = 3, TenDanhMuc = "Đồ ăn nhẹ" }
            );

            // Seed Sản phẩm
            modelBuilder.Entity<SanPham>().HasData(
                new SanPham { MaSanPham = 1, TenSanPham = "Nước suối Aquafina", GiaBan = 10000, SoLuongTon = 100, DonViTinh = "Chai", MaDanhMuc = 1, HinhAnh = "" },
                new SanPham { MaSanPham = 2, TenSanPham = "Nước ngọt Sting đỏ", GiaBan = 12000, SoLuongTon = 50, DonViTinh = "Chai", MaDanhMuc = 1, HinhAnh = "" },
                new SanPham { MaSanPham = 3, TenSanPham = "Thuê giày đá bóng", GiaBan = 30000, SoLuongTon = 20, DonViTinh = "Đôi/Lượt", MaDanhMuc = 2, HinhAnh = "" },
                new SanPham { MaSanPham = 4, TenSanPham = "Thuê áo bóng đá bib", GiaBan = 5000, SoLuongTon = 40, DonViTinh = "Cái/Lượt", MaDanhMuc = 2, HinhAnh = "" },
                new SanPham { MaSanPham = 5, TenSanPham = "Mì tôm ly", GiaBan = 15000, SoLuongTon = 30, DonViTinh = "Ly", MaDanhMuc = 3, HinhAnh = "" }
            );
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
