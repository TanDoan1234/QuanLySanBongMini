using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSanPham { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không quá 100 ký tự.")]
        public string TenSanPham { get; set; } = null!;

        [Required(ErrorMessage = "Đơn giá bán không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal GiaBan { get; set; }

        [Required(ErrorMessage = "Số lượng tồn không được để trống.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0.")]
        public int SoLuongTon { get; set; }

        [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
        [StringLength(50)]
        public string DonViTinh { get; set; } = "Chai"; // Chai, Lon, Đôi, Bộ, Lượt...

        [StringLength(255)]
        public string? HinhAnh { get; set; } // Đường dẫn file hình ảnh

        [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm.")]
        public int MaDanhMuc { get; set; }

        [ForeignKey("MaDanhMuc")]
        public virtual DanhMucSanPham? DanhMucSanPham { get; set; }
    }
}
