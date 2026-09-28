using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public class AuthService : IAuthService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IEmailSenderService _emailSenderService;
    private readonly EmailVerificationSettings _emailVerificationSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public AuthService(
        QLTapChiKhoaHocContext context,
        IConfiguration configuration,
        IEmailVerificationService emailVerificationService,
        IEmailSenderService emailSenderService,
        IOptions<EmailVerificationSettings> emailVerificationSettings,
        ILogger<AuthService> logger,
        IHostEnvironment hostEnvironment)
    {
        _context = context;
        _configuration = configuration;
        _emailVerificationService = emailVerificationService;
        _emailSenderService = emailSenderService;
        _emailVerificationSettings = emailVerificationSettings.Value ?? new EmailVerificationSettings();
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var usernameOrEmail = request.UsernameOrEmail.Trim().ToLower();

        var user = await _context.NguoiDungs
            .Include(u => u.NguoiDungVaiTros)
                .ThenInclude(nv => nv.VaiTro)
            .Include(u => u.NguoiDungChuyenMons)
                .ThenInclude(nc => nc.ChuyenNganh)
            .FirstOrDefaultAsync(u =>
                (u.Email != null && u.Email.ToLower() == usernameOrEmail) ||
                (u.TenDangNhap != null && u.TenDangNhap.ToLower() == usernameOrEmail));

        if (user == null || !user.TrangThai)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa."
            };
        }

        bool passwordValid = false;

        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.MatKhau);
        }
        catch
        {
            // Bỏ qua lỗi định dạng salt của hash cũ
        }

        if (!passwordValid && !_hostEnvironment.IsProduction() && user.MatKhau == request.Password)
        {
            passwordValid = true;
            // Chỉ cho phép mật khẩu cũ dạng văn bản trong môi trường phát triển cục bộ.
        }

        if (!passwordValid || (_hostEnvironment.IsProduction() && CommonWeakPasswords.Contains(request.Password.Trim())))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không chính xác."
            };
        }

        var profileDto = MapToProfileDto(user);
        var token = GenerateJwtToken(user, profileDto.VaiTros);

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            Token = token,
            User = profileDto
        };
    }

    // Danh sách mật khẩu phổ biến / quá đơn giản bị cấm
    private static readonly HashSet<string> CommonWeakPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456", "12345678", "123456789", "1234567890", "0123456789",
        "password", "password1", "password123", "pass1234", "pass@123",
        "admin123", "admin1234", "administrator", "root1234",
        "qwerty", "qwertyuiop", "asdfghjk", "zxcvbnm",
        "111111", "11111111", "00000000", "123123", "abc12345",
        "iloveyou", "welcome1", "letmein"
    };

    public static (bool IsValid, string Message) ValidatePasswordStrength(string password, string? username = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Mật khẩu không được để trống.");
        }

        if (password.Length < 8)
        {
            return (false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự để đảm bảo an toàn.");
        }

        if (password.Length > 100)
        {
            return (false, "Mật khẩu không được vượt quá 100 ký tự.");
        }

        if (CommonWeakPasswords.Contains(password.Trim()))
        {
            return (false, "Mật khẩu quá đơn giản và dễ đoán (như 123456, password,...). Vui lòng chọn mật khẩu phức tạp hơn.");
        }

        // Kiểm tra không được trùng tên đăng nhập hoặc email
        if (!string.IsNullOrEmpty(username) && string.Equals(password.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Mật khẩu không được trùng với tên đăng nhập.");
        }

        if (!string.IsNullOrEmpty(email))
        {
            var emailPrefix = email.Split('@')[0].Trim();
            if (!string.IsNullOrEmpty(emailPrefix) && string.Equals(password.Trim(), emailPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Mật khẩu không được trùng với phần tên tài khoản email.");
            }
        }

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (!hasUpper)
        {
            return (false, "Mật khẩu phải chứa ít nhất một chữ cái in hoa (A-Z).");
        }

        if (!hasLower)
        {
            return (false, "Mật khẩu phải chứa ít nhất một chữ cái in thường (a-z).");
        }

        if (!hasDigit && !hasSpecial)
        {
            return (false, "Mật khẩu phải chứa ít nhất một chữ số (0-9) hoặc ký tự đặc biệt (!@#$%...).");
        }

        return (true, string.Empty);
    }

    public static (bool IsValid, string NormalizedPhone, string Message) ValidatePhoneNumberByCountry(string rawPhone, string? country)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return (false, string.Empty, "Vui lòng cung cấp số điện thoại liên lạc.");
        }

        // Loại bỏ các ký tự khoảng trắng, gạch nối, dấu chấm, dấu ngoặc
        string phone = System.Text.RegularExpressions.Regex.Replace(rawPhone.Trim(), @"[\s\-\.\(\)]", "");
        string targetCountry = string.IsNullOrWhiteSpace(country) ? "Vietnam" : country.Trim();

        bool isValid = false;
        string errorMsg = "";

        switch (targetCountry.ToLowerInvariant())
        {
            case "vietnam":
            case "việt nam":
                // Định dạng VN: 10 chữ số bắt đầu bằng 03, 05, 07, 08, 09; hoặc tiền tố +84 / 84
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+84|84)0?|0)(?:3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$");
                errorMsg = "Số điện thoại Việt Nam không hợp lệ. Vui lòng nhập đúng số di động gồm 10 chữ số (bắt đầu bằng 03, 05, 07, 08, 09) hoặc định dạng quốc tế (+84...).";
                break;

            case "united states":
            case "canada":
                // Bắc Mỹ (NANP): 10 chữ số, mã vùng & tổng đài từ 2-9; tiền tố +1 hoặc 1 tùy chọn
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:\+?1)?[2-9]\d{2}[2-9]\d{6}$");
                errorMsg = $"Số điện thoại {targetCountry} không hợp lệ. Vui lòng nhập đúng 10 chữ số hợp lệ theo chuẩn Bắc Mỹ (ví dụ: (202) 555-0123 hoặc +12025550123).";
                break;

            case "united kingdom":
                // Anh: +44 hoặc 0, theo sau là di động 7xxx xxxxxx hoặc cố định 1/2
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+44|0044)0?|0)(?:[12]\d{8,9}|7\d{9})$");
                errorMsg = "Số điện thoại Vương quốc Anh (UK) không hợp lệ (ví dụ: 07911 123456 hoặc +447911123456).";
                break;

            case "australia":
                // Úc: +61 hoặc 0, theo sau là 4xx xxx xxx hoặc cố định 2,3,7,8
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+61|0061)0?|0)(?:4\d{8}|[2378]\d{8})$");
                errorMsg = "Số điện thoại Úc (Australia) không hợp lệ (ví dụ: 0412 345 678 hoặc +61412345678).";
                break;

            case "japan":
                // Nhật: +81 hoặc 0, di động 70/80/90 hoặc cố định
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+81|0081)0?|0)(?:[789]0\d{8}|[1-9]\d{8,9})$");
                errorMsg = "Số điện thoại Nhật Bản không hợp lệ (ví dụ: 090-1234-5678 hoặc +819012345678).";
                break;

            case "south korea":
                // Hàn Quốc: +82 hoặc 0, di động 10/11/16/17/18/19 hoặc cố định
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+82|0082)0?|0)(?:1[016789]\d{7,8}|[2-6]\d{7,8})$");
                errorMsg = "Số điện thoại Hàn Quốc không hợp lệ (ví dụ: 010-1234-5678 hoặc +821012345678).";
                break;

            case "singapore":
                // Singapore: +65 hoặc 8 chữ số bắt đầu 6, 8, 9
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+65|0065)0?)?[689]\d{7}$");
                errorMsg = "Số điện thoại Singapore không hợp lệ. Vui lòng nhập đúng 8 chữ số bắt đầu bằng 6, 8 hoặc 9 (ví dụ: +65 9123 4567).";
                break;

            case "france":
                // Pháp: +33 hoặc 0, theo sau là 9 chữ số 1-9
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+33|0033)0?|0)[1-9]\d{8}$");
                errorMsg = "Số điện thoại Pháp không hợp lệ (ví dụ: 06 12 34 56 78 hoặc +33612345678).";
                break;

            case "germany":
                // Đức: +49 hoặc 0, di động 15/16/17 hoặc cố định
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+49|0049)0?|0)(?:1[567]\d{8,9}|[2-9]\d{5,10})$");
                errorMsg = "Số điện thoại Đức không hợp lệ (ví dụ: 0151 12345678 hoặc +4915112345678).";
                break;

            case "thailand":
                // Thái Lan: +66 hoặc 0, di động 6, 8, 9
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+66|0066)0?|0)[689]\d{8}$");
                errorMsg = "Số điện thoại Thái Lan không hợp lệ (ví dụ: 081 234 5678 hoặc +66812345678).";
                break;

            case "malaysia":
                // Malaysia: +60 hoặc 0, di động 1x
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+60|0060)0?|0)1[0-46-9]\d{7,8}$");
                errorMsg = "Số điện thoại Malaysia không hợp lệ (ví dụ: 012-345 6789 hoặc +60123456789).";
                break;

            case "vatican":
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(?:(?:\+379|\+39|00379|0039)0?|0)?[0-9]{6,11}$");
                errorMsg = "Số điện thoại Vatican không hợp lệ (ví dụ: +379 0669812345 hoặc +39 0669812345).";
                break;

            default:
                // Chuẩn quốc tế E.164: 8-15 chữ số
                isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[1-9]\d{7,14}$");
                errorMsg = "Số điện thoại không hợp lệ theo chuẩn viễn thông quốc tế E.164 (từ 8 đến 15 chữ số).";
                break;
        }

        return (isValid, phone, errorMsg);
    }

    private class NormalizedRegistrationData
    {
        public string NormalizedEmail { get; set; } = null!;
        public string NormalizedUsername { get; set; } = null!;
        public string? HoDem { get; set; }
        public string Ten { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string HocVi { get; set; } = "Không";
        public string HocHam { get; set; } = "Không";
        public string GioiTinh { get; set; } = "Nam";
        public string QuocGia { get; set; } = "Vietnam";
        public string NgonNgu { get; set; } = "Tiếng Việt";
        public string? SoDienThoai { get; set; }
        public string? DonVi { get; set; }
        public string? DiaChi { get; set; }
        public string? SoTaiKhoan { get; set; }
        public string? ChuTaiKhoan { get; set; }
        public string? NganHang { get; set; }
        public string? MaORCID { get; set; }
        // [REMOVED] QualifyReviewer — Vai trò Phản biện do Tổng biên tập phân công qua WinForms
    }

    private async Task<(bool IsValid, string? ErrorMessage, NormalizedRegistrationData? Data)> ValidateAndNormalizeRegistrationAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = string.IsNullOrWhiteSpace(request.TenDangNhap)
            ? email.Split('@')[0]
            : request.TenDangNhap.Trim().ToLowerInvariant();

        // 1. Kiểm tra tính hợp lệ và duy nhất của email trong NguoiDung
        if (await _context.NguoiDungs.AnyAsync(u => u.Email.ToLower() == email))
        {
            return (false, "Email này đã được sử dụng trong hệ thống. Vui lòng chọn một địa chỉ email khác hoặc sử dụng tính năng đăng nhập.", null);
        }

        // 2. Ràng buộc định dạng Tên đăng nhập (3-30 ký tự, chữ cái tiếng Anh, số, ., _, -)
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_.-]{3,30}$"))
        {
            return (false, "Tên đăng nhập không hợp lệ. Tên đăng nhập chỉ được chứa các ký tự chữ cái tiếng Anh (không dấu), số và các ký tự '.', '_', '-' (từ 3 đến 30 ký tự).", null);
        }

        // 3. Ràng buộc tính duy nhất của Tên đăng nhập trong NguoiDung
        if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap != null && u.TenDangNhap.ToLower() == username))
        {
            return (false, "Tên đăng nhập này đã được sử dụng trong hệ thống. Vui lòng chọn một tên đăng nhập khác.", null);
        }

        // 4. Ràng buộc độ an toàn và độ phức tạp của mật khẩu
        var passCheck = ValidatePasswordStrength(request.Password, username, email);
        if (!passCheck.IsValid)
        {
            return (false, passCheck.Message, null);
        }

        // 5. Ràng buộc chéo Học vị ↔ Học hàm (Academic Integrity Constraint)
        var hocVi = string.IsNullOrWhiteSpace(request.HocVi) ? "Không" : request.HocVi.Trim();
        var hocHam = string.IsNullOrWhiteSpace(request.HocHam) ? "Không" : request.HocHam.Trim();
        bool isRankPGSorGS = string.Equals(hocHam, "Phó giáo sư", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(hocHam, "Giáo sư", StringComparison.OrdinalIgnoreCase);
        bool hasDocDegree = string.Equals(hocVi, "Tiến sĩ", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(hocVi, "TSKH", StringComparison.OrdinalIgnoreCase) ||
                            hocVi.Contains("Tiến sĩ", StringComparison.OrdinalIgnoreCase);
        if (isRankPGSorGS && !hasDocDegree)
        {
            return (false, $"Tổ hợp Học hàm \"{hocHam}\" với Học vị \"{hocVi}\" không hợp lệ theo quy chế học thuật của Hội đồng Giáo sư Nhà nước. Học hàm Phó giáo sư / Giáo sư bắt buộc phải có Học vị tối thiểu từ Tiến sĩ trở lên.", null);
        }

        // 6. Ràng buộc định dạng Số điện thoại theo từng quốc gia
        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.SoDienThoai))
        {
            var phoneCheck = ValidatePhoneNumberByCountry(request.SoDienThoai, request.QuocGia);
            if (!phoneCheck.IsValid)
            {
                return (false, phoneCheck.Message, null);
            }
            phone = phoneCheck.NormalizedPhone;
        }

        // 7. Ràng buộc bộ 3 thông tin ngân hàng (nhận thù lao/nhuận bút)
        var stk = string.IsNullOrWhiteSpace(request.SoTaiKhoan) ? null : request.SoTaiKhoan.Trim();
        var ctk = string.IsNullOrWhiteSpace(request.ChuTaiKhoan) ? null : request.ChuTaiKhoan.Trim().ToUpper();
        var nh = string.IsNullOrWhiteSpace(request.NganHang) ? null : request.NganHang.Trim();

        bool hasStk = !string.IsNullOrEmpty(stk);
        bool hasCtk = !string.IsNullOrEmpty(ctk);
        bool hasNh = !string.IsNullOrEmpty(nh);

        if ((hasStk || hasCtk || hasNh) && !(hasStk && hasCtk && hasNh))
        {
            return (false, "Nếu cung cấp thông tin tài khoản ngân hàng nhận thù lao/nhuận bút, vui lòng điền đầy đủ cả 3 mục: Số tài khoản, Chủ tài khoản và Tên ngân hàng.", null);
        }

        if (hasStk)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(stk!, @"^[a-zA-Z0-9]{6,25}$"))
            {
                return (false, "Số tài khoản ngân hàng không hợp lệ (từ 6 đến 25 ký tự chữ và số, không chứa khoảng trắng hoặc ký tự đặc biệt).", null);
            }
        }

        // 8. Ràng buộc và chuẩn hóa mã định danh tác giả ORCID
        string? cleanOrcid = null;
        if (!string.IsNullOrWhiteSpace(request.MaORCID))
        {
            cleanOrcid = request.MaORCID.Trim();
            if (cleanOrcid.StartsWith("https://orcid.org/", StringComparison.OrdinalIgnoreCase))
            {
                cleanOrcid = cleanOrcid.Substring("https://orcid.org/".Length).Trim();
            }
            else if (cleanOrcid.StartsWith("http://orcid.org/", StringComparison.OrdinalIgnoreCase))
            {
                cleanOrcid = cleanOrcid.Substring("http://orcid.org/".Length).Trim();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanOrcid, @"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$"))
            {
                return (false, "Mã ORCID không hợp lệ. Định dạng chuẩn gồm 16 ký tự phân tách bằng dấu gạch ngang (ví dụ: 0000-0002-1825-0097).", null);
            }

            if (await _context.NguoiDungs.AnyAsync(u => u.MaORCID == cleanOrcid))
            {
                return (false, "Mã định danh tác giả ORCID này đã được liên kết với một tài khoản khác trong hệ thống.", null);
            }
        }

        // Tự động phân tách Họ đệm và Tên nếu chưa điền riêng
        var hoTen = request.HoTen.Trim();
        string? hoDem = string.IsNullOrWhiteSpace(request.HoDem) ? null : request.HoDem.Trim();
        string ten = string.IsNullOrWhiteSpace(request.Ten) ? "" : request.Ten.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            var parts = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                ten = parts[^1];
                hoDem = string.Join(' ', parts[..^1]);
            }
            else
            {
                ten = hoTen;
            }
        }


        var data = new NormalizedRegistrationData
        {
            NormalizedEmail = email,
            NormalizedUsername = username,
            HoDem = hoDem,
            Ten = ten,
            HoTen = hoTen,
            HocVi = hocVi,
            HocHam = hocHam,
            GioiTinh = string.IsNullOrWhiteSpace(request.GioiTinh) ? "Nam" : request.GioiTinh.Trim(),
            QuocGia = string.IsNullOrWhiteSpace(request.QuocGia) ? "Vietnam" : request.QuocGia.Trim(),
            NgonNgu = string.IsNullOrWhiteSpace(request.NgonNgu) ? "Tiếng Việt" : request.NgonNgu.Trim(),
            SoDienThoai = phone,
            DonVi = string.IsNullOrWhiteSpace(request.DonVi) ? null : request.DonVi.Trim(),
            DiaChi = string.IsNullOrWhiteSpace(request.DiaChi) ? null : request.DiaChi.Trim(),
            SoTaiKhoan = stk,
            ChuTaiKhoan = ctk,
            NganHang = nh,
            MaORCID = cleanOrcid
        };

        return (true, null, data);
    }

    /// <inheritdoc />
    public async Task<RegisterPendingResponse> RegisterPendingAsync(RegisterRequest request)
    {
        var validation = await ValidateAndNormalizeRegistrationAsync(request);
        if (!validation.IsValid || validation.Data == null)
        {
            return new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                Message = validation.ErrorMessage ?? "Thông tin đăng ký không hợp lệ."
            };
        }

        var data = validation.Data;

        // Vô hiệu hóa bất kỳ hồ sơ chờ nào trước đó cùng email này (chưa xác nhận)
        var existingPending = await _context.DangKyChoXacNhans
            .Where(p => p.EmailSoSanh == data.NormalizedEmail && p.TrangThai == "Pending")
            .ToListAsync();
        foreach (var p in existingPending)
        {
            p.TrangThai = "Cancelled";
        }

        var regId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var pendingExpiryUtc = nowUtc.AddHours(_emailVerificationSettings.PendingProfileExpiryHours);

        var pending = new DangKyChoXacNhan
        {
            MaDangKy = regId,
            EmailGoc = request.Email.Trim(),
            EmailSoSanh = data.NormalizedEmail,
            TenDangNhapSoSanh = data.NormalizedUsername,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 11),
            HoDem = data.HoDem,
            Ten = data.Ten,
            HoTen = data.HoTen,
            HocVi = data.HocVi,
            HocHam = data.HocHam,
            GioiTinh = data.GioiTinh,
            QuocGia = data.QuocGia,
            NgonNgu = data.NgonNgu,
            SoDienThoai = data.SoDienThoai,
            DonVi = data.DonVi,
            DiaChi = data.DiaChi,
            SoTaiKhoan = data.SoTaiKhoan,
            ChuTaiKhoan = data.ChuTaiKhoan,
            NganHang = data.NganHang,
            MaORCID = data.MaORCID,
            ChuyenNganhId = request.ChuyenNganhId,
            DangKyPhanBien = false, // Vai trò Phản biện: chỉ do Tổng biên tập phân công qua WinForms
            TaoLucUtc = nowUtc,
            HetHanHoSoUtc = pendingExpiryUtc,
            TrangThai = "Pending"
        };

        _context.DangKyChoXacNhans.Add(pending);

        // Sinh mã OTP 6 chữ số CSPRNG và băm HMAC-SHA256
        var maId = Guid.NewGuid();
        var (plainCode, hashCode) = _emailVerificationService.GenerateOtp(maId);
        var codeExpiryUtc = nowUtc.AddMinutes(_emailVerificationSettings.OtpExpiryMinutes);

        var codeRecord = new MaXacNhanEmail
        {
            MaId = maId,
            MaDangKy = regId,
            MaHash = hashCode,
            TaoLucUtc = nowUtc,
            HetHanUtc = codeExpiryUtc,
            SoLanNhapSai = 0,
            LanGuiCuoiUtc = nowUtc
        };

        _context.MaXacNhanEmails.Add(codeRecord);
        await _context.SaveChangesAsync();

        // Gửi email xác nhận kèm mẫu thư tòa soạn chuẩn Hallmark
        var delivery = await _emailSenderService.SendVerificationEmailAsync(
            request.Email.Trim(),
            data.HoTen,
            plainCode,
            _emailVerificationSettings.OtpExpiryMinutes,
            regId);

        _logger.LogInformation("Đã khởi tạo đăng ký chờ xác thực cho email {Email} (MaDangKy: {RegId})",
            data.NormalizedEmail, regId);

        return new RegisterPendingResponse
        {
            Success = true,
            RequiresVerification = true,
            EmailSent = delivery.Success,
            RegistrationId = regId,
            MaskedEmail = _emailVerificationService.MaskEmail(request.Email.Trim()),
            ExpiresAtUtc = codeExpiryUtc,
            ResendAfterSeconds = _emailVerificationSettings.ResendCooldownSeconds,
            Message = delivery.Success
                ? "Mã xác nhận 6 chữ số đã được gửi đến hộp thư của bạn. Vui lòng kiểm tra email để hoàn tất đăng ký tài khoản."
                : "Hồ sơ đã được lưu nhưng chưa gửi được mã xác nhận. Vui lòng thử gửi lại sau ít phút."
        };
    }

    /// <inheritdoc />
    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        // 1. Xác thực mã OTP thông qua IEmailVerificationService
        var otpCheck = await _emailVerificationService.ValidateAndConsumeOtpAsync(request.RegistrationId, request.VerificationCode);
        if (!otpCheck.IsValid)
        {
            return new AuthResponse
            {
                Success = false,
                Message = otpCheck.Message,
                RemainingAttempts = otpCheck.RemainingAttempts
            };
        }

        // 2. Lấy hồ sơ chờ tương ứng
        var pending = await _context.DangKyChoXacNhans
            .Include(p => p.MaXacNhanEmails)
            .FirstOrDefaultAsync(p => p.MaDangKy == request.RegistrationId);

        if (pending == null || pending.TrangThai != "Pending")
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Hồ sơ đăng ký không tồn tại hoặc đã được xử lý trước đó."
            };
        }

        // 3. Thực thi Transaction kích hoạt tài khoản chính thức NguoiDung
        int newUserId;
        try
        {
            await using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                // Kiểm tra tương tranh chống race condition
                if (await _context.NguoiDungs.AnyAsync(u => u.Email.ToLower() == pending.EmailSoSanh))
                {
                    await transaction.RollbackAsync();
                    return new AuthResponse { Success = false, Message = "Email này đã được sử dụng trong hệ thống." };
                }

                if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap != null && u.TenDangNhap.ToLower() == pending.TenDangNhapSoSanh))
                {
                    await transaction.RollbackAsync();
                    return new AuthResponse { Success = false, Message = "Tên đăng nhập này đã được sử dụng trong hệ thống." };
                }

                // Đánh dấu mã đã sử dụng
                var activeCode = pending.MaXacNhanEmails.FirstOrDefault(c => c.DaDungLucUtc == null && c.HetHanUtc > DateTime.UtcNow);
                if (activeCode != null)
                {
                    activeCode.DaDungLucUtc = DateTime.UtcNow;
                }

                // Cập nhật trạng thái hồ sơ chờ sang Verified
                pending.TrangThai = "Verified";

                // Tạo tài khoản NguoiDung chính thức
                var user = new NguoiDung
                {
                    TenDangNhap = pending.TenDangNhapSoSanh,
                    HoDem = pending.HoDem,
                    Ten = pending.Ten,
                    HoTen = pending.HoTen,
                    Email = pending.EmailGoc,
                    MatKhau = pending.MatKhauHash,
                    DonVi = pending.DonVi,
                    SoDienThoai = pending.SoDienThoai,
                    HocVi = pending.HocVi,
                    HocHam = pending.HocHam,
                    GioiTinh = pending.GioiTinh,
                    QuocGia = pending.QuocGia,
                    NgonNgu = pending.NgonNgu,
                    DiaChi = pending.DiaChi,
                    SoTaiKhoan = pending.SoTaiKhoan,
                    ChuTaiKhoan = pending.ChuTaiKhoan,
                    NganHang = pending.NganHang,
                    MaORCID = pending.MaORCID,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                _context.NguoiDungs.Add(user);
                await _context.SaveChangesAsync();

                // Gán 2 vai trò mặc định: Tác giả (3) và Độc giả (5)
                _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = user.MaNguoiDung, MaVaiTro = 3 });
                _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = user.MaNguoiDung, MaVaiTro = 5 });

                // Gán lĩnh vực chuyên môn nếu có
                if (pending.ChuyenNganhId.HasValue && pending.ChuyenNganhId > 0)
                {
                    _context.NguoiDungChuyenMons.Add(new NguoiDungChuyenMon
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        MaChuyenNganh = pending.ChuyenNganhId.Value,
                        LaChuyenMonChinh = true,
                        NgayDangKy = DateTime.Now
                    });
                }

                // [REMOVED] Tự động tạo đơn đăng ký phản biện khi xác thực email
                // Vai trò Phản biện viên: chỉ do Tổng biên tập phân công qua WinForms

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                newUserId = user.MaNguoiDung;
            }

            // 4. Đồng bộ các bài báo đồng tác giả trùng khớp email (chỉ thực hiện SAU KHI transaction commit & dispose hoàn tất)
            int soBaiDaLienKet = 0;
            try
            {
                var pMaNguoiDung = new SqlParameter("@MaNguoiDung", newUserId);
                var pSoBaiDaLienKet = new SqlParameter("@SoBaiDaLienKet", System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_DongBoDongTacGia_TheoEmail @MaNguoiDung, @SoBaiDaLienKet OUTPUT",
                    pMaNguoiDung, pSoBaiDaLienKet);

                if (pSoBaiDaLienKet.Value != DBNull.Value && pSoBaiDaLienKet.Value != null)
                {
                    soBaiDaLienKet = (int)pSoBaiDaLienKet.Value;
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, "sp_DongBoDongTacGia_TheoEmail có cảnh báo cho người dùng {UserId}: {Msg}",
                    newUserId, syncEx.Message);
            }

            // Tải lại đối tượng người dùng hoàn chỉnh kèm các quan hệ
            var createdUser = await _context.NguoiDungs
                .AsNoTracking()
                .Include(u => u.NguoiDungVaiTros).ThenInclude(nv => nv.VaiTro)
                .Include(u => u.NguoiDungChuyenMons).ThenInclude(nc => nc.ChuyenNganh)
                .FirstAsync(u => u.MaNguoiDung == newUserId);

            var profileDto = MapToProfileDto(createdUser);
            var token = GenerateJwtToken(createdUser, profileDto.VaiTros);

            return new AuthResponse
            {
                Success = true,
                Message = soBaiDaLienKet > 0
                    ? $"Xác thực email thành công! Hệ thống đã tự động liên kết {soBaiDaLienKet} bài báo đồng tác giả với tài khoản của bạn."
                    : "Xác thực email và kích hoạt tài khoản thành công!",
                Token = token,
                User = profileDto,
                SoBaiDongTacGiaLienKet = soBaiDaLienKet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kích hoạt tài khoản từ hồ sơ {RegId}: {Error}", request.RegistrationId, ex.Message);
            return new AuthResponse
            {
                Success = false,
                Message = "Chưa thể kích hoạt tài khoản. Vui lòng thử lại sau."
            };
        }
    }

    /// <inheritdoc />
    public async Task<RegisterPendingResponse> ResendVerificationAsync(ResendVerificationRequest request)
    {
        var pending = await _context.DangKyChoXacNhans
            .FirstOrDefaultAsync(p => p.MaDangKy == request.RegistrationId);

        if (pending == null)
        {
            return new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                RegistrationId = request.RegistrationId,
                Message = "Hồ sơ đăng ký không tồn tại hoặc đã bị hủy khỏi hệ thống."
            };
        }

        if (pending.TrangThai != "Pending")
        {
            return new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                RegistrationId = request.RegistrationId,
                Message = "Hồ sơ đăng ký này đã được xác thực hoặc không còn trong trạng thái chờ."
            };
        }

        var newCodeResult = await _emailVerificationService.CreateNewVerificationCodeAsync(request.RegistrationId);
        if (!newCodeResult.Success)
        {
            return new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = true,
                RegistrationId = request.RegistrationId,
                ResendAfterSeconds = newCodeResult.ResendAfterSeconds,
                Message = newCodeResult.Message
            };
        }

        // Gửi email mới qua IEmailSenderService
        var delivery = await _emailSenderService.SendVerificationEmailAsync(
            pending.EmailGoc,
            pending.HoTen,
            newCodeResult.PlainCode!,
            _emailVerificationSettings.OtpExpiryMinutes,
            request.RegistrationId);

        return new RegisterPendingResponse
        {
            Success = true,
            RequiresVerification = true,
            EmailSent = delivery.Success,
            RegistrationId = request.RegistrationId,
            MaskedEmail = _emailVerificationService.MaskEmail(pending.EmailGoc),
            ExpiresAtUtc = newCodeResult.ExpiresAtUtc ?? DateTime.UtcNow.AddMinutes(_emailVerificationSettings.OtpExpiryMinutes),
            ResendAfterSeconds = newCodeResult.ResendAfterSeconds,
            Message = delivery.Success
                ? "Mã xác nhận bảo mật mới đã được gửi đến hộp thư của bạn."
                : "Chưa gửi được mã mới. Vui lòng thử lại sau ít phút."
        };
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var pending = await RegisterPendingAsync(request);
        return new AuthResponse
        {
            Success = pending.Success,
            Message = pending.Message
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(int maNguoiDung)
    {
        var user = await _context.NguoiDungs
            .Include(u => u.NguoiDungVaiTros).ThenInclude(nv => nv.VaiTro)
            .Include(u => u.NguoiDungChuyenMons).ThenInclude(nc => nc.ChuyenNganh)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

        return user == null ? null : MapToProfileDto(user);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int maNguoiDung, UpdateProfileRequest request)
    {
        var user = await _context.NguoiDungs
            .Include(u => u.NguoiDungVaiTros).ThenInclude(nv => nv.VaiTro)
            .Include(u => u.NguoiDungChuyenMons).ThenInclude(nc => nc.ChuyenNganh)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

        if (user == null) return null;

        user.HoTen = request.HoTen.Trim();
        user.DonVi = request.DonVi;
        user.DiaChi = request.DiaChi;
        user.SoDienThoai = request.SoDienThoai;
        user.HocVi = request.HocVi;
        user.HocHam = request.HocHam;
        user.GioiTinh = request.GioiTinh;
        user.SoTaiKhoan = request.SoTaiKhoan;
        user.ChuTaiKhoan = request.ChuTaiKhoan;
        user.NganHang = request.NganHang;
        user.MaORCID = request.MaORCID;
        if (request.AnhDaiDien != null)
        {
            user.AnhDaiDien = request.AnhDaiDien;
        }

        if (request.ChuyenMonIds != null)
        {
            // Xóa chuyên môn cũ và cập nhật danh sách mới
            var existingSpecialties = _context.NguoiDungChuyenMons.Where(cm => cm.MaNguoiDung == maNguoiDung);
            _context.NguoiDungChuyenMons.RemoveRange(existingSpecialties);

            bool isFirst = true;
            foreach (var cmId in request.ChuyenMonIds.Distinct())
            {
                _context.NguoiDungChuyenMons.Add(new NguoiDungChuyenMon
                {
                    MaNguoiDung = maNguoiDung,
                    MaChuyenNganh = cmId,
                    LaChuyenMonChinh = isFirst,
                    NgayDangKy = DateTime.Now
                });
                isFirst = false;
            }
        }

        await _context.SaveChangesAsync();
        return MapToProfileDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int maNguoiDung, ChangePasswordRequest request)
    {
        var user = await _context.NguoiDungs.FindAsync(maNguoiDung);
        if (user == null) return false;

        bool isCurrentValid = false;
        try
        {
            isCurrentValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.MatKhau);
        }
        catch
        {
            isCurrentValid = !_hostEnvironment.IsProduction() && user.MatKhau == request.CurrentPassword;
        }

        if (!isCurrentValid) return false;

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ArgumentException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        var passCheck = ValidatePasswordStrength(request.NewPassword, user.TenDangNhap, user.Email);
        if (!passCheck.IsValid)
        {
            throw new ArgumentException(passCheck.Message);
        }

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 11);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserProfileDto?> UploadAvatarAsync(int maNguoiDung, IFormFile file)
    {
        var user = await _context.NguoiDungs
            .Include(u => u.NguoiDungVaiTros).ThenInclude(nv => nv.VaiTro)
            .Include(u => u.NguoiDungChuyenMons).ThenInclude(nc => nc.ChuyenNganh)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

        if (user == null) return null;

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Tệp tin ảnh tải lên không hợp lệ hoặc rỗng.");
        }

        // Giới hạn dung lượng 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("Dung lượng ảnh đại diện không được vượt quá 5MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExts = new HashSet<string> { ".jpg", ".jpeg", ".jfif", ".png", ".webp", ".gif" };
        if (!allowedExts.Contains(ext))
        {
            throw new ArgumentException("Định dạng ảnh không được hỗ trợ. Vui lòng chọn tệp .jpg, .jpeg, .jfif, .png, .webp hoặc .gif.");
        }

        // Tạo thư mục Uploads/avatars
        var currentDir = Directory.GetCurrentDirectory();
        var avatarsFolder = Path.Combine(currentDir, "Uploads", "avatars");
        if (!Directory.Exists(avatarsFolder))
        {
            Directory.CreateDirectory(avatarsFolder);
        }

        var fileName = $"avatar_{maNguoiDung}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(avatarsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Đồng thời copy vào Web/uploads/avatars nếu thư mục Web tồn tại để hỗ trợ cả 2 server
        try
        {
            var webAvatarsFolder = Path.Combine(currentDir, "..", "..", "Web", "uploads", "avatars");
            if (!Directory.Exists(webAvatarsFolder))
            {
                var altWebFolder = Path.Combine(currentDir, "Web", "uploads", "avatars");
                if (Directory.Exists(Path.Combine(currentDir, "Web")))
                {
                    webAvatarsFolder = altWebFolder;
                }
            }
            if (Directory.Exists(Path.GetDirectoryName(webAvatarsFolder)))
            {
                Directory.CreateDirectory(webAvatarsFolder);
                File.Copy(filePath, Path.Combine(webAvatarsFolder, fileName), true);
            }
        }
        catch
        {
            // Bỏ qua lỗi copy phụ trợ
        }

        user.AnhDaiDien = $"/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync();

        return MapToProfileDto(user);
    }

    public async Task<UserProfileDto?> DeleteAvatarAsync(int maNguoiDung)
    {
        var user = await _context.NguoiDungs
            .Include(u => u.NguoiDungVaiTros).ThenInclude(nv => nv.VaiTro)
            .Include(u => u.NguoiDungChuyenMons).ThenInclude(nc => nc.ChuyenNganh)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

        if (user == null) return null;

        user.AnhDaiDien = null;
        await _context.SaveChangesAsync();

        return MapToProfileDto(user);
    }

    private string GenerateJwtToken(NguoiDung user, List<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("Thiếu khóa ký JWT.");
        var issuer = _configuration["Jwt:Issuer"] ?? "HuitJournal";
        var audience = _configuration["Jwt:Audience"] ?? "HuitJournalUsers";
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryInHours"], out var h) ? h : 24;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
            new(ClaimTypes.Name, user.TenDangNhap ?? user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.HoTen)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserProfileDto MapToProfileDto(NguoiDung user)
    {
        return new UserProfileDto
        {
            MaNguoiDung = user.MaNguoiDung,
            TenDangNhap = user.TenDangNhap,
            HoDem = user.HoDem,
            Ten = user.Ten,
            HoTen = user.HoTen,
            Email = user.Email,
            HocVi = user.HocVi,
            HocHam = user.HocHam,
            GioiTinh = user.GioiTinh,
            NgonNgu = user.NgonNgu,
            QuocGia = user.QuocGia,
            SoDienThoai = user.SoDienThoai,
            DonVi = user.DonVi,
            DiaChi = user.DiaChi,
            SoTaiKhoan = user.SoTaiKhoan,
            ChuTaiKhoan = user.ChuTaiKhoan,
            NganHang = user.NganHang,
            MaORCID = user.MaORCID,
            AnhDaiDien = user.AnhDaiDien,
            VaiTros = user.NguoiDungVaiTros.Select(nv => nv.VaiTro.TenVaiTro).ToList(),
            ChuyenMonIds = user.NguoiDungChuyenMons.Select(nc => nc.MaChuyenNganh).ToList(),
            ChuyenMonNames = user.NguoiDungChuyenMons.Select(nc => nc.ChuyenNganh.TenChuyenNganh).ToList()
        };
    }

    /// <summary>
    /// Lấy danh sách các đơn đăng ký phản biện đang chờ Ban biên tập thẩm định
    /// </summary>
    public async Task<List<DonDangKyPhanBienDto>> GetPendingReviewerRegistrationsAsync()
    {
        var list = await _context.DonDangKyPhanBiens
            .AsNoTracking()
            .Where(d => d.TrangThai == "Chờ duyệt")
            .Include(d => d.NguoiDung)
                .ThenInclude(u => u.NguoiDungChuyenMons)
                    .ThenInclude(nc => nc.ChuyenNganh)
            .OrderByDescending(d => d.NgayDangKy)
            .Select(d => new DonDangKyPhanBienDto
            {
                MaDon = d.MaDon,
                MaNguoiDung = d.MaNguoiDung,
                HoTen = d.NguoiDung.HoTen,
                Email = d.NguoiDung.Email,
                DonVi = d.NguoiDung.DonVi,
                HocVi = d.NguoiDung.HocVi,
                HocHam = d.NguoiDung.HocHam,
                MaORCID = d.NguoiDung.MaORCID,
                ChuyenMonNames = d.NguoiDung.NguoiDungChuyenMons.Select(nc => nc.ChuyenNganh.TenChuyenNganh).ToList(),
                NgayDangKy = d.NgayDangKy,
                GhiChu = d.GhiChu,
                TrangThai = d.TrangThai
            })
            .ToListAsync();

        return list;
    }

    /// <summary>
    /// Ban biên tập / Quản trị viên thẩm định và chính thức phê duyệt vai trò Chuyên gia phản biện
    /// maDon: Mã đơn đăng ký (DonDangKyPhanBien) ở trạng thái "Chờ duyệt"
    /// </summary>
    public async Task<(bool Success, string Message)> ApproveReviewerRoleAsync(int maDon, int maEditor)
    {
        // 1. Chỉ tìm theo MaDon để tránh xung đột định danh giữa MaDon và MaNguoiDung
        var don = await _context.DonDangKyPhanBiens
            .Include(d => d.NguoiDung)
                .ThenInclude(u => u.NguoiDungVaiTros)
            .FirstOrDefaultAsync(d => d.MaDon == maDon);

        if (don == null)
        {
            return (false, $"Không tìm thấy đơn đăng ký phản biện với mã #{maDon}.");
        }

        if (!don.TrangThai.Equals("Chờ duyệt", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Đơn đăng ký #{maDon} hiện ở trạng thái '{don.TrangThai}', không phải 'Chờ duyệt'.");
        }

        var user = don.NguoiDung;
        if (user == null)
        {
            return (false, "Không tìm thấy thông tin ứng viên gắn với đơn đăng ký này.");
        }

        var eligibleDegrees = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Thạc sĩ", "Tiến sĩ", "TSKH" };
        var eligibleTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Phó giáo sư", "Giáo sư" };

        bool hasEligibleDegree = eligibleDegrees.Contains(user.HocVi ?? "")
                              || eligibleTitles.Contains(user.HocHam ?? "");

        if (!hasEligibleDegree)
        {
            return (false, "Ứng viên chưa đủ điều kiện học vị (tối thiểu Thạc sĩ) để được cấp quyền phản biện.");
        }

        if (user.NguoiDungVaiTros.Any(v => v.MaVaiTro == 4))
        {
            don.TrangThai = "Đã duyệt";
            don.MaNguoiDuyet = maEditor;
            don.NgayDuyet = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, $"Đơn đăng ký #{maDon} đã được duyệt. Người dùng {user.HoTen} đã có vai trò Chuyên gia phản biện từ trước.");
        }

        // Cập nhật trạng thái đơn
        don.TrangThai = "Đã duyệt";
        don.MaNguoiDuyet = maEditor;
        don.NgayDuyet = DateTime.Now;

        // Cấp vai trò phản biện (Role 4)
        _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro
        {
            MaNguoiDung = user.MaNguoiDung,
            MaVaiTro = 4
        });

        await _context.SaveChangesAsync();

        // Kiểm tra hậu kiểm bảo đảm đúng người nhận vai trò
        var hasRoleNow = await _context.NguoiDungVaiTros
            .AnyAsync(nv => nv.MaNguoiDung == user.MaNguoiDung && nv.MaVaiTro == 4);

        if (!hasRoleNow)
        {
            return (false, "Lỗi kiểm tra hệ thống: Chưa ghi nhận được vai trò phản biện sau khi duyệt.");
        }

        var editor = await _context.NguoiDungs.FindAsync(maEditor);
        return (true, $"Ban biên tập ({editor?.HoTen}) đã thẩm định đơn #{maDon} và chính thức phê duyệt vai trò Chuyên gia phản biện cho {user.HoTen}.");
    }

    /// <summary>
    /// Ban biên tập / Quản trị viên từ chối đơn đăng ký phản biện
    /// maDon: Mã đơn đăng ký (DonDangKyPhanBien) ở trạng thái "Chờ duyệt"
    /// </summary>
    public async Task<(bool Success, string Message)> RejectReviewerRoleAsync(int maDon, int maEditor, string? lyDo = null)
    {
        var don = await _context.DonDangKyPhanBiens
            .Include(d => d.NguoiDung)
            .FirstOrDefaultAsync(d => d.MaDon == maDon);

        if (don == null)
        {
            return (false, $"Không tìm thấy đơn đăng ký phản biện với mã #{maDon}.");
        }

        if (!don.TrangThai.Equals("Chờ duyệt", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Đơn đăng ký #{maDon} đang ở trạng thái '{don.TrangThai}', không thể từ chối.");
        }

        don.TrangThai = "Từ chối";
        don.MaNguoiDuyet = maEditor;
        don.NgayDuyet = DateTime.Now;
        don.LyDoTuChoi = lyDo ?? "Hồ sơ chưa phù hợp với yêu cầu của Hội đồng phản biện hiện tại.";

        await _context.SaveChangesAsync();

        var editor = await _context.NguoiDungs.FindAsync(maEditor);
        return (true, $"Ban biên tập ({editor?.HoTen}) đã từ chối đơn đăng ký #{maDon} của {don.NguoiDung?.HoTen}.");
    }
}
