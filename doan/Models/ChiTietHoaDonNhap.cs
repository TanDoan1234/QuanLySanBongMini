using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("ChiTietHoaDonNhap")]
    public class ChiTietHoaDonNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTietNhap { get; set; }

        [Required]
        public int MaHoaDonNhap { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
        public int SoLuong { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá nhập phải lớn hơn hoặc bằng 0.")]
        public decimal DonGiaNhap { get; set; }

        [Required]
        public decimal ThanhTien { get; set; }

        [ForeignKey("MaHoaDonNhap")]
        public virtual HoaDonNhap? HoaDonNhap { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
