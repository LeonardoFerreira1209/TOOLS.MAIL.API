using APPLICATION.DOMAIN.CONTRACTS.REPOSITORIES.TEMPLATES;
using APPLICATION.DOMAIN.DTOS.CONFIGURATION;
using APPLICATION.DOMAIN.DTOS.ENTITIES.TEMPLATES;
using APPLICATION.INFRAESTRUTURE.CONTEXTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace APPLICATION.INFRAESTRUTURE.REPOSITORY.TEMPLATES;

/// <summary>
/// Repositorio de templates
/// </summary>
public class TemplateRepository : ITemplateRepository
{
    private readonly DbContextOptions<Contexto> _dbContextOptions;

    private readonly IOptions<AppSettings> _appsettings;

    public TemplateRepository(IOptions<AppSettings> appsettings)
    {
        _dbContextOptions = new DbContextOptions<Contexto>();

        _appsettings = appsettings;
    }

    /// <summary>
    /// Metodo responsavel por salvar template.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    public async Task Save(string name, string description, string fileContent)
    {
        Log.Information($"[LOG INFORMATION] - SET TITLE {nameof(TemplateRepository)} - METHOD {nameof(Save)}\n");

        using var conteto = new Contexto(_dbContextOptions, _appsettings);

        await conteto.Templates.AddAsync(new TemplateEntity
        {
            Name = name,
            Description = description,
            Content = fileContent
        });

        await conteto.SaveChangesAsync();

    }

    /// <summary>
    /// Metodo responsavel por recuperar e-mail pelo nome.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<string> GetContentTemplateWithName(string name)
    {
        Log.Information($"[LOG INFORMATION] - SET TITLE {nameof(TemplateRepository)} - METHOD {nameof(GetContentTemplateWithName)}\n");

        using var conteto = new Contexto(_dbContextOptions, _appsettings);

        var template = await conteto.Templates.FirstOrDefaultAsync(t => t.Name == name);

        return template.Content;
    }
}
