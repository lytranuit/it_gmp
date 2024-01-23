using it.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using it.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using System.Net;
using it.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using schedule.Middleware;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'IdentityContextConnection' not found.");

builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<UserModel>(options => options.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityContext>(); ;
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddDbContext<ItContext>(options =>
  options.UseSqlServer(connectionString)
  );
builder.Services.AddScoped<ViewRender, ViewRender>();
builder.Services.AddScoped<LoginMailPyme, LoginMailPyme>();

//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = new AuthorizationPolicyBuilder()
//        .RequireAuthenticatedUser()
//        .Build();
//});
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(Int64.Parse(configuration["JWT:Expire"]));

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});
//builder.Services.AddAuthentication().AddMicrosoftIdentityWebApp(configuration);


var MyAllowSpecificOrigins = "tran";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
                      });
});

var app = builder.Build();

app.UseMiddleware<CheckTokenMiddleware>();
// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment() && !app.Environment.IsStaging())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}
//else
//{
app.UseDeveloperExceptionPage();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(builder.Configuration["Source:Path_Private"]),
	RequestPath = "/private",
	OnPrepareResponse = ctx =>
	{
		var token = builder.Configuration["Key_Access"];
		var token_query = ctx.Context.Request.Query["token"].ToString();

        if (!ctx.Context.User.Identity.IsAuthenticated && token_query != token)
        {
            ctx.Context.Response.Redirect("/admin");
        }
    }
});
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
            name: "areaRoute",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
        );
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

});
app.MapRazorPages();
app.Run();
