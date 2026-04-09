using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using KhoaNVCB_API.Services;
var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ DbContext kết nối SQL Server
// "DefaultConnection" phải khớp chính xác tên trong file JSON
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Đăng ký DbContext và "bơm" chuỗi kết nối vào
builder.Services.AddDbContext<KhoaNvcbBlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(); // Thêm dòng này
    }));
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCorsPolicy", policy =>
    {
        policy.WithOrigins("https://sweet-twilight-be5f4e.netlify.app",
            "https://localhost:7129",
            "https://khoaanctnb.netlify.app",
            "http://localhost:5198",
            "https://anninhchinhtrinoibo.netlify.app") // Giữ nguyên, không có gạch chéo cuối
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Pagination");
    });
});
// Đảm bảo có dòng này trước app.Run()
builder.Services.AddScoped<ScraperService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,   // Tắt để tránh lỗi mismatch domain issuer
            ValidateAudience = false, // QUAN TRỌNG: Tắt cái này để sửa lỗi 'Audience empty'
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. Định nghĩa chuẩn bảo mật JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Token theo định dạng: Bearer [khoảng cách] [token của bạn]"
    });

    // 2. Áp dụng chuẩn bảo mật này vào các endpoint
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
// Đọc cấu hình Cloudinary từ appsettings.json
builder.Services.Configure<KhoaNVCB_API.Helpers.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

// Đăng ký PhotoService để các Controller có thể lấy ra xài
builder.Services.AddScoped<KhoaNVCB_API.Services.IPhotoService, KhoaNVCB_API.Services.PhotoService>();

var app = builder.Build();

// 1. Swagger nên để đầu tiên để test API dễ dàng
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KhoaNVCB API V1");
    c.RoutePrefix = "swagger";
});

// 2. HttpsRedirection (Nếu bạn dùng HTTPS)
app.UseHttpsRedirection();

// 3. QUAN TRỌNG NHẤT: Cấu hình CORS với đúng tên Policy đã khai báo ở trên
// Phải đặt TRƯỚC UseAuthentication và UseAuthorization
app.UseCors("BlazorCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();