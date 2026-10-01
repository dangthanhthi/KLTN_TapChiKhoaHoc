using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Data;

public class QLTapChiKhoaHocContext : DbContext
{
    public QLTapChiKhoaHocContext(DbContextOptions<QLTapChiKhoaHocContext> options) : base(options)
    {
    }

    public virtual DbSet<VaiTro> VaiTros { get; set; } = null!;
    public virtual DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
    public virtual DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = null!;
    public virtual DbSet<ChuyenNganh> ChuyenNganhs { get; set; } = null!;
    public virtual DbSet<NguoiDungChuyenMon> NguoiDungChuyenMons { get; set; } = null!;
    public virtual DbSet<SoTapChi> SoTapChis { get; set; } = null!;
    public virtual DbSet<BaiBao> BaiBaos { get; set; } = null!;
    public virtual DbSet<DongTacGia> DongTacGias { get; set; } = null!;
    public virtual DbSet<ThuMucBaiBao> ThuMucBaiBaos { get; set; } = null!;
    public virtual DbSet<PhanCongPhanBien> PhanCongPhanBiens { get; set; } = null!;
    public virtual DbSet<PhieuDanhGia> PhieuDanhGias { get; set; } = null!;
    public virtual DbSet<PhieuDanhGiaBanNhap> PhieuDanhGiaBanNhaps { get; set; } = null!;
    public virtual DbSet<PhanBienDeXuat> PhanBienDeXuats { get; set; } = null!;
    public virtual DbSet<LichSuTrangThaiBaiBao> LichSuTrangThais { get; set; } = null!;
    public virtual DbSet<DonDangKyPhanBien> DonDangKyPhanBiens { get; set; } = null!;
    public virtual DbSet<DangKyChoXacNhan> DangKyChoXacNhans { get; set; } = null!;
    public virtual DbSet<MaXacNhanEmail> MaXacNhanEmails { get; set; } = null!;
    public virtual DbSet<EmailOutbox> EmailOutboxes { get; set; } = null!;

    public DbSet<WorkflowRecord> WorkflowRecords { get; set; } = null!;
    public DbSet<WorkflowFile> WorkflowFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WorkflowRecord>().HasIndex(r => new { r.Kind, r.UserId, r.State });
        modelBuilder.Entity<WorkflowRecord>().HasIndex(r => new { r.ArticleId, r.Kind });
        modelBuilder.Entity<WorkflowFile>().HasOne(f => f.Record).WithMany().HasForeignKey(f => f.RecordId).OnDelete(DeleteBehavior.Cascade);

        // Khóa chính phức hợp NguoiDung_VaiTro
        modelBuilder.Entity<NguoiDungVaiTro>(entity =>
        {
            entity.HasKey(e => new { e.MaNguoiDung, e.MaVaiTro });

            entity.HasOne(e => e.NguoiDung)
                .WithMany(u => u.NguoiDungVaiTros)
                .HasForeignKey(e => e.MaNguoiDung)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VaiTro)
                .WithMany(r => r.NguoiDungVaiTros)
                .HasForeignKey(e => e.MaVaiTro)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Khóa chính phức hợp NguoiDung_ChuyenMon
        modelBuilder.Entity<NguoiDungChuyenMon>(entity =>
        {
            entity.HasKey(e => new { e.MaNguoiDung, e.MaChuyenNganh });

            entity.HasOne(e => e.NguoiDung)
                .WithMany(u => u.NguoiDungChuyenMons)
                .HasForeignKey(e => e.MaNguoiDung)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChuyenNganh)
                .WithMany(c => c.NguoiDungChuyenMons)
                .HasForeignKey(e => e.MaChuyenNganh)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ràng buộc duy nhất
        modelBuilder.Entity<NguoiDung>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<NguoiDung>()
            .HasIndex(u => u.TenDangNhap)
            .IsUnique();

        modelBuilder.Entity<SoTapChi>()
            .HasIndex(s => new { s.Tap, s.So, s.Nam })
            .IsUnique();

        modelBuilder.Entity<DongTacGia>()
            .HasIndex(d => new { d.MaBaiBao, d.ThuTu })
            .IsUnique();

        modelBuilder.Entity<DongTacGia>()
            .HasIndex(d => new { d.MaBaiBao, d.Email })
            .IsUnique();

        modelBuilder.Entity<PhanCongPhanBien>()
            .HasIndex(p => new { p.MaBaiBao, p.MaNguoiDung, p.SoVong })
            .IsUnique();

        modelBuilder.Entity<PhieuDanhGiaBanNhap>(entity =>
        {
            entity.HasKey(e => e.MaPhanCong);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(e => e.PhanCongPhanBien)
                .WithOne(p => p.PhieuDanhGiaBanNhap)
                .HasForeignKey<PhieuDanhGiaBanNhap>(e => e.MaPhanCong)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Báo cho EF Core biết các bảng có Database Trigger để tránh xung đột OUTPUT clause (Lỗi SQL 334)
        modelBuilder.Entity<BaiBao>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TRG_BaiBao_NgayCapNhat"));
        });

        modelBuilder.Entity<PhanCongPhanBien>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TRG_PhanCongPhanBien_KiemTraChuyenMon"));
        });

        modelBuilder.Entity<DonDangKyPhanBien>(entity =>
        {
            entity.HasOne(e => e.NguoiDung)
                .WithMany(u => u.DonDangKyPhanBiens)
                .HasForeignKey(e => e.MaNguoiDung)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.NguoiDuyet)
                .WithMany()
                .HasForeignKey(e => e.MaNguoiDuyet)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DangKyChoXacNhan>(entity =>
        {
            entity.HasIndex(e => e.EmailSoSanh);
            entity.HasIndex(e => e.TenDangNhapSoSanh);
            entity.HasIndex(e => new { e.TrangThai, e.HetHanHoSoUtc });
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<MaXacNhanEmail>(entity =>
        {
            entity.HasIndex(e => e.MaDangKy);
            entity.HasOne(e => e.DangKyChoXacNhan)
                .WithMany(d => d.MaXacNhanEmails)
                .HasForeignKey(e => e.MaDangKy)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOutbox>(entity =>
        {
            entity.HasIndex(e => new { e.TrangThai, e.TaoLucUtc });
        });
    }
}
