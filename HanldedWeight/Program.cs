using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Interfaces;
using HandledWeight.Services;
using HandledWeight.Services.Print;
using HandledWeight.Services.Weight;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<JwtTokensService>();

// ⚠️ Deja solo un registro para Scale (normalmente Singleton si mantiene la conexión al puerto COM, o Scoped)
builder.Services.AddSingleton<Scale>();
builder.Services.AddSingleton<IComPrintInterface, ComPrinterService>();

builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Obligatorio para SignalR
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

// 1. PRIMERO ROUTING
app.UseRouting();

// 2. SEGUNDO CORS (Obligatorio entre UseRouting y UseAuthorization)
app.UseCors("AllowReact");

// 3. TERCERO AUTORIZACIÓN
app.UseAuthorization();

// 4. AL FINAL MAPEAR LOS ENDPOINTS (Controllers y Hubs)
app.MapControllers();
app.MapHub<ScaleHub>("/scaleHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();