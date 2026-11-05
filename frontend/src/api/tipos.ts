// Contratos de la API (los enums viajan como texto).

export type ModoDenuncia = "ANombrePropio" | "Anonimo";
export type RelacionDenunciante = "Empleado" | "Proveedor" | "Cliente" | "Otro";
export type CategoriaConducta =
  | "Fraude"
  | "Soborno"
  | "Corrupcion"
  | "ConflictoIntereses"
  | "AcosoLaboral"
  | "Otra";
export type EstadoDenuncia = "Recibida" | "EnRevision" | "EnInvestigacion" | "Cerrada";
export type MotivoCierre = "Fundada" | "NoFundada" | "InformacionInsuficiente" | "FueraDeAlcance";
export type RolGestor = "OficialCumplimiento" | "Gestor";
export type AutorMensaje = "Denunciante" | "Empresa";
export type TipoActor = "Denunciante" | "Gestor" | "Sistema";
export type TipoTextoLegal = "AvisoPrivacidad" | "Autorizacion" | "PoliticaNoRepresalias";

export interface Formulario {
  empresas: { id: number; nombre: string }[];
  textos: { id: number; tipo: TipoTextoLegal; version: number; contenido: string }[];
}

export interface NuevaDenuncia {
  empresaId: number;
  modo: ModoDenuncia;
  categoria: CategoriaConducta;
  relato: string;
  fechaHechos: string | null;
  lugar: string | null;
  personasInvolucradas: string | null;
  identidad: {
    nombre: string;
    correo: string;
    telefono: string | null;
    relacion: RelacionDenunciante;
  } | null;
  textoAvisoId: number;
  textoAutorizacionId: number;
  aceptaTratamiento: boolean;
}

export interface DenunciaRecibida {
  codigo: string;
  clave: string;
  textoAcuse: string;
  venceRespuestaEn: string;
}

export interface MensajeCaso {
  autor: AutorMensaje;
  texto: string;
  fecha: string;
}

export interface CasoDenunciante {
  codigo: string;
  estado: EstadoDenuncia;
  recibidaEn: string;
  venceRespuestaEn: string | null;
  cerradaEn: string | null;
  mensajes: MensajeCaso[];
}

export interface EstadoSesion {
  nombre: string;
  correo: string;
  rol: RolGestor;
  debeCambiarClave: boolean;
  totpConfirmado: boolean;
  completa: boolean;
}

export interface RegistroTotp {
  uri: string;
  secreto: string;
}

export interface CasoResumen {
  id: string;
  codigo: string;
  empresa: string;
  categoria: CategoriaConducta;
  modo: ModoDenuncia;
  estado: EstadoDenuncia;
  recibidaEn: string;
  venceRespuestaEn: string | null;
  asignados: string[];
}

export interface CasoGestion {
  id: string;
  codigo: string;
  empresa: string;
  modo: ModoDenuncia;
  categoria: CategoriaConducta;
  estado: EstadoDenuncia;
  motivoCierre: MotivoCierre | null;
  recibidaEn: string;
  venceRespuestaEn: string | null;
  cerradaEn: string | null;
  borrarDatosEn: string | null;
  datosEliminados: boolean;
  relato: string | null;
  fechaHechos: string | null;
  lugar: string | null;
  personasInvolucradas: string | null;
  identidad: { nombre: string; correo: string; telefono: string | null; relacion: RelacionDenunciante } | null;
  asignados: { gestorId: number; nombre: string; asignadaEn: string }[];
  mensajes: { autor: AutorMensaje; gestor: string | null; texto: string; fecha: string }[];
  anotaciones: { gestor: string; texto: string; fecha: string }[];
  bitacora: { actor: TipoActor; gestor: string | null; accion: string; detalle: string | null; fecha: string }[];
}

export interface GestorResumen {
  id: number;
  nombre: string;
  correo: string;
  rol: RolGestor;
  activo: boolean;
  totpConfirmado: boolean;
  debeCambiarClave: boolean;
  bloqueadoHasta: string | null;
}

export interface ClaveTemporal {
  gestorId: number;
  clave: string;
}
