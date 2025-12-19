using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using AuthApi.Services;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Repositories;
using AuthApi.Middleware;
using Serilog;
using Serilog.Events;
using AuthApi.Logging;
using Prometheus; // ✅ added

try
{
    // --- Startup Log ---
    Log.Information("Starting Auth API...");

    var builder = WebApplication.CreateBuilder(args);

    // 👇 Register HttpContextAccessor before building logger
    builder.Services.AddHttpContextAccessor();

    // --- Configure Serilog ---
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter()) // ✅ JSON console (kept)
        .WriteTo.File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(),    // ✅ JSON file (kept)
            "Logs/app-.json", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();

    // 🔇 Disable built-in loggers and route everything through Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    // --- Services ---
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Auth API",
            Version = "v1",
            Description = "JWT Authentication Example"
        });

        // Swagger JWT setup
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // --- Dependency Injection ---
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddAutoMapper(typeof(Program));
    builder.Services.AddSingleton<MetricsService>();

    // --- Database ---
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

    // --- Authentication & Authorization ---
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };

            // Prevents 500 errors on invalid tokens
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    ctx.NoResult();
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // --- Build the app ---
    var app = builder.Build();

    // 👇 Nice startup message via Serilog
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = string.Join(", ", app.Urls);
        Log.Information("✅ Auth API is running at {Urls} (ENV: {Env})",
            urls, app.Environment.EnvironmentName);
    });

    // ✅ Expose prometheus-net default registry at your scrape path
    //    (this is what makes http_requests_total visible to Prometheus)
    app.UseMetricServer("/metrics/prometheus"); // ✅ added

    // --- Swagger ---
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();
    }

    // --- Middleware pipeline ---
    // ✅ Skip HTTPS redirect for /metrics routes (Prometheus scrapes use plain HTTP)
    var enableHttpsRedirect = builder.Configuration.GetValue("EnableHttpsRedirect", false);
    if (enableHttpsRedirect)
    {
        app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/metrics"), branch =>
        {
            branch.UseHttpsRedirection();
        });
    }

    // Global middlewares
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<MetricsMiddleware>();

    // ✅ Skip logging & wrappers for Prometheus /metrics/** endpoints
    app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/metrics"), branch =>
    {
        branch.UseMiddleware<RequestLoggingMiddleware>();
        branch.UseResponseWrapper();
        branch.UseAuthentication();
        branch.UseAuthorization();
    });

    // --- Controllers ---
    app.MapControllers();

    // --- Run app ---
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
