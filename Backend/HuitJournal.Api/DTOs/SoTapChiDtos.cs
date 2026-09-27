namespace HuitJournal.Api.DTOs;

public class SoTapChiListItemDto
{
    public int MaSoTapChi { get; set; }
    public string TenSo { get; set; } = null!;
    public int Tap { get; set; }
    public int So { get; set; }
    public int Nam { get; set; }
    public DateTime? NgayPhatHanh { get; set; }
    public string TrangThai { get; set; } = null!;
    public int TongSoBaiBao { get; set; }
    public string? AnhBiaUrl { get; set; }
}

public class SoTapChiDetailDto
{
    public int MaSoTapChi { get; set; }
    public string TenSo { get; set; } = null!;
    public int Tap { get; set; }
    public int So { get; set; }
    public int Nam { get; set; }
    public DateTime? NgayPhatHanh { get; set; }
    public string TrangThai { get; set; } = null!;
    public string? AnhBiaUrl { get; set; }
    public string? MoTa { get; set; }

    public List<BaiBaoPublicDto> DanhSachBaiBao { get; set; } = new();
}
