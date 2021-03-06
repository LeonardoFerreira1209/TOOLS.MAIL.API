using APPLICATION.APPLICATION.CONFIGURATIONS.SWAGGER;
using APPLICATION.APPLICATION.SERVICES.EMAIL;
using APPLICATION.APPLICATION.SERVICES.TEMPLATE;
using APPLICATION.DOMAIN.CONTRACTS.REPOSITORIES.TEMPLATES;
using APPLICATION.DOMAIN.CONTRACTS.SERVICES.EMAIL;
using APPLICATION.DOMAIN.DTOS.REQUEST;
using APPLICATION.DOMAIN.DTOS.RESPONSE;
using APPLICATION.DOMAIN.UTILS;
using APPLICATION.INFRAESTRUTURE.CONTEXTO;
using APPLICATION.INFRAESTRUTURE.FACADES.EMAIL;
using APPLICATION.INFRAESTRUTURE.REPOSITORY.TEMPLATES;
using HotChocolate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Swashbuckle.AspNetCore.Annotations;
using System.Globalization;
using System.Net.Mime;

namespace APPLICATION.APPLICATION.CONFIGURATIONS;

public static class ExtensionsConfigurations
{
    public static readonly string HealthCheckEndpoint = "/application/healthcheck";

    /// <summary>
    /// Configuração de Logs do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder applicationBuilder)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        applicationBuilder.Host.UseSerilog((context, config) =>
        {
            config
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .WriteTo.Console();
        });

        return applicationBuilder;
    }

    /// <summary>
    /// Configuração de linguagem principal do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureLanguage(this IServiceCollection services)
    {
        var cultureInfo = new CultureInfo("pt-BR");

        CultureInfo
            .DefaultThreadCurrentCulture = cultureInfo;

        CultureInfo
            .DefaultThreadCurrentUICulture = cultureInfo;

        return services;
    }

    /// <summary>
    /// Configuração do banco de dados do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureContexto(this IServiceCollection services, IConfiguration configurations)
    {
        services
            .AddDbContext<Contexto>(options => options.UseSqlServer(configurations.GetValue<string>("ConnectionStrings:BaseDados")));

        return services;
    }

    /// <summary>
    /// Configuração do swagger do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurations"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services, IConfiguration configurations)
    {
        var apiVersion = configurations.GetValue<string>("SwaggerInfo:ApiVersion"); var apiDescription = configurations.GetValue<string>("SwaggerInfo:ApiDescription"); var uriMyGit = configurations.GetValue<string>("SwaggerInfo:UriMyGit");

        services.AddSwaggerGen(swagger =>
        {
            swagger.EnableAnnotations();

            swagger.SwaggerDoc(apiVersion, new OpenApiInfo
            {
                Version = apiVersion,
                Title = $"{apiDescription} - {apiVersion}",
                Description = apiDescription,

                Contact = new OpenApiContact
                {
                    Name = "HYPER.IO DESENVOLVIMENTOS LTDA",
                    Email = "HYPER.IO@OUTLOOK.COM",
                },

            });

            swagger.DocumentFilter<HealthCheckSwagger>();
        });

        return services;
    }

    /// <summary>
    /// Configuração das dependencias (Serrvices, Repository, Facades, etc...).
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configurations)
    {
        services
            .AddTransient(x => configurations)
            // Services
            .AddTransient<IEmailService, EmailService>()
            .AddTransient<ITemplateService, TemplateService>()
            // Facades
            .AddSingleton<EmailFacade, EmailFacade>()
            // Repositories
            .AddSingleton<ITemplateRepository, TemplateRepository>();

        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        return services;
    }

    /// <summary>
    /// Configuração do HealthChecks do sistema.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurations"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configurations)
    {
        services
            .AddHealthChecks().AddSqlServer(configurations.GetConnectionString("BaseDados"), name: "Base de dados padrão.", tags: new string[] { "Core", "SQL Server" });

        return services;
    }

    /// <summary>
    /// Configuração dos cors aceitos.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        return services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
    }

    /// <summary>
    /// Configuração do HealthChecks do sistema.
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    public static IApplicationBuilder ConfigureHealthChecks(this IApplicationBuilder application)
    {
        application.UseHealthChecks(ExtensionsConfigurations.HealthCheckEndpoint, new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var result = JsonConvert.SerializeObject(new
                {
                    statusApplication = report.Status.ToString(),

                    healthChecks = report.Entries.Select(e => new
                    {
                        check = e.Key,
                        ErrorMessage = e.Value.Exception?.Message,
                        status = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                    })
                });

                context.Response.ContentType = MediaTypeNames.Application.Json;

                await context.Response.WriteAsync(result);
            }
        });

        return application;
    }

    /// <summary>
    /// Configuração de uso do swagger do sistema.
    /// </summary>
    /// <param name="application"></param>
    /// <param name="configurations"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSwaggerConfigurations(this IApplicationBuilder application, IConfiguration configurations)
    {
        var apiVersion = configurations.GetValue<string>("SwaggerInfo:ApiVersion");

        application.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        application.UseSwaggerUI(swagger =>
        {
            swagger.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{apiVersion}");
            swagger.DefaultModelExpandDepth(0);
        });

        application
            .UseMvcWithDefaultRoute();

        return application;
    }

    /// <summary>
    /// Configruação de minimals APIS.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <returns></returns>
    public static WebApplication UseMinimalAPI(this WebApplication application, IConfiguration configurations)
    {
        #region Mail's
        application.MapPost("/mail/invite",
        [EnableCors("CorsPolicy")][AllowAnonymous][SwaggerOperation(Summary = "Criar uauário.", Description = "Método responsavel por criar usuário")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        async ([Service] IEmailService emailService, MailRequest request) =>
        {
            using (LogContext.PushProperty("Controller", "MailController"))
            using (LogContext.PushProperty("Payload", JsonConvert.SerializeObject(request)))
            using (LogContext.PushProperty("Metodo", "Invite"))
            {
                return await Tracker.Time(() => emailService.Invite(request), "Enviar e-mail");
            }
        });
        #endregion

        #region Templates
        application.MapPost("templates/save",
        [EnableCors("CorsPolicy")][AllowAnonymous][SwaggerOperation(Summary = "Salvar modelos de templates no banco de dados.", Description = "Método responsavel por salvar templates no banco de dados.")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        async ([Service] ITemplateService templateService, string name, string description, HttpRequest request) =>
        {
            using (var reader = new StreamReader(request.Body, System.Text.Encoding.UTF8))
            {
                string fileContent = await reader.ReadToEndAsync();

                using (LogContext.PushProperty("Controller", "MailController"))
                using (LogContext.PushProperty("Payload", JsonConvert.SerializeObject(new List<string> { name, description, fileContent })))
                using (LogContext.PushProperty("Metodo", "Save Templates"))
                {
                    return await Tracker.Time(() => templateService.Save(name, description, fileContent), "Salvar template");
                }
            }

        }).Accepts<IFormFile>("text/plain").Produces(200);
        #endregion

        return application;
    }

}
