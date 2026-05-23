using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using ManwhaWebsite.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();
var resendConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Resend:ApiKey"]);
var smtpConfigured   = !string.IsNullOrWhiteSpace(builder.Configuration["EmailSettings:Username"]);
if (resendConfigured)
{
    builder.Services.AddHttpClient<ResendEmailSender>();
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, ResendEmailSender>();
    builder.Services.AddTransient<IContactEmailSender, ResendEmailSender>();
}
else if (smtpConfigured)
{
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
    builder.Services.AddTransient<IContactEmailSender, SmtpEmailSender>();
    builder.Services.AddTransient<SmtpEmailSender>();
}
else
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, LoggingEmailSender>();
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("ManwhaWebsite");

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<AniListService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ManhwaVault/1.0");
});
builder.Services.AddHttpClient<MangaUpdatesService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ManhwaVault/1.0");
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<RecommendationService>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["AzureStorage:ConnectionString"]))
    builder.Services.AddSingleton<BlobStorageService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.MapControllerRoute("dev-email", "dev/test-email", new { controller = "Dev", action = "TestEmail" });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
