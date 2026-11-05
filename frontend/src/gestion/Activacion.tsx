import { useEffect, useRef, useState, type FormEvent } from "react";
import { QRCodeSVG } from "qrcode.react";
import { ArrowRight, KeyRound, LogOut, ShieldCheck, Smartphone } from "lucide-react";
import { Navigate, useNavigate } from "react-router";
import { api, mensajeDeError } from "../api/cliente";
import type { EstadoSesion, RegistroTotp } from "../api/tipos";
import { Aviso, Cargando, CodigoOtp, Girando, Marca } from "../componentes/comunes";
import { useSesion } from "./sesion";

/** Primer ingreso (o tras un restablecimiento): cambio de la clave temporal y registro del segundo factor. */
export default function Activacion() {
  const { sesion, cargando, salir } = useSesion();

  if (cargando) {
    return <Cargando />;
  }

  if (!sesion) {
    return <Navigate to="/gestion/ingresar" replace />;
  }

  if (sesion.completa) {
    return <Navigate to="/gestion" replace />;
  }

  return (
    <div className="activacion">
      <div style={{ width: "100%", maxWidth: 560 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
          <Marca destino="/gestion" subtitulo="Panel de gestión" />
          <button type="button" className="boton boton-fantasma boton-pequeno" onClick={salir}>
            <LogOut /> Salir
          </button>
        </div>
        <div className="tarjeta">
          {sesion.debeCambiarClave ? <CambiarClave /> : <RegistrarTotp />}
        </div>
        <p className="ayuda" style={{ marginTop: 14, textAlign: "center" }}>
          Paso {sesion.debeCambiarClave ? 1 : 2} de 2 · Hola, {sesion.nombre}
        </p>
      </div>
    </div>
  );
}

export function CambiarClave({ alTerminar }: { alTerminar?: () => void }) {
  const { establecer } = useSesion();
  const [actual, setActual] = useState("");
  const [nueva, setNueva] = useState("");
  const [repetida, setRepetida] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function guardar(e: FormEvent) {
    e.preventDefault();
    if (nueva.length < 12) {
      setError("La contraseña nueva debe tener al menos 12 caracteres.");
      return;
    }
    if (nueva !== repetida) {
      setError("Las contraseñas nuevas no coinciden.");
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      establecer(await api<EstadoSesion>("/api/gestion/clave", "POST", { claveActual: actual, claveNueva: nueva }));
      alTerminar?.();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form className="tarjeta-cuerpo" onSubmit={guardar} noValidate>
      <div className="encabezado-paso">
        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <KeyRound size={15} /> Contraseña
        </span>
        <h2>{alTerminar ? "Cambiar contraseña" : "Cree su contraseña"}</h2>
        <p>
          {alTerminar
            ? "Al cambiarla se cerrarán sus sesiones abiertas en otros equipos."
            : "Reemplace la contraseña temporal por una que solo usted conozca. Use al menos 12 caracteres; una frase es fácil de recordar y difícil de adivinar."}
        </p>
      </div>
      <div className="campo">
        <label className="etiqueta" htmlFor="actual">
          {alTerminar ? "Contraseña actual" : "Contraseña temporal"}
        </label>
        <input id="actual" type="password" className="entrada" autoComplete="current-password" value={actual} onChange={(e) => setActual(e.target.value)} />
      </div>
      <div className="campo">
        <label className="etiqueta" htmlFor="nueva">
          Contraseña nueva
        </label>
        <input id="nueva" type="password" className="entrada" autoComplete="new-password" value={nueva} onChange={(e) => setNueva(e.target.value)} />
        <span className="ayuda">{nueva.length > 0 && nueva.length < 12 ? `Faltan ${12 - nueva.length} caracteres.` : "Mínimo 12 caracteres."}</span>
      </div>
      <div className="campo">
        <label className="etiqueta" htmlFor="repetida">
          Repita la contraseña nueva
        </label>
        <input id="repetida" type="password" className="entrada" autoComplete="new-password" value={repetida} onChange={(e) => setRepetida(e.target.value)} />
      </div>
      {error && (
        <div style={{ marginTop: 18 }}>
          <Aviso tipo="error">{error}</Aviso>
        </div>
      )}
      <div className="acciones-formulario">
        <span />
        <button type="submit" className="boton boton-primario" disabled={enviando}>
          {enviando ? <Girando /> : null} Guardar contraseña {!alTerminar && <ArrowRight />}
        </button>
      </div>
    </form>
  );
}

function RegistrarTotp() {
  const { establecer } = useSesion();
  const navegar = useNavigate();
  const [registro, setRegistro] = useState<RegistroTotp | null>(null);
  const [codigo, setCodigo] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const solicitado = useRef(false);

  // Cada llamada genera un secreto nuevo: se pide una sola vez (el modo estricto de React monta dos veces).
  useEffect(() => {
    if (solicitado.current) {
      return;
    }
    solicitado.current = true;
    api<RegistroTotp>("/api/gestion/totp/registro", "POST")
      .then(setRegistro)
      .catch((e) => setError(mensajeDeError(e)));
  }, []);

  async function confirmar(e: FormEvent) {
    e.preventDefault();
    if (codigo.length !== 6) {
      setError("Escriba los 6 dígitos que muestra la aplicación.");
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      const sesion = await api<EstadoSesion>("/api/gestion/totp/confirmar", "POST", { codigo });
      establecer(sesion);
      navegar("/gestion", { replace: true });
    } catch (e) {
      setError(mensajeDeError(e));
      setCodigo("");
      setEnviando(false);
    }
  }

  return (
    <form className="tarjeta-cuerpo" onSubmit={confirmar} noValidate>
      <div className="encabezado-paso">
        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <Smartphone size={15} /> Segundo factor
        </span>
        <h2>Vincule su aplicación de autenticación</h2>
        <p>Cada vez que ingrese le pediremos un código de 6 dígitos que solo aparece en su celular.</p>
      </div>

      {!registro && !error && <Cargando texto="Generando el código QR…" />}

      {registro && (
        <>
          <div className="registro-totp">
            <div className="qr">
              <QRCodeSVG value={registro.uri} size={156} marginSize={0} />
            </div>
            <ol>
              <li>
                Abra <strong>Microsoft Authenticator</strong>, Google Authenticator u otra aplicación similar.
              </li>
              <li>Elija agregar una cuenta y escanee este código.</li>
              <li>Escriba abajo los 6 dígitos que le muestra.</li>
            </ol>
          </div>
          <div className="secreto-manual">
            ¿No puede escanear? Escriba esta clave en la aplicación:
            <code>{registro.secreto.replace(/(.{4})/g, "$1 ").trim()}</code>
          </div>
          <div className="campo" style={{ marginTop: 22 }}>
            <span className="etiqueta">Código de la aplicación</span>
            <CodigoOtp valor={codigo} onCambio={setCodigo} etiqueta="Código de 6 dígitos" autoFocus />
          </div>
        </>
      )}

      {error && (
        <div style={{ marginTop: 18 }}>
          <Aviso tipo="error">{error}</Aviso>
        </div>
      )}

      <div className="acciones-formulario">
        <span />
        <button type="submit" className="boton boton-primario" disabled={enviando || !registro}>
          {enviando ? <Girando /> : <ShieldCheck />} Activar y entrar
        </button>
      </div>
    </form>
  );
}
