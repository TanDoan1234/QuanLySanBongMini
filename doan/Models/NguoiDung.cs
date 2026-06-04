using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("NguoiDung")]
    public class NguoiDung
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(256)]
        public string MatKhau { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = null!;

        [StringLength(15, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string? SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống.")]
        [StringLength(20)]
        public string VaiTro { get; set; } = "NhanVien"; // Admin hoặc NhanVien

        public bool TrangThai { get; set; } = true;
    }
}
