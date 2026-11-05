namespace LineaEtica.Domain.Entidades;

/// <summary>Registro de la bitácora. Nunca lleva contenido de la denuncia ni datos personales.</summary>
public class Actuacion
{
    public long Id { get; set; }
    public Guid? DenunciaId { get; set; }
    public Denuncia? Denuncia { get; set; }
    public TipoActor ActorTipo { get; set; }
    public int? GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public required string Accion { get; set; }
    public string? Detalle { get; set; }
    public DateTime CreadaEn { get; set; }
}
