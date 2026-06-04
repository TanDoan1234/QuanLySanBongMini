using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doan.Models
{
    [Table("PhieuDatSan")]
    public class PhieuDatSan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhieuDat { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sân bóng.")]
        public int MaSan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        public int MaKhachHang { get; set; }

        [Required(ErrorMessage = "Người lập phiếu không xác định.")]
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Ngày đặt sân không được để trống.")]
        [DataType(DataType.Date)]
        public DateTime NgayDat { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Giờ bắt đầu không được để trống.")]
        public TimeSpan GioBatDau { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc không được để trống.")]
        public TimeSpan GioKetThuc { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Đã đặt"; // Đã đặt, Đã nhận sân, Đã hủy, Đã thanh toán

        [ForeignKey("MaSan")]
        public virtual SanBong? SanBong { get; set; }

        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
