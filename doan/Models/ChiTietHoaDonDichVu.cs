using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("ChiTietHoaDonDichVu")]
    public class ChiTietHoaDonDichVu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTietDV { get; set; }

        [Required]
        public int MaHoaDon { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng dịch vụ phải lớn hơn 0.")]
        public int SoLuong { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        public decimal DonGiaBan { get; set; }

        [Required]
        public decimal ThanhTien { get; set; }

        [ForeignKey("MaHoaDon")]
        public virtual HoaDonThanhToan? HoaDonThanhToan { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
