using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    //context.Database.ExecuteSqlRaw("DELETE FROM Manhwas");

    if (!context.Manhwas.Any())
    {
        context.Manhwas.AddRange(
            new Manhwa
            {
                Title = "Solo Leveling",
                Description = "The weakest hunter becomes humanity’s strongest weapon.",
                CoverImageUrl = "/images/sololeveling.jpg",
                Status = "Completed",
                LastUpdated = DateTime.Now.AddDays(-30),
                ViewCount = 2500000,
                Rating = 9.8
            },

new Manhwa
{
    Title = "Tower of God",
    Description = "A boy enters a mysterious tower to find his only friend.",
    CoverImageUrl = "/images/towerofgod.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-3),
    ViewCount = 1800000,
    Rating = 9.2
},

new Manhwa
{
    Title = "The Beginning After The End",
    Description = "A king reincarnates into a magical world.",
    CoverImageUrl = "/images/tbate.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-1),
    ViewCount = 2100000,
    Rating = 9.5
},

new Manhwa
{
    Title = "Omniscient Reader",
    Description = "A reader becomes the protagonist of his favorite novel.",
    CoverImageUrl = "/images/omniscientreader.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-2),
    ViewCount = 1700000,
    Rating = 9.4
},

new Manhwa
{
    Title = "The God of High School",
    Description = "High schoolers fight in a tournament for ultimate power.",
    CoverImageUrl = "/images/goh.jpg",
    Status = "Completed",
    LastUpdated = DateTime.Now.AddDays(-200),
    ViewCount = 1400000,
    Rating = 8.9
},

new Manhwa
{
    Title = "Eleceed",
    Description = "A kind-hearted boy and a secret agent cat fight villains.",
    CoverImageUrl = "/images/eleceed.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-4),
    ViewCount = 950000,
    Rating = 8.8
},

new Manhwa
{
    Title = "Noblesse",
    Description = "A powerful noble awakens after 820 years of sleep.",
    CoverImageUrl = "/images/noblesse.jpg",
    Status = "Completed",
    LastUpdated = DateTime.Now.AddDays(-500),
    ViewCount = 1300000,
    Rating = 8.7
},

new Manhwa
{
    Title = "Hardcore Leveling Warrior",
    Description = "The top player of a VR game loses everything overnight.",
    CoverImageUrl = "/images/hlw.jpg",
    Status = "Completed",
    LastUpdated = DateTime.Now.AddDays(-120),
    ViewCount = 880000,
    Rating = 8.5
},

new Manhwa
{
    Title = "Legend of the Northern Blade",
    Description = "A lone swordsman rises to avenge his fallen clan.",
    CoverImageUrl = "/images/northernblade.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-5),
    ViewCount = 760000,
    Rating = 9.1
},

new Manhwa
{
    Title = "Lookism",
    Description = "A bullied student wakes up in a different body.",
    CoverImageUrl = "/images/lookism.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-2),
    ViewCount = 1600000,
    Rating = 8.6
},

new Manhwa
{
    Title = "Weak Hero",
    Description = "Brains beat brawn in brutal high school fights.",
    CoverImageUrl = "/images/weakhero.jpg",
    Status = "Completed",
    LastUpdated = DateTime.Now.AddDays(-90),
    ViewCount = 720000,
    Rating = 8.4
},

new Manhwa
{
    Title = "Return of the Mount Hua Sect",
    Description = "A legendary warrior reincarnates centuries later.",
    CoverImageUrl = "/images/mounthua.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-1),
    ViewCount = 1100000,
    Rating = 9.3
},

new Manhwa
{
    Title = "Mercenary Enrollment",
    Description = "A child soldier returns to live a normal high school life.",
    CoverImageUrl = "/images/mercenary.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-6),
    ViewCount = 980000,
    Rating = 8.9
},

new Manhwa
{
    Title = "The Boxer",
    Description = "A mysterious coach trains a boy with godlike talent.",
    CoverImageUrl = "/images/theboxer.jpg",
    Status = "Completed",
    LastUpdated = DateTime.Now.AddDays(-150),
    ViewCount = 600000,
    Rating = 9.0
},

new Manhwa
{
    Title = "Doom Breaker",
    Description = "A warrior sent back in time to save humanity.",
    CoverImageUrl = "/images/doombreaker.jpg",
    Status = "Ongoing",
    LastUpdated = DateTime.Now.AddDays(-7),
    ViewCount = 540000,
    Rating = 8.7
}
);

        context.SaveChanges(); 
    } 
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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
