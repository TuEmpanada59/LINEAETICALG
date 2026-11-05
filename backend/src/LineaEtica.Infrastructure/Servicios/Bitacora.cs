using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>Nombres de las acciones de la bitácora. Nunca incluyen contenido de la denuncia ni datos personales.</summary>
public static class AccionesBitacora
{
    public const string DenunciaRecibida = "Denuncia recibida";
    public const string IngresoSeguimiento = "Ingreso al seguimiento";
    public const string IngresoFallido = "Ingreso fallido al seguimiento";
    public const string IngresoBloqueado = "Ingreso rechazado: seguimiento bloqueado";
    public const string SeguimientoBloqueado = "Seguimiento bloqueado por intentos fallidos";
    public const string ConsultaDenunciante = "Consulta del caso por el denunciante";
    public const string MensajeDenunciante = "Mensaje del denunciante";

    public const string IngresoGestor = "Ingreso al panel";
    public const string IngresoGestorFallido = "Ingreso fallido al panel";
    public const string IngresoGestorRechazado = "Ingreso rechazado: cuenta bloqueada o inactiva";
    public const string GestorBloqueado = "Cuenta bloqueada por intentos fallidos";
    public const string CambioClave = "Cambio de contraseña";
    public const string RegistroTotp = "Registro del segundo factor";
    public const string ConsultaCaso = "Consulta del caso";
    public const string CambioEstado = "Cambio de estado";
    public const string Asignacion = "Caso asignado";
    public const string Desasignacion = "Asignación retirada";
    public const string MensajeEmpresa = "Mensaje de la empresa";
    public const string Anotacion = "Anotación interna";
    public const string GestorCreado = "Gestor creado";
    public const string ClaveRestablecida = "Contraseña restablecida";
    public const string TotpRestablecido = "Segundo factor restablecido";
    public const string OficialInicial = "Oficial inicial creado";
}

public static class Bitacora
{
    /// <summary>Agrega la actuación al contexto; se guarda con el siguiente SaveChanges.</summary>
    public static void Registrar(this LineaEticaDbContext db, TipoActor actor, Guid? denunciaId, int? gestorId, string accion,
        string? detalle = null) =>
        db.Actuaciones.Add(new Actuacion
        {
            ActorTipo = actor,
            DenunciaId = denunciaId,
            GestorId = gestorId,
            Accion = accion,
            Detalle = detalle,
        });
}
