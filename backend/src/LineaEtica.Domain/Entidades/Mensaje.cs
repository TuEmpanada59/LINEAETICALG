namespace LineaEtica.Domain.Entidades;

public class Mensaje
{
    public long Id { get; set; }
    public Guid DenunciaId { get; set; }
    public Denuncia? Denuncia { get; set; }
    public AutorMensaje Autor { get; set; }
    public int? GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public required byte[] TextoCifrado { get; set; }
    public DateTime CreadoEn { get; set; }
}
