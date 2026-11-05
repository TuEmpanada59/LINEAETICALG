namespace LineaEtica.Domain.Entidades;

/// <summary>Versión de un texto legal. Una versión publicada no se edita: se publica una nueva.</summary>
public class TextoLegal
{
    public int Id { get; set; }
    public TipoTextoLegal Tipo { get; set; }
    public int Version { get; set; }
    public required string Contenido { get; set; }
    public DateTime VigenteDesde { get; set; }
    public int? PublicadoPorId { get; set; }
    public Gestor? PublicadoPor { get; set; }
}
