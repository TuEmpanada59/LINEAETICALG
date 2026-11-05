import type {
  CategoriaConducta,
  EstadoDenuncia,
  ModoDenuncia,
  MotivoCierre,
  RelacionDenunciante,
  RolGestor,
  TipoActor,
} from "./tipos";

export const categorias: Record<CategoriaConducta, { nombre: string; descripcion: string }> = {
  Fraude: {
    nombre: "Fraude",
    descripcion: "Engaños para obtener un beneficio: alteración de registros, robos o desvío de recursos.",
  },
  Soborno: {
    nombre: "Soborno",
    descripcion: "Ofrecer, pedir o recibir dinero, regalos o favores para influir en una decisión.",
  },
  Corrupcion: {
    nombre: "Corrupción",
    descripcion: "Abuso de un cargo o de una posición para beneficio propio o de terceros.",
  },
  ConflictoIntereses: {
    nombre: "Conflicto de intereses",
    descripcion: "Decisiones influidas por intereses personales, familiares o económicos.",
  },
  AcosoLaboral: {
    nombre: "Acoso laboral",
    descripcion: "Maltrato, intimidación, humillación o discriminación en el trabajo.",
  },
  Otra: {
    nombre: "Otra conducta",
    descripcion: "Cualquier otra conducta contraria a la ética, a las políticas o a la ley.",
  },
};

export const estados: Record<EstadoDenuncia, string> = {
  Recibida: "Recibida",
  EnRevision: "En revisión",
  EnInvestigacion: "En investigación",
  Cerrada: "Cerrada",
};

export const descripcionEstados: Record<EstadoDenuncia, string> = {
  Recibida: "Su denuncia llegó al equipo de cumplimiento.",
  EnRevision: "El equipo está analizando la información.",
  EnInvestigacion: "Se están verificando los hechos.",
  Cerrada: "El equipo terminó la gestión del caso.",
};

export const ordenEstados: EstadoDenuncia[] = ["Recibida", "EnRevision", "EnInvestigacion", "Cerrada"];

export const motivos: Record<MotivoCierre, string> = {
  Fundada: "Fundada",
  NoFundada: "No fundada",
  InformacionInsuficiente: "Información insuficiente",
  FueraDeAlcance: "Fuera de alcance",
};

export const modos: Record<ModoDenuncia, string> = {
  ANombrePropio: "A nombre propio",
  Anonimo: "Anónima",
};

export const relaciones: Record<RelacionDenunciante, string> = {
  Empleado: "Empleado",
  Proveedor: "Proveedor",
  Cliente: "Cliente",
  Otro: "Otro",
};

export const roles: Record<RolGestor, string> = {
  OficialCumplimiento: "Oficial de cumplimiento",
  Gestor: "Gestor",
};

export const actores: Record<TipoActor, string> = {
  Denunciante: "Denunciante",
  Gestor: "Gestor",
  Sistema: "Sistema",
};

const zona = "America/Bogota";
const formatoFecha = new Intl.DateTimeFormat("es-CO", { dateStyle: "long", timeZone: zona });
const formatoFechaCorta = new Intl.DateTimeFormat("es-CO", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: zona,
});
const formatoFechaHora = new Intl.DateTimeFormat("es-CO", { dateStyle: "medium", timeStyle: "short", timeZone: zona });

export const fecha = (iso: string) => formatoFecha.format(new Date(iso));
export const fechaCorta = (iso: string) => formatoFechaCorta.format(new Date(iso));
export const fechaHora = (iso: string) => formatoFechaHora.format(new Date(iso));

/** Días completos que faltan hasta la fecha (negativo si ya pasó). */
export function diasHasta(iso: string): number {
  return Math.ceil((new Date(iso).getTime() - Date.now()) / 86_400_000);
}

export function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/);
  const primera = partes[0]?.[0] ?? "";
  const ultima = partes.length > 1 ? partes[partes.length - 1][0] : "";
  return (primera + ultima).toUpperCase();
}
