using LineaEtica.Domain;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>
/// Estado de la sesión de un gestor. Mientras deba cambiar la contraseña o registrar el segundo factor,
/// la sesión está incompleta y solo permite esos dos trámites.
/// </summary>
public sealed record SesionGestor(int Id, string Nombre, string Correo, RolGestor Rol, Guid Sello, bool DebeCambiarClave, bool TotpConfirmado)
{
    public bool Completa => TotpConfirmado && !DebeCambiarClave;
}

public sealed record RegistroTotp(string Uri, string Secreto);

public sealed record CasoResumen(
    Guid Id,
    string Codigo,
    string Empresa,
    CategoriaConducta Categoria,
    ModoDenuncia Modo,
    EstadoDenuncia Estado,
    DateTime RecibidaEn,
    DateTime? VenceRespuestaEn,
    IReadOnlyList<string> Asignados);

public sealed record IdentidadCaso(string Nombre, string Correo, string? Telefono, RelacionDenunciante Relacion);

public sealed record AsignadoCaso(int GestorId, string Nombre, DateTime AsignadaEn);

public sealed record MensajeGestion(AutorMensaje Autor, string? Gestor, string Texto, DateTime Fecha);

public sealed record AnotacionCaso(string Gestor, string Texto, DateTime Fecha);

public sealed record ActuacionCaso(TipoActor Actor, string? Gestor, string Accion, string? Detalle, DateTime Fecha);

/// <summary>Detalle completo del caso. Si los datos ya se purgaron, los campos cifrados vienen vacíos.</summary>
public sealed record CasoGestion(
    Guid Id,
    string Codigo,
    string Empresa,
    ModoDenuncia Modo,
    CategoriaConducta Categoria,
    EstadoDenuncia Estado,
    MotivoCierre? MotivoCierre,
    DateTime RecibidaEn,
    DateTime? VenceRespuestaEn,
    DateTime? CerradaEn,
    DateTime? BorrarDatosEn,
    bool DatosEliminados,
    string? Relato,
    string? FechaHechos,
    string? Lugar,
    string? PersonasInvolucradas,
    IdentidadCaso? Identidad,
    IReadOnlyList<AsignadoCaso> Asignados,
    IReadOnlyList<MensajeGestion> Mensajes,
    IReadOnlyList<AnotacionCaso> Anotaciones,
    IReadOnlyList<ActuacionCaso> Bitacora);

public sealed record CambioEstado(EstadoDenuncia Estado, MotivoCierre? MotivoCierre);

public sealed record GestorResumen(
    int Id,
    string Nombre,
    string Correo,
    RolGestor Rol,
    bool Activo,
    bool TotpConfirmado,
    bool DebeCambiarClave,
    DateTime? BloqueadoHasta);

public sealed record NuevoGestor(string? Nombre, string? Correo, RolGestor Rol);

/// <summary>La contraseña temporal solo existe en esta respuesta: el oficial se la entrega al gestor.</summary>
public sealed record ClaveTemporal(int GestorId, string Clave);
