namespace LineaEtica.Domain.Entidades;

public class Gestor
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Correo { get; set; }
    public required string HashClave { get; set; }
    public RolGestor Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
    public byte[]? TotpSecretoCifrado { get; set; }
    public DateTime? TotpConfirmadoEn { get; set; }
    public long? TotpUltimoPaso { get; set; }
    public bool DebeCambiarClave { get; set; } = true;
    public Guid SelloSesion { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
}
