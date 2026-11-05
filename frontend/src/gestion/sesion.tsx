import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { api, ErrorApi, EVENTO_SESION_VENCIDA } from "../api/cliente";
import type { EstadoSesion } from "../api/tipos";

interface ContextoSesion {
  sesion: EstadoSesion | null;
  cargando: boolean;
  establecer: (sesion: EstadoSesion | null) => void;
  salir: () => Promise<void>;
}

const Contexto = createContext<ContextoSesion | null>(null);

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<EstadoSesion | null>(null);
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    api<EstadoSesion>("/api/gestion/sesion")
      .then(setSesion)
      .catch((e) => {
        if (!(e instanceof ErrorApi) || e.estado !== 401) {
          console.error(e);
        }
      })
      .finally(() => setCargando(false));

    const alVencer = () => setSesion(null);
    window.addEventListener(EVENTO_SESION_VENCIDA, alVencer);
    return () => window.removeEventListener(EVENTO_SESION_VENCIDA, alVencer);
  }, []);

  const salir = useCallback(async () => {
    await api("/api/gestion/salir", "POST").catch(() => undefined);
    setSesion(null);
  }, []);

  return <Contexto.Provider value={{ sesion, cargando, establecer: setSesion, salir }}>{children}</Contexto.Provider>;
}

export function useSesion(): ContextoSesion {
  const contexto = useContext(Contexto);
  if (!contexto) {
    throw new Error("useSesion debe usarse dentro de ProveedorSesion.");
  }
  return contexto;
}
