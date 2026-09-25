using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// DATABASE
// ==========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<GiftOfTheGiversDbContext>(options =>
    options.UseSqlServer(connectionString));


// ==========================================================
// IDENTITY
// ==========================================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Development-friendly setting
        options.SignIn.RequireConfirmedAccount = false;

        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<GiftOfTheGiversDbContext>();


// ==========================================================
// MVC
// ==========================================================

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();


// ==========================================================
// BUILD
// ==========================================================

var app = builder.Build();


// ==========================================================
// HTTP PIPELINE
// ==========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


// ==========================================================
// MVC ROUTING
// ==========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ==========================================================
// IDENTITY UI
// ==========================================================

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Employee"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Employee"));
    }

    if (!await roleManager.RoleExistsAsync("Administrator"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Administrator"));
    }

    var employeeEmail =
        "employee@giftofthegivers.org";

    var employee =
        await userManager.FindByEmailAsync(employeeEmail);

    if (employee == null)
    {
        employee = new ApplicationUser
        {
            UserName = employeeEmail,
            Email = employeeEmail,
            EmailConfirmed = true,
            FirstName = "Gift",
            LastName = "Employee",
            CreatedAt = DateTime.UtcNow
        };

        await userManager.CreateAsync(
            employee,
            "Employee123!");
    }

    if (!await userManager.IsInRoleAsync(
            employee,
            "Employee"))
    {
        await userManager.AddToRoleAsync(
            employee,
            "Employee");
    }
}
// ==========================================================
// RUN
// ==========================================================

app.Run();