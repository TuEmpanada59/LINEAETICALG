namespace LineaEtica.Domain;

public enum ModoDenuncia : byte { ANombrePropio = 1, Anonimo = 2 }

public enum RelacionDenunciante : byte { Empleado = 1, Proveedor = 2, Cliente = 3, Otro = 4 }

public enum CategoriaConducta : byte
{
    Fraude = 1,
    Soborno = 2,
    Corrupcion = 3,
    ConflictoIntereses = 4,
    AcosoLaboral = 5,
    Otra = 6,
}

public enum EstadoDenuncia : byte { Recibida = 1, EnRevision = 2, EnInvestigacion = 3, Cerrada = 4 }

public enum MotivoCierre : byte { Fundada = 1, NoFundada = 2, InformacionInsuficiente = 3, FueraDeAlcance = 4 }

public enum RolGestor : byte { OficialCumplimiento = 1, Gestor = 2 }

public enum AutorMensaje : byte { Denunciante = 1, Empresa = 2 }

public enum TipoActor : byte { Denunciante = 1, Gestor = 2, Sistema = 3 }

public enum TipoTextoLegal : byte { AvisoPrivacidad = 1, Autorizacion = 2, PoliticaNoRepresalias = 3 }
