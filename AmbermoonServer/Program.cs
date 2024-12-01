using AmbermoonServer.Controllers;
using AmbermoonServer.Data;
using AmbermoonServer.Middleware;
using AmbermoonServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace AmbermoonServer;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddScoped<TemplateService>();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<SavegameService>();
        builder.Services.AddScoped<AdminService>();
        builder.Services.AddSingleton<IAuthorizationHandler, AdminRequirementHandler>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();

        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = false;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new HeaderApiVersionReader(Headers.ApiVersion);

        });

        builder.Services.AddDbContext<AppDbContext>();

        var authScheme = "CustomScheme";
        var authHeader = "AuthKeyHeader";

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition(authHeader, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = Headers.UserKey,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Description = "Auth key of the form 'email:token' (e.g., test@example.com:ABCDEF123456...)"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = authHeader
                        }
                    },
                    []
                }
            });
        });

        // Add authentication
        builder.Services.AddAuthentication(authScheme)
            .AddScheme<AuthenticationSchemeOptions, CustomAuthentificationHandler>(authScheme, options => { });

        // Add authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy => policy.Requirements.Add(new AdminRequirement()));
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        var assembly = typeof(TemplateService).Assembly;
        var resources = assembly.GetManifestResourceNames();

        Console.WriteLine("Available resources:");
        foreach (var resource in resources)
        {
            Console.WriteLine(resource);
        }

        app.Run();
	}
}
