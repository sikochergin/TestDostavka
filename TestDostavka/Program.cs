using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

builder.Services.AddScoped<IPasswordHasher<Person>, PasswordHasher<Person>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "InternationalDelivery.Auth";

        options.LoginPath = "/Person/Login";
        options.AccessDeniedPath = "/Person/access-denied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

builder.Services
    .AddOptions<YooKassaOptions>()
    .Bind(
        builder.Configuration.GetSection(
            YooKassaOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ShopId),
        "В конфигурации не указан YooKassa:ShopId.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SecretKey),
        "В конфигурации не указан YooKassa:SecretKey.")
    .Validate(
        options =>
            Uri.TryCreate(
                options.ApiUrl,
                UriKind.Absolute,
                out var apiUri)
            && (apiUri.Scheme == Uri.UriSchemeHttps ||
                apiUri.Scheme == Uri.UriSchemeHttp),
        "YooKassa:ApiUrl должен быть корректным абсолютным URL.")
    .ValidateOnStart();

builder.Services.AddHttpClient<
    IYooKassaService,
    YooKassaService>(
    (serviceProvider, httpClient) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<YooKassaOptions>>()
            .Value;

        httpClient.BaseAddress =
            new Uri(options.ApiUrl);

        var credentials =
            $"{options.ShopId}:{options.SecretKey}";

        var encodedCredentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(credentials));

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                encodedCredentials);

        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        httpClient.Timeout =
            TimeSpan.FromSeconds(30);
    });


builder.Services.AddAuthorization();

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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
