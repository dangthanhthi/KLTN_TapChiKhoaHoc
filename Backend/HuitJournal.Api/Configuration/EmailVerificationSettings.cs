namespace HuitJournal.Api.Configuration;

public class EmailVerificationSettings
{
    public string HmacSecretKey { get; set; } = "";
    public int OtpExpiryMinutes { get; set; } = 10;
    public int PendingProfileExpiryHours { get; set; } = 24;
    public int MaxFailedAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxResendsPerHour { get; set; } = 3;
    public int MaxResendsPerDay { get; set; } = 10;
    public string SenderEmail { get; set; } = "journal@huit.edu.vn";
    public string SenderDisplayName { get; set; } = "Tạp chí Khoa học Đại học Công Thương TP.HCM";
    public string PublicWebBaseUrl { get; set; } = "http://localhost:8088";
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool SimulateDeliveryInDev { get; set; } = true;
    public bool EnableBackgroundDispatcher { get; set; } = true;
    public int OutboxPollingIntervalSeconds { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 20;
    public int MaxDeliveryAttempts { get; set; } = 3;
    public int CleanupIntervalHours { get; set; } = 1;
    public int SentRetentionDays { get; set; } = 7;
}
