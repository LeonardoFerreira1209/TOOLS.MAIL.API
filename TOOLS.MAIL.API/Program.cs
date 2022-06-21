using APPLICATION.APPLICATION.CONFIGURATIONS;
using APPLICATION.DOMAIN.DTOS.CONFIGURATION;
using Microsoft.AspNetCore.Mvc;
using Serilog;

try
{
    // Preparando builder.
    var builder = WebApplication.CreateBuilder(args);

    // Pegando configura��es do appsettings.json.
    var configurations = builder.Configuration;

    // Chamada da configura��o do Serilog - Logs do sistema.
    builder
        .ConfigureSerilog();

    Log.Information($"[LOG INFORMATION] - Inicializando aplica��o [TOOLS.MAIL.API] - [INICIANDO]\n");

    /// <summary>
    /// Chamada das configura��es do projeto.
    /// </summary>
    /// 
    builder.Services
        .Configure<AppSettings>(configurations).AddSingleton<AppSettings>()
        .AddHttpContextAccessor()
        .AddEndpointsApiExplorer()
        .AddOptions()
        .ConfigureLanguage()
        .ConfigureSwagger(configurations)
        .ConfigureDependencies(configurations)
        .ConfigureHealthChecks(configurations)
        .ConfigureCors()
        .AddControllers(options =>
        {
            options.EnableEndpointRouting = false;

            options.Filters.Add(new ProducesAttribute("application/json"));

        });

    // Preparando WebApplication Build.
    var applicationbuilder = builder.Build();

    // Chamada das connfigura��es do WebApplication Build.
    applicationbuilder
        .UseSwaggerConfigurations(configurations)
        .ConfigureHealthChecks()
        .UseDefaultFiles()
        .UseStaticFiles()
        .UseHttpsRedirection()
        .UseHttpsRedirection();

    // Chamando as configura��es de Minimal APIS.
    applicationbuilder.UseMinimalAPI(configurations);

    Log.Information($"[LOG INFORMATION] - Inicializando aplica��o [TOOLS.MAIL.API] - [FINALIZADO]\n");

    // Iniciando a aplica��o com todas as configura��es j� carregadas.
    applicationbuilder.Run();
}
catch (Exception exception)
{
    Log.Error("[LOG ERROR] - Ocorreu um erro ao inicializar a aplicacao [TOOLS.MAIL.API]\n", exception.Message);
}