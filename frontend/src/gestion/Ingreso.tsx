import { useState, type FormEvent } from "react";
import { LogIn } from "lucide-react";
import { Navigate, useNavigate } from "react-router";
import { api, mensajeDeError } from "../api/cliente";
import type { EstadoSesion } from "../api/tipos";
import { Aviso, Cargando, CodigoOtp, Girando, Marca } from "../componentes/comunes";
import { useSesion } from "./sesion";

export default function Ingreso() {
  const { sesion, cargando, establecer } = useSesion();
  const navegar = useNavigate();
  const [correo, setCorreo] = useState("");
  const [clave, setClave] = useState("");
  const [codigo, setCodigo] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (cargando) {
    return <Cargando />;
  }

  if (sesion) {
    return <Navigate to={sesion.completa ? "/gestion" : "/gestion/activar"} replace />;
  }

  async function ingresar(e: FormEvent) {
    e.preventDefault();
    if (!correo.trim() || !clave) {
      setError("Escriba su correo y su contraseña.");
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      const nueva = await api<EstadoSesion>("/api/gestion/ingresar", "POST", {
        correo,
        clave,
        codigo: codigo || null,
      });
      establecer(nueva);
      navegar(nueva.completa ? "/gestion" : "/gestion/activar", { replace: true });
    } catch (e) {
      setError(mensajeDeError(e));
      setCodigo("");
      setEnviando(false);
    }
  }

  return (
    <div className="ingreso-panel">
      <aside className="ingreso-lateral">
        <Marca destino="/" subtitulo="Panel de gestión" />
        <div>
          <h1>Cada caso merece una gestión cuidadosa y confidencial.</h1>
          <p>Acceso exclusivo para el equipo de cumplimiento. Todas las consultas quedan registradas en la bitácora.</p>
        </div>
        <footer>Grupo Londoño Gómez · Uso interno</footer>
      </aside>

      <main className="ingreso-formulario">
        <div>
          <h2>Ingresar al panel</h2>
          <p>Use su correo corporativo, su contraseña y el código de su aplicación de autenticación.</p>
        </div>

        <form onSubmit={ingresar} noValidate>
          <div className="campo">
            <label className="etiqueta" htmlFor="correo">
              Correo
            </label>
            <input
              id="correo"
              type="email"
              className="entrada"
              autoComplete="username"
              value={correo}
              onChange={(e) => setCorreo(e.target.value)}
              autoFocus
            />
          </div>
          <div className="campo">
            <label className="etiqueta" htmlFor="clave">
              Contraseña
            </label>
            <input
              id="clave"
              type="password"
              className="entrada"
              autoComplete="current-password"
              value={clave}
              onChange={(e) => setClave(e.target.value)}
            />
          </div>
          <div className="campo">
            <span className="etiqueta">Código de verificación</span>
            <CodigoOtp valor={codigo} onCambio={setCodigo} etiqueta="Código de verificación de 6 dígitos" />
            <span className="ayuda">Si es su primer ingreso, o le restablecieron el segundo factor, déjelo vacío.</span>
          </div>

          {error && (
            <div style={{ marginTop: 18 }}>
              <Aviso tipo="error">{error}</Aviso>
            </div>
          )}

          <button type="submit" className="boton boton-primario boton-grande boton-bloque" style={{ marginTop: 26 }} disabled={enviando}>
            {enviando ? <Girando /> : <LogIn />} Ingresar
          </button>
        </form>
      </main>
    </div>
  );
}
