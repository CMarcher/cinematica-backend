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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Amazon.AspNetCore.Identity.Cognito;
using Microsoft.IdentityModel.Tokens;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Microsoft.AspNetCore.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

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

        var imageSettings = Configuration.GetSection("ImageSettings").Get<ImageSettings>();

        if (Environment.IsDevelopment())
        {
            imageSettings.UploadLocation = Path.Combine(Environment.ContentRootPath,
                imageSettings.UploadLocation);
            services.AddSingleton<IFileStorageService>(new LocalFileStorageService());
        }
        else
        {
            var s3Client = new AmazonS3Client();
            services.AddSingleton<IFileStorageService>(new S3FileStorageService(s3Client, "cinematica-media"));
        }

        services.AddSingleton(imageSettings);

        services.AddControllers();

        services.AddDbContext<DataContext>();

        // Add Cognito Identity Provider
        AmazonCognitoIdentityProviderClient cognitoClient = new AmazonCognitoIdentityProviderClient();
        services.AddSingleton(cognitoClient);

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Removes null fields when sending JSON response
        services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
            });

        // Add Cors
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

        // Add authentication using Cognito tokens
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = Configuration["AWS:Authority"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Configuration["AWS:Authority"],
                ValidateAudience = false
            };
        });
        
        services.AddScoped<IHelperService, HelperService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var imageSettings = Configuration.GetSection("ImageSettings").Get<ImageSettings>();
        
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

            //URL Location for image files.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), imageSettings.UploadLocation)),
                RequestPath = "/images"
            });
        }
        
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                // using static System.Net.Mime.MediaTypeNames;
                context.Response.ContentType = Text.Plain;

                await context.Response.WriteAsync("An exception was thrown.");

                var exceptionHandlerPathFeature =
                    context.Features.Get<IExceptionHandlerPathFeature>();

                if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                {
                    await context.Response.WriteAsync(" The file was not found.");
                }

                if (exceptionHandlerPathFeature?.Path == "/")
                {
                    await context.Response.WriteAsync(" Page: Home.");
                }
            });

            app.UseHttpsRedirection();
       
        app.UseAuthentication();

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