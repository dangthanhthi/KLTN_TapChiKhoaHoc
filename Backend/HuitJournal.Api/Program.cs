using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.Services;
using HuitJournal.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Development credentials are loaded through .NET User Secrets; production uses environment variables / secret store.

if (!builder.Environment.IsProduction())
{
    var localSecrets = new Dictionary<string, string?>();
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        localSecrets["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    if (string.IsNullOrWhiteSpace(builder.Configuration["EmailVerification:HmacSecretKey"]))
        localSecrets["EmailVerification:HmacSecretKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Configuration.AddInMemoryCollection(localSecrets);
}

if (builder.Environment.IsProduction())
{
    var otpKey = builder.Configuration["EmailVerification:HmacSecretKey"];
    if (string.IsNullOrWhiteSpace(otpKey) || Encoding.UTF8.GetByteCount(otpKey) < 32)
        throw new InvalidOperationException("Production yêu cầu EmailVerification:HmacSecretKey riêng (ít nhất 32 byte).");
    if (builder.Configuration.GetValue<bool>("EmailVerification:SimulateDeliveryInDev"))
        throw new InvalidOperationException("Production không được phép giả lập gửi thư xác nhận.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["EmailVerification:SmtpUsername"]) ||
        string.IsNullOrWhiteSpace(builder.Configuration["EmailVerification:SmtpPassword"]))
        throw new InvalidOperationException("Production yêu cầu cấu hình SMTP thật để gửi mã xác nhận.");
    if (!Uri.TryCreate(builder.Configuration["EmailVerification:PublicWebBaseUrl"], UriKind.Absolute, out var publicWebUrl) ||
        publicWebUrl.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("Production yêu cầu EmailVerification:PublicWebBaseUrl là URL HTTPS của Web.");
}

// 1. Cấu hình DbContext kết nối SQL Server
var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction())
{
    var configuredDatabase = string.IsNullOrWhiteSpace(configuredConnectionString)
        ? null
        : new SqlConnectionStringBuilder(configuredConnectionString);
    if (configuredDatabase is null ||
        string.IsNullOrWhiteSpace(configuredDatabase.InitialCatalog) ||
        configuredDatabase.DataSource is "." or ".\\CSSQL22" or ".\\SQLEXPRESS" or "(localdb)\\MSSQLLocalDB")
        throw new InvalidOperationException("Production yêu cầu ConnectionStrings:DefaultConnection trỏ tới SQL Server công khai đã cấu hình.");
}

var connectionString = configuredConnectionString
    ?? "Server=.\\CSSQL22;Database=QL_TapChiKhoaHoc;Integrated Security=True;TrustServerCertificate=True;";

if (builder.Environment.IsEnvironment("Testing") &&
    !string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
        "QL_TapChiKhoaHoc_Test", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Profile Testing phải kết nối QL_TapChiKhoaHoc_Test.");
}

builder.Services.AddDbContext<QLTapChiKhoaHocContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Đăng ký Services & Cấu hình Email Verification
builder.Services.Configure<EmailVerificationSettings>(builder.Configuration.GetSection("EmailVerification"));
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();
builder.Services.AddHostedService<EmailOutboxDispatcherService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JournalWorkflowService>();
builder.Services.AddScoped<WorkflowMaintenanceService>();
builder.Services.AddScoped<IssuePublicationService>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 125L * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 125L * 1024 * 1024);
builder.Services.AddScoped<IBaiBaoService, BaiBaoService>();
builder.Services.AddScoped<ISoTapChiService, SoTapChiService>();
builder.Services.AddScoped<IPhanBienService, PhanBienService>();

// 3. Cấu hình JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32))
    throw new InvalidOperationException("Production yêu cầu Jwt:Key riêng (ít nhất 32 byte), đặt qua biến môi trường hoặc secret store.");
var issuer = builder.Configuration["Jwt:Issuer"] ?? "HuitJournal";
var audience = builder.Configuration["Jwt:Audience"] ?? "HuitJournalUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<QLTapChiKhoaHocContext>();
            if (!int.TryParse(ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ||
                !await db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == id && u.TrangThai)) { ctx.Fail("Tài khoản không còn hoạt động."); return; }
            var version = await db.WorkflowRecords.AsNoTracking().Where(r => r.UserId == id && r.Kind == "AuthVersion" && r.State == "Active")
                .OrderByDescending(r => r.UpdatedUtc).Select(r => r.Id.ToString()).FirstOrDefaultAsync();
            if ((ctx.Principal?.FindFirst("session_version")?.Value ?? "") != (version ?? "")) ctx.Fail("Phiên đăng nhập đã hết hiệu lực.");
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Chỉ cho phép origin đã cấu hình khi triển khai; môi trường phát triển giữ cổng Web riêng.
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        if (!builder.Environment.IsProduction())
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// 5. Cấu hình Controllers & OpenAPI
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
                );

            var firstError = errors.Values.SelectMany(v => v).FirstOrDefault() 
                ?? "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại các trường được đánh dấu đỏ.";

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                success = false,
                requiresVerification = false,
                message = firstError,
                errors = errors
            });
        };
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// 6. Pipeline cấu hình
if (app.Environment.IsProduction()) app.UseHttpsRedirection();
app.UseCors("WebClient");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 7. Phục vụ thư mục Web/ tĩnh trực tiếp từ Backend
var webFolder = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "Web"));
if (Directory.Exists(webFolder))
{
    var fileProvider = new PhysicalFileProvider(webFolder);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        DefaultFileNames = new List<string> { "UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html", "index.html" }
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
}

// 8. Bảo vệ tài liệu bản thảo: Tuyệt đối không phục vụ static route mở cho thư mục Uploads/
// Bản thảo khoa học bắt buộc tải qua các API endpoint có xác thực JWT (PhanBienController/BaiBaoController).
// Chỉ mở static route cho thư mục ảnh đại diện công khai /uploads/avatars.
var avatarsFolder = Path.Combine(UploadStoragePaths.GetRoot(app.Environment.ContentRootPath), "avatars");
if (!Directory.Exists(avatarsFolder))
{
    Directory.CreateDirectory(avatarsFolder);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarsFolder),
    RequestPath = "/uploads/avatars"
});

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsEnvironment("Testing"))
{
    // Chỉ có trong môi trường Testing; bài kiểm thử phải xác minh endpoint này trước khi ghi dữ liệu.
    app.MapGet("/api/testing/environment", async (QLTapChiKhoaHocContext db) =>
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var connection = db.Database.GetDbConnection();
            return Results.Ok(new
            {
                environment = app.Environment.EnvironmentName,
                database = connection.Database,
                dataSource = connection.DataSource
            });
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }).ExcludeFromDescription();
}

app.MapControllers();

app.Run();
