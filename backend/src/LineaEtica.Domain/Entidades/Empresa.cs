namespace LineaEtica.Domain.Entidades;

public class Empresa
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; }
}
