using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TMDbLib.Client;
using Swashbuckle.AspNetCore.SwaggerUI;
using Cinematica.API.Data;
using Cinematica.API.Models;
using Cinematica.API.Services;
using Microsoft.Extensions.FileProviders;

namespace Cinematica.API;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {

        var TMDbApiKey = Configuration.GetSection("TMDbApiKey").Value;
        TMDbClient client = new (TMDbApiKey);

        services.AddSingleton(client);

        // Map the images folder
        string myImages = Path.Combine(Environment.ContentRootPath, "images");

        // Add the path to the service collection so it can be injected
        services.AddSingleton(myImages);

        services.AddControllers();

        services.AddDbContext<DataContext>();

        // Removes null fields when sending JSON response
        services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
            });

        services.AddCors(options => {
            options.AddPolicy("AllowReactFrontend",
                builder => builder.WithOrigins("https://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader());
        });

        services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cinematica API", Version = "v1" });
            });

        services.AddScoped<IHelperService, HelperService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseStaticFiles();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinematica API v1");
                c.RoutePrefix = string.Empty; // Set the Swagger UI at the root URL
                c.DocExpansion(DocExpansion.List); // Configure UI layout
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "images")),
                RequestPath = "/images"
            });
        }
        else
        {
            //TODO: S3 bucket details here for static file connection
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