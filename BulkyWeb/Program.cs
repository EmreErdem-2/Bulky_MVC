using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using BulkyBook.DataAccess.DbInitializer;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.Configure<IyzicoSettings>(builder.Configuration.GetSection("Iyzico"));

    builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = $"/Identity/Account/Login";
        options.LogoutPath = $"/Identity/Account/Logout";
        options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
    });

    builder.Services.AddAuthentication().AddFacebook(option =>
    {
        option.AppId = builder.Configuration["Facebook:AppId"];
        option.AppSecret = builder.Configuration["Facebook:AppSecret"];
    });

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(100);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Make the session cookie essential
    });

    builder.Services.AddRazorPages();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IEmailSender, EmailSender>();
    builder.Services.AddScoped<IDbInitializer, DbInitializer>();
}
else //Production
{
    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // Get DB credentials from environment variables
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    // Build the connection string dynamically
    var connectionString = $"Server=tcp:bulkyne.database.windows.net,1433;Initial Catalog=bulkyDB;Persist Security Info=False;User ID={dbUser};Password={dbPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    // Register DbContext with the dynamic connection string
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Manually create IyzicoSettings from environment variables
    var iyzicoSettings = new IyzicoSettings
    {
        ApiKey = Environment.GetEnvironmentVariable("IYZICO_API_KEY"),
        SecretKey = Environment.GetEnvironmentVariable("IYZICO_SECRET_KEY"),
        BaseUrl = Environment.GetEnvironmentVariable("IYZICO_BASE_URL")
    };

    // Register the instance with the DI container
    builder.Services.Configure<IyzicoSettings>(options =>
    {
        options.ApiKey = iyzicoSettings.ApiKey;
        options.SecretKey = iyzicoSettings.SecretKey;
        options.BaseUrl = "https://sandbox-api.iyzipay.com";
    });

    builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = $"/Identity/Account/Login";
        options.LogoutPath = $"/Identity/Account/Logout";
        options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
    });


    builder.Services.AddAuthentication().AddFacebook(option =>
    {
        option.AppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
        option.AppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET");
    });

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(100);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Make the session cookie essential
    });

    builder.Services.AddRazorPages();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IEmailSender, EmailSender>();
    builder.Services.AddScoped<IDbInitializer, DbInitializer>();
}


    var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
SeedDatabase();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}