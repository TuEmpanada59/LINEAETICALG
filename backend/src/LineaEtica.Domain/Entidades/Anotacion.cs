namespace LineaEtica.Domain.Entidades;

/// <summary>Nota interna de la empresa. Nunca se muestra al denunciante.</summary>
public class Anotacion
{
    public long Id { get; set; }
    public Guid DenunciaId { get; set; }
    public Denuncia? Denuncia { get; set; }
    public int GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public required byte[] TextoCifrado { get; set; }
    public DateTime CreadaEn { get; set; }
}
