using Microsoft.Extensions.DependencyInjection;
using TMDbLib.Objects.General;
using TMDbLib.Client;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Cinematica.API;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {

        // Load environment variables from .env file
        DotNetEnv.Env.Load();
        var ApiKey = DotNetEnv.Env.GetString("TMDbApiKey");

        services.AddControllers();

        services.AddCors(options => {
            options.AddPolicy("AllowReactFrontend",
                builder => builder.WithOrigins("https://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader());
        });

        var tmdbClient = new TMDbClient(ApiKey);
        services.AddSingleton(tmdbClient);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseCors("AllowReactFrontend");
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}