using LineaEtica.Domain;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>
/// Elimina el contenido de los casos cerrados cuya fecha de borrado ya pasó: relato, datos de los hechos,
/// identidad, mensajes, anotaciones y la contraseña de seguimiento. Queda solo lo necesario para estadísticas
/// y auditoría: código, empresa, categoría, estado, fechas, asignaciones y bitácora.
/// </summary>
public sealed class PurgaDatosService(LineaEticaDbContext db, TimeProvider reloj)
{
    private const int TamanoLote = 200;

    /// <summary>Devuelve cuántos casos purgó.</summary>
    public async Task<int> Purgar(CancellationToken ct = default)
    {
        var total = 0;
        while (true)
        {
            var ahora = reloj.Ahora();
            var vencidos = await db.Denuncias
                .Where(d => !d.DatosPersonalesEliminados && d.Estado == EstadoDenuncia.Cerrada && d.BorrarDatosEn <= ahora)
                .OrderBy(d => d.BorrarDatosEn)
                .Select(d => d.Id)
                .Take(TamanoLote)
                .ToListAsync(ct);

            foreach (var id in vencidos)
            {
                total += await PurgarCaso(id, ct) ? 1 : 0;
            }

            if (vencidos.Count < TamanoLote)
            {
                return total;
            }
        }
    }

    /// <summary>Cada caso en su propia transacción: si falla uno, los demás siguen.</summary>
    private Task<bool> PurgarCaso(Guid id, CancellationToken ct) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            // La condición se repite: si otra instancia ya lo purgó, no hace nada.
            var actualizados = await db.Denuncias
                .Where(d => d.Id == id && !d.DatosPersonalesEliminados && d.Estado == EstadoDenuncia.Cerrada)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.RelatoCifrado, (byte[]?)null)
                    .SetProperty(d => d.FechaHechosCifrada, (byte[]?)null)
                    .SetProperty(d => d.LugarCifrado, (byte[]?)null)
                    .SetProperty(d => d.PersonasInvolucradasCifrado, (byte[]?)null)
                    .SetProperty(d => d.HashClave, (string?)null)
                    .SetProperty(d => d.DatosPersonalesEliminados, true), ct);

            if (actualizados == 0)
            {
                return false;
            }

            await db.Identidades.Where(i => i.DenunciaId == id).ExecuteDeleteAsync(ct);
            await db.Mensajes.Where(m => m.DenunciaId == id).ExecuteDeleteAsync(ct);
            await db.Anotaciones.Where(a => a.DenunciaId == id).ExecuteDeleteAsync(ct);

            db.Registrar(TipoActor.Sistema, id, null, AccionesBitacora.DatosEliminados);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return true;
        });
}
