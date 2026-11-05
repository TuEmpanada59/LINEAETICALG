namespace LineaEtica.Domain.Entidades;

public class ParametroConfiguracion
{
    public const string PlazoRespuestaDias = nameof(PlazoRespuestaDias);
    public const string RetencionDiasTrasCierre = nameof(RetencionDiasTrasCierre);
    public const string TextoAcuse = nameof(TextoAcuse);

    public required string Clave { get; set; }
    public required string Valor { get; set; }
    public string? Descripcion { get; set; }
    public int? ModificadaPorId { get; set; }
    public Gestor? ModificadaPor { get; set; }
    public DateTime ModificadaEn { get; set; }
}
