namespace LineaEtica.Domain.Entidades;

public class Asignacion
{
    public Guid DenunciaId { get; set; }
    public Denuncia? Denuncia { get; set; }
    public int GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public int AsignadaPorId { get; set; }
    public Gestor? AsignadaPor { get; set; }
    public DateTime AsignadaEn { get; set; }
}
