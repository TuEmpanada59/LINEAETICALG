namespace LineaEtica.Domain.Entidades;

/// <summary>Datos del denunciante a nombre propio. Las denuncias anónimas no tienen fila.</summary>
public class Identidad
{
    public Guid DenunciaId { get; set; }
    public ModoDenuncia Modo { get; set; } = ModoDenuncia.ANombrePropio;
    public Denuncia? Denuncia { get; set; }
    public required byte[] NombreCifrado { get; set; }
    public required byte[] CorreoCifrado { get; set; }
    public byte[]? TelefonoCifrado { get; set; }
    public RelacionDenunciante Relacion { get; set; }
}
