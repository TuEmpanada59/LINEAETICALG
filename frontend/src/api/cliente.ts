/** Error de la API con el mensaje en español que envía el servidor (ProblemDetails). */
export class ErrorApi extends Error {
  readonly estado: number;

  constructor(estado: number, mensaje: string) {
    super(mensaje);
    this.estado = estado;
  }
}

/** Se emite cuando la sesión del panel vence o es revocada (401 en /api/gestion). */
export const EVENTO_SESION_VENCIDA = "lineaetica:sesion-vencida";

type Metodo = "GET" | "POST" | "DELETE";

export async function api<T = void>(ruta: string, metodo: Metodo = "GET", cuerpo?: unknown): Promise<T> {
  let respuesta: Response;
  try {
    respuesta = await fetch(ruta, {
      method: metodo,
      credentials: "same-origin",
      headers:
        cuerpo === undefined
          ? { Accept: "application/json" }
          : { Accept: "application/json", "Content-Type": "application/json" },
      body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
    });
  } catch {
    throw new ErrorApi(0, "No hay conexión con el servidor. Revise su conexión a internet e intente de nuevo.");
  }

  if (respuesta.ok) {
    return (respuesta.status === 204 ? undefined : await respuesta.json()) as T;
  }

  let mensaje = "Ocurrió un error inesperado. Intente de nuevo más tarde.";
  try {
    const problema = (await respuesta.json()) as { title?: string; detail?: string };
    mensaje = problema.detail ?? problema.title ?? mensaje;
  } catch {
    // Respuesta sin cuerpo: se usa el mensaje genérico.
  }

  if (respuesta.status === 401 && ruta.startsWith("/api/gestion") && !ruta.endsWith("/ingresar")) {
    window.dispatchEvent(new Event(EVENTO_SESION_VENCIDA));
  }

  throw new ErrorApi(respuesta.status, mensaje);
}

export function mensajeDeError(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}
