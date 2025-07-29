using BibliotecaAPI;
using BibliotecaAPI.Datos;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Area de Servicios
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));
builder.Services.AddIdentityCore<IdentityUser>().
    AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<IdentityUser>>();
builder.Services.AddScoped<SignInManager<IdentityUser>>();
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;
    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Area de middlewares
app.MapControllers();

app.Run();
