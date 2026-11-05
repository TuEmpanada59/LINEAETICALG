namespace LineaEtica.Domain.Entidades;

public class Denuncia
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public string? HashClave { get; set; }
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public ModoDenuncia Modo { get; set; }
    public CategoriaConducta Categoria { get; set; }
    public byte[]? RelatoCifrado { get; set; }
    public EstadoDenuncia Estado { get; set; } = EstadoDenuncia.Recibida;
    public DateTime CreadaEn { get; set; }
    public DateTime? VenceRespuestaEn { get; set; }
    public DateTime? CerradaEn { get; set; }
    public DateTime? BorrarDatosEn { get; set; }
    public bool DatosPersonalesEliminados { get; set; }
    public DateTime AceptoTratamientoEn { get; set; }
    public int TextoAvisoId { get; set; }
    public TextoLegal? TextoAviso { get; set; }
    public int TextoAutorizacionId { get; set; }
    public TextoLegal? TextoAutorizacion { get; set; }
    public MotivoCierre? MotivoCierre { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadaHasta { get; set; }
    public byte[]? FechaHechosCifrada { get; set; }
    public byte[]? LugarCifrado { get; set; }
    public byte[]? PersonasInvolucradasCifrado { get; set; }
    public byte[] Version { get; set; } = [];

    public Identidad? Identidad { get; set; }
    public List<Asignacion> Asignaciones { get; set; } = [];
    public List<Mensaje> Mensajes { get; set; } = [];
    public List<Anotacion> Anotaciones { get; set; } = [];
}
