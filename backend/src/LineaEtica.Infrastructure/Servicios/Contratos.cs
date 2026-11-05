using LineaEtica.Domain;

namespace LineaEtica.Infrastructure.Servicios;

public sealed record EmpresaOpcion(int Id, string Nombre);

public sealed record TextoVigente(int Id, TipoTextoLegal Tipo, int Version, string Contenido);

public sealed record Formulario(IReadOnlyList<EmpresaOpcion> Empresas, IReadOnlyList<TextoVigente> Textos);

public sealed record DatosIdentidad(string? Nombre, string? Correo, string? Telefono, RelacionDenunciante Relacion);

public sealed record NuevaDenuncia(
    int EmpresaId,
    ModoDenuncia Modo,
    CategoriaConducta Categoria,
    string? Relato,
    string? FechaHechos,
    string? Lugar,
    string? PersonasInvolucradas,
    DatosIdentidad? Identidad,
    int TextoAvisoId,
    int TextoAutorizacionId,
    bool AceptaTratamiento);

/// <summary>Acuse de recibo. La contraseña solo existe aquí: en la base queda únicamente su hash.</summary>
public sealed record DenunciaRecibida(string Codigo, string Clave, string TextoAcuse, DateTime VenceRespuestaEn);

public sealed record MensajeCaso(AutorMensaje Autor, string Texto, DateTime Fecha);

public sealed record CasoDenunciante(
    string Codigo,
    EstadoDenuncia Estado,
    DateTime RecibidaEn,
    DateTime? VenceRespuestaEn,
    DateTime? CerradaEn,
    IReadOnlyList<MensajeCaso> Mensajes);
