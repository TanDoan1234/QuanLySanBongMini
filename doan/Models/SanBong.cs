using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("SanBong")]
    public class SanBong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSan { get; set; }

        [Required(ErrorMessage = "Tên sân không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên sân không quá 100 ký tự.")]
        public string TenSan { get; set; } = null!;

        [Required(ErrorMessage = "Loại sân không được để trống.")]
        [StringLength(20)]
        public string LoaiSan { get; set; } = "Sân 5"; // Sân 5, Sân 7, Sân 11

        [Required(ErrorMessage = "Đơn giá không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        public decimal DonGiaTheoGio { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Trống"; // Trống, Đang sửa chữa, Bảo trì
    }
}
