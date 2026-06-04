using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("HoaDonNhap")]
    public class HoaDonNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHoaDonNhap { get; set; }

        [Required]
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [Required]
        public int MaNguoiDung { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TongTien { get; set; } = 0;

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
