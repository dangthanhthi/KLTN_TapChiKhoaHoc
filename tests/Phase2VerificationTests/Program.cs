using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=======================================================================");
Console.WriteLine("  BỘ KIỂM THỬ GIAI ĐOẠN 2: DỊCH VỤ XÁC NHẬN EMAIL (PHASE 2 VERIFICATION)");
Console.WriteLine("  Hệ thống Tạp chí Khoa học Đại học Công Thương TP.HCM (HUIT)");
Console.WriteLine("=======================================================================\n");

int passedTests = 0;
int totalTests = 0;

void AssertTrue(bool condition, string testName, string? detail = null)
{
    totalTests++;
    if (condition)
    {
        passedTests++;
        Console.WriteLine($"  [PASS] {testName}");
        if (!string.IsNullOrEmpty(detail))
        {
            Console.WriteLine($"         Chi tiết: {detail}");
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [FAIL] {testName}");
        if (!string.IsNullOrEmpty(detail))
        {
            Console.WriteLine($"         Chi tiết lỗi: {detail}");
        }
        Console.ResetColor();
        throw new Exception($"Test thất bại: {testName}");
    }
}

// -----------------------------------------------------------------------------
// 1. Cấu hình dịch vụ thử nghiệm
// -----------------------------------------------------------------------------
var settings = new EmailVerificationSettings
{
    HmacSecretKey = "TestHmacSecretKey2026!#VerySecureForTestingPurpose9999",
    OtpExpiryMinutes = 10,
    PendingProfileExpiryHours = 24,
    MaxFailedAttempts = 5,
    ResendCooldownSeconds = 60,
    MaxResendsPerHour = 3,
    MaxResendsPerDay = 10,
    SenderEmail = "journal@huit.edu.vn",
    SenderDisplayName = "Tạp chí Khoa học Đại học Công Thương TP.HCM",
    SimulateDeliveryInDev = true
};
var options = Options.Create(settings);

var connectionString = "Server=.;Database=QL_TapChiKhoaHoc_Test;Integrated Security=True;TrustServerCertificate=True;";
var dbOptions = new DbContextOptionsBuilder<QLTapChiKhoaHocContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new QLTapChiKhoaHocContext(dbOptions);
var verificationService = new EmailVerificationService(context, options, NullLogger<EmailVerificationService>.Instance);
var senderService = new EmailSenderService(context, options, NullLogger<EmailSenderService>.Instance);

Console.WriteLine("--- NHÓM 1: KIỂM THỬ THUẬT TOÁN SINH MÃ CSPRNG & HMAC-SHA256 ---");

// Test 1.1: Sinh mã 6 số đúng chuẩn
var sampleMaId = Guid.NewGuid();
var (plainCode, hashCode) = verificationService.GenerateOtp(sampleMaId);
AssertTrue(plainCode.Length == 6, "Mã OTP có độ dài đúng 6 ký tự");
AssertTrue(int.TryParse(plainCode, out int numericVal) && numericVal >= 0 && numericVal <= 999999,
    "Mã OTP là số nằm trong khoảng 000000 - 999999");
AssertTrue(!string.IsNullOrWhiteSpace(hashCode) && hashCode.Length == 64,
    "Chuỗi băm HMAC-SHA256 có độ dài đúng 64 ký tự hex");

// Test 1.2: Sinh 1.000 mã ngẫu nhiên để kiểm tra tính duy nhất và bảo toàn số 0 ở đầu
bool foundLeadingZero = false;
var generatedSet = new HashSet<string>();
for (int i = 0; i < 1000; i++)
{
    var (code, _) = verificationService.GenerateOtp(Guid.NewGuid());
    if (code.Length != 6) throw new Exception($"Mã {code} không đủ 6 chữ số!");
    if (code.StartsWith("0")) foundLeadingZero = true;
    generatedSet.Add(code);
}
AssertTrue(generatedSet.Count > 950, "CSPRNG sinh mã phân tán ngẫu nhiên cao (không bị trùng lặp rập khuôn)",
    $"Sinh 1000 mã được {generatedSet.Count} mã duy nhất");
AssertTrue(foundLeadingZero, "CSPRNG bảo toàn số 0 ở đầu (định dạng D6 chuẩn)", "Đã tìm thấy mã có số 0 ở đầu");

// Test 1.3: Tính tất định của HMAC-SHA256 và so sánh hash thời gian cố định
var fixedId = Guid.NewGuid();
var hash1 = verificationService.ComputeHmacHash(fixedId, "048291");
var hash2 = verificationService.ComputeHmacHash(fixedId, "048291");
AssertTrue(hash1 == hash2, "HMAC-SHA256 có tính tất định cho cùng MaId + Code + SecretKey");

AssertTrue(verificationService.VerifyHash(fixedId, "048291", hash1), "VerifyHash trả về TRUE cho mã khớp chính xác");
AssertTrue(!verificationService.VerifyHash(fixedId, "048292", hash1), "VerifyHash trả về FALSE cho mã sai 1 chữ số");
AssertTrue(!verificationService.VerifyHash(fixedId, "48291", hash1), "VerifyHash trả về FALSE khi thiếu số 0 ở đầu");
AssertTrue(!verificationService.VerifyHash(Guid.NewGuid(), "048291", hash1), "VerifyHash trả về FALSE khi khác MaId (chống replay/cross-session)");

Console.WriteLine("\n--- NHÓM 2: KIỂM THỬ CHE GIẤU EMAIL (EMAIL MASKING) ---");

// Test 2.1: Che giấu email an toàn
AssertTrue(verificationService.MaskEmail("dangthanhthi@gmail.com") == "d***i@gmail.com",
    "Che giấu email dài thông thường: dangthanhthi@gmail.com -> d***i@gmail.com");
AssertTrue(verificationService.MaskEmail("john.doe@huit.edu.vn") == "j***e@huit.edu.vn",
    "Che giấu email học thuật: john.doe@huit.edu.vn -> j***e@huit.edu.vn");
AssertTrue(verificationService.MaskEmail("ab@domain.com") == "a*@domain.com",
    "Che giấu email ngắn (2 ký tự): ab@domain.com -> a*@domain.com");
AssertTrue(verificationService.MaskEmail("a@domain.com") == "a*@domain.com",
    "Che giấu email tối thiểu (1 ký tự): a@domain.com -> a*@domain.com");

Console.WriteLine("\n--- NHÓM 3: KIỂM THỬ QUẢN LÝ MÃ, NHẬP SAI & RATE LIMIT (DATABASE) ---");

// Tạo hồ sơ chờ thử nghiệm
var testRegId = Guid.NewGuid();
var testEmail = $"test_phase2_{Guid.NewGuid():N}@huit.edu.vn";
var testPending = new DangKyChoXacNhan
{
    MaDangKy = testRegId,
    EmailGoc = testEmail,
    EmailSoSanh = testEmail.ToLowerInvariant(),
    TenDangNhapSoSanh = $"user_{testRegId:N}"[..20],
    MatKhauHash = BCrypt.Net.BCrypt.HashPassword("TestPass@2026", 11),
    HoDem = "Nguyễn Văn",
    Ten = "Thử Nghiệm",
    HoTen = "Nguyễn Văn Thử Nghiệm",
    HocVi = "Tiến sĩ",
    HocHam = "Không",
    GioiTinh = "Nam",
    QuocGia = "Vietnam",
    NgonNgu = "Tiếng Việt",
    DonVi = "Đại học Công Thương TP.HCM",
    TaoLucUtc = DateTime.UtcNow,
    HetHanHoSoUtc = DateTime.UtcNow.AddHours(24),
    TrangThai = "Pending"
};
context.DangKyChoXacNhans.Add(testPending);
await context.SaveChangesAsync();

try
{
    // Test 3.1: Tạo mã xác nhận đầu tiên
    var createResult = await verificationService.CreateNewVerificationCodeAsync(testRegId);
    AssertTrue(createResult.Success, "Tạo mã xác nhận mới thành công cho hồ sơ chờ",
        $"Hết hạn: {createResult.ExpiresAtUtc}");
    string validCode = createResult.PlainCode!;

    // Test 3.2: Kiểm tra Cooldown 60s khi gửi lại ngay
    var resendImmediate = await verificationService.CheckResendEligibilityAsync(testRegId);
    AssertTrue(!resendImmediate.IsEligible && resendImmediate.CooldownRemainingSeconds > 0,
        "Chặn gửi lại ngay lập tức do vi phạm Cooldown 60 giây",
        $"Thời gian còn lại: {resendImmediate.CooldownRemainingSeconds}s | Lời báo: {resendImmediate.Message}");

    // Test 3.3: Thử nhập sai mã 5 lần (Brute Force Protection)
    for (int attempt = 1; attempt <= 4; attempt++)
    {
        var wrongResult = await verificationService.ValidateAndConsumeOtpAsync(testRegId, "999999");
        AssertTrue(!wrongResult.IsValid && wrongResult.RemainingAttempts == (5 - attempt) && !wrongResult.IsMaxAttemptsReached,
            $"Nhập sai lần {attempt}: Hệ thống từ chối và ghi nhận số lần còn lại là {5 - attempt}");
    }

    // Lần nhập sai thứ 5 -> Khóa mã
    var fifthWrong = await verificationService.ValidateAndConsumeOtpAsync(testRegId, "999999");
    AssertTrue(!fifthWrong.IsValid && fifthWrong.IsMaxAttemptsReached && fifthWrong.RemainingAttempts == 0,
        "Nhập sai lần thứ 5: Mã xác nhận bị khóa hoàn toàn (IsMaxAttemptsReached = true)");

    // Lần thử thứ 6: Vẫn bị chặn do mã đã bị khóa
    var sixthTry = await verificationService.ValidateAndConsumeOtpAsync(testRegId, validCode);
    AssertTrue(!sixthTry.IsValid && sixthTry.IsMaxAttemptsReached,
        "Nhập mã đúng sau khi đã bị khóa 5 lần vẫn bị từ chối bảo vệ hệ thống");

    // Test 3.4: Bỏ qua cooldown để sinh mã mới (mô phỏng sau khi cooldown đã hết)
    var dbCode = await context.MaXacNhanEmails.OrderByDescending(c => c.TaoLucUtc).FirstOrDefaultAsync(c => c.MaDangKy == testRegId);
    if (dbCode != null)
    {
        dbCode.LanGuiCuoiUtc = DateTime.UtcNow.AddSeconds(-65); // Tua thời gian lùi lại quá 60s
        await context.SaveChangesAsync();
    }

    var createResult2 = await verificationService.CreateNewVerificationCodeAsync(testRegId);
    AssertTrue(createResult2.Success, "Tạo mã mới thành công sau khi hết cooldown 60 giây");
    string newValidCode = createResult2.PlainCode!;

    // Mã cũ bị vô hiệu hóa khi mã mới được tạo
    var tryOldCode = await verificationService.ValidateAndConsumeOtpAsync(testRegId, validCode);
    AssertTrue(!tryOldCode.IsValid, "Mã xác nhận cũ tự động bị vô hiệu hóa khi đã cấp mã mới");

    // Nhập đúng mã mới
    var validCheck = await verificationService.ValidateAndConsumeOtpAsync(testRegId, newValidCode);
    AssertTrue(validCheck.IsValid, "Nhập đúng mã mới còn hạn thành công mỹ mãn", validCheck.Message);

    Console.WriteLine("\n--- NHÓM 4: KIỂM THỬ GỬI EMAIL & TRANSACTIONAL OUTBOX ---");

    // Test 4.1: Gửi email xác nhận
    var sendResult = await senderService.SendVerificationEmailAsync(testEmail, "Nguyễn Văn Thử Nghiệm", newValidCode, 10);
    AssertTrue(sendResult.Success, "Dịch vụ gửi email hoàn tất thành công", sendResult.Message);

    // Test 4.2: Kiểm tra EmailOutbox trong CSDL
    var outboxItem = await context.EmailOutboxes
        .OrderByDescending(o => o.TaoLucUtc)
        .FirstOrDefaultAsync(o => o.NguoiNhan == testEmail);

    AssertTrue(outboxItem != null, "EmailOutbox đã ghi nhận bản ghi thư gửi vào CSDL");
    AssertTrue(outboxItem!.TrangThai == "Sent", "Trạng thái EmailOutbox đã chuyển sang Sent (Dev Simulation)");
    AssertTrue(outboxItem.NoiDungText != null && outboxItem.NoiDungText.Contains(newValidCode),
        "Chế độ Testing lưu nội dung thư để kiểm tra OTP mà không gửi email thật");
}
finally
{
    // Dọn dẹp dữ liệu kiểm thử
    Console.WriteLine("\n--- DỌN DẸP DỮ LIỆU KIỂM THỬ ---");
    var outboxList = await context.EmailOutboxes.Where(o => o.NguoiNhan == testEmail).ToListAsync();
    context.EmailOutboxes.RemoveRange(outboxList);

    var codeList = await context.MaXacNhanEmails.Where(c => c.MaDangKy == testRegId).ToListAsync();
    context.MaXacNhanEmails.RemoveRange(codeList);

    var pendingItem = await context.DangKyChoXacNhans.FirstOrDefaultAsync(p => p.MaDangKy == testRegId);
    if (pendingItem != null)
    {
        context.DangKyChoXacNhans.Remove(pendingItem);
    }

    await context.SaveChangesAsync();
    Console.WriteLine("  Đã xóa sạch hồ sơ kiểm thử tạm và EmailOutbox.");
}

Console.WriteLine("\n=======================================================================");
Console.WriteLine($"  KẾT QUẢ TỔNG HỢP: {passedTests}/{totalTests} KIỂM THỬ THÀNH CÔNG (100% PASS)");
Console.WriteLine("  GIAI ĐOẠN 2 ĐÃ HOÀN TẤT VỚI CHẤT LƯỢNG CAO NHẤT!");
Console.WriteLine("=======================================================================\n");
