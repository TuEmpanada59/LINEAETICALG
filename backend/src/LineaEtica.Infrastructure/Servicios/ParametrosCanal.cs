using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>Parámetros del canal guardados en la tabla Configuracion.</summary>
public sealed class ParametrosCanal(LineaEticaDbContext db)
{
    public async Task<int> PlazoRespuestaDias(CancellationToken ct = default) =>
        int.Parse(await Valor(ParametroConfiguracion.PlazoRespuestaDias, ct));

    public async Task<int> RetencionDiasTrasCierre(CancellationToken ct = default) =>
        int.Parse(await Valor(ParametroConfiguracion.RetencionDiasTrasCierre, ct));

    public Task<string> TextoAcuse(CancellationToken ct = default) =>
        Valor(ParametroConfiguracion.TextoAcuse, ct);

    private async Task<string> Valor(string clave, CancellationToken ct) =>
        await db.Configuracion.Where(p => p.Clave == clave).Select(p => p.Valor).SingleOrDefaultAsync(ct)
        ?? throw new InvalidOperationException($"Falta el parámetro '{clave}' en la tabla Configuracion.");
}
