using LineaEtica.Infrastructure.Servicios;

namespace LineaEtica.Api.Tareas;

public sealed class OpcionesPurga
{
    public const string Seccion = "Purga";

    public bool Habilitada { get; set; } = true;

    /// <summary>Cada cuánto revisa si hay casos vencidos. Corre también al arrancar la API.</summary>
    public int IntervaloHoras { get; set; } = 24;
}

/// <summary>Purga diaria de los datos de casos cerrados cuya retención ya venció.</summary>
internal sealed class PurgaProgramada(IServiceScopeFactory alcances, OpcionesPurga opciones, ILogger<PurgaProgramada> registro)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!opciones.Habilitada)
        {
            return;
        }

        using var temporizador = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, opciones.IntervaloHoras)));
        do
        {
            try
            {
                await using var alcance = alcances.CreateAsyncScope();
                var purgados = await alcance.ServiceProvider.GetRequiredService<PurgaDatosService>().Purgar(ct);
                if (purgados > 0)
                {
                    registro.LogInformation("Purga de datos vencidos: {Casos} casos.", purgados);
                }
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                // Se reintenta en la siguiente vuelta; la API sigue funcionando.
                registro.LogError(error, "Falló la purga de datos vencidos.");
            }
        }
        while (await temporizador.WaitForNextTickAsync(ct));
    }
}
