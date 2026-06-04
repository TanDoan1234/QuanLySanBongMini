using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("HoaDonThanhToan")]
    public class HoaDonThanhToan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHoaDon { get; set; }

        [Required]
        public int MaPhieuDat { get; set; }

        [Required]
        public DateTime NgayThanhToan { get; set; } = DateTime.Now;

        [Required]
        public int MaNguoiDung { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TienSan { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TienDichVu { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GiamGia { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PhuThu { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TongTien { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Chưa thanh toán"; // Chưa thanh toán, Đã thanh toán

        [StringLength(255)]
        public string? GhiChu { get; set; }

        [ForeignKey("MaPhieuDat")]
        public virtual PhieuDatSan? PhieuDatSan { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
