using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.OpenApi.Models;
using TMDbLib.Client;
using Swashbuckle.AspNetCore.SwaggerUI;

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
        TMDbClient client = new TMDbClient("8ffc6501c39d47b08fdb929144b5b4b4");
        services.AddSingleton(client);

        services.AddControllers();

        services.AddCors(options => {
            options.AddPolicy("AllowReactFrontend",
                builder => builder.WithOrigins("https://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader());
        });

        services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cinamatica", Version = "v1" });
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinamatica API v1");
                c.RoutePrefix = string.Empty; // Set the Swagger UI at the root URL
                c.DocExpansion(DocExpansion.List); // Configure UI layout
            });
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