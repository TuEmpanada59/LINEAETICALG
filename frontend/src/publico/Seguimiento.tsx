import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { ArrowLeft, Check, CheckCircle2, KeyRound, LogOut, MessagesSquare, RefreshCw, Send } from "lucide-react";
import { Link } from "react-router";
import { api, ErrorApi, mensajeDeError } from "../api/cliente";
import { descripcionEstados, estados, fecha, fechaHora, ordenEstados } from "../api/textos";
import type { CasoDenunciante } from "../api/tipos";
import { AreaTexto, Aviso, Cargando, ChipEstado, Girando } from "../componentes/comunes";

export default function Seguimiento() {
  const [caso, setCaso] = useState<CasoDenunciante | null>(null);
  const [revisando, setRevisando] = useState(true);

  const cargar = useCallback(async () => {
    try {
      setCaso(await api<CasoDenunciante>("/api/publico/seguimiento"));
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 401) {
        setCaso(null);
      } else {
        throw e;
      }
    }
  }, []);

  // Si ya hay una sesión de seguimiento abierta (cookie), entra directo.
  useEffect(() => {
    cargar()
      .catch(() => setCaso(null))
      .finally(() => setRevisando(false));
  }, [cargar]);

  async function salir() {
    await api("/api/publico/seguimiento/salir", "POST").catch(() => undefined);
    setCaso(null);
  }

  if (revisando) {
    return <Cargando />;
  }

  return caso ? <Caso caso={caso} recargar={cargar} salir={salir} /> : <Ingreso alIngresar={cargar} />;
}

function Ingreso({ alIngresar }: { alIngresar: () => Promise<void> }) {
  const [codigo, setCodigo] = useState("");
  const [clave, setClave] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function ingresar(e: FormEvent) {
    e.preventDefault();
    if (!codigo.trim() || !clave.trim()) {
      setError("Escriba el código del caso y la contraseña.");
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      await api("/api/publico/seguimiento/ingresar", "POST", { codigo, clave });
      await alIngresar();
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <div className="contenedor">
      <div className="seguimiento-ingreso">
        <div>
          <Link to="/" className="migas">
            <ArrowLeft /> Inicio
          </Link>
          <h1>Consultar mi caso</h1>
          <p>Use el código y la contraseña que recibió al enviar su denuncia.</p>
          <ul className="lista-puntos">
            <li>
              <CheckCircle2 /> Vea en qué etapa está la gestión de su caso.
            </li>
            <li>
              <CheckCircle2 /> Lea las preguntas y respuestas del equipo de cumplimiento.
            </li>
            <li>
              <CheckCircle2 /> Aporte información nueva en cualquier momento.
            </li>
          </ul>
        </div>

        <form className="tarjeta tarjeta-cuerpo" onSubmit={ingresar} noValidate>
          <div className="campo">
            <label className="etiqueta" htmlFor="codigo">
              Código del caso
            </label>
            <input
              id="codigo"
              className="entrada entrada-codigo"
              placeholder="LG-XXXX-XXXX"
              autoComplete="off"
              spellCheck={false}
              maxLength={20}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
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
              className="entrada entrada-codigo"
              placeholder="XXXX-XXXX-XXXX-XXXX"
              autoComplete="off"
              spellCheck={false}
              maxLength={40}
              value={clave}
              onChange={(e) => setClave(e.target.value)}
            />
          </div>

          {error && (
            <div style={{ marginTop: 18 }}>
              <Aviso tipo="error">{error}</Aviso>
            </div>
          )}

          <button type="submit" className="boton boton-primario boton-grande boton-bloque" style={{ marginTop: 24 }} disabled={enviando}>
            {enviando ? <Girando /> : <KeyRound />} Ingresar
          </button>
          <p className="ayuda" style={{ marginTop: 14, textAlign: "center" }}>
            ¿Perdió sus datos? Por seguridad no se pueden recuperar. Puede presentar una nueva denuncia.
          </p>
        </form>
      </div>
    </div>
  );
}

function Caso({
  caso,
  recargar,
  salir,
}: {
  caso: CasoDenunciante;
  recargar: () => Promise<void>;
  salir: () => Promise<void>;
}) {
  const [texto, setTexto] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [enviado, setEnviado] = useState(false);
  const [actualizando, setActualizando] = useState(false);
  const final = useRef<HTMLDivElement>(null);
  const actual = ordenEstados.indexOf(caso.estado);

  useEffect(() => {
    final.current?.scrollIntoView({ block: "nearest" });
  }, [caso.mensajes.length]);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!texto.trim()) {
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      await api("/api/publico/seguimiento/mensajes", "POST", { texto });
      setTexto("");
      setEnviado(true);
      setTimeout(() => setEnviado(false), 3000);
      await recargar();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  async function actualizar() {
    setActualizando(true);
    await recargar().catch(() => undefined);
    setActualizando(false);
  }

  return (
    <div className="contenedor">
      <div className="caso-publico">
        <section className="tarjeta" aria-labelledby="titulo-buzon" style={{ overflow: "hidden" }}>
          <div className="tarjeta-encabezado">
            <h2 id="titulo-buzon" style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <MessagesSquare size={19} /> Conversación con el equipo de cumplimiento
            </h2>
            <button type="button" className="boton boton-fantasma boton-pequeno" onClick={actualizar} disabled={actualizando}>
              <RefreshCw className={actualizando ? "girando" : undefined} /> Actualizar
            </button>
          </div>

          <div className="conversacion" aria-live="polite">
            {caso.mensajes.length === 0 ? (
              <div className="vacio" style={{ margin: "auto" }}>
                <MessagesSquare />
                <strong>Aún no hay mensajes</strong>
                <span>Cuando el equipo le escriba, verá aquí su mensaje. También puede escribir usted primero.</span>
              </div>
            ) : (
              caso.mensajes.map((m, i) => (
                <div key={i} className={`burbuja ${m.autor === "Denunciante" ? "burbuja-propia" : "burbuja-otra"}`}>
                  {m.texto}
                  <span className="burbuja-meta">
                    {m.autor === "Denunciante" ? "Usted" : "Equipo de cumplimiento"} · {fechaHora(m.fecha)}
                  </span>
                </div>
              ))
            )}
            <div ref={final} />
          </div>

          <form className="redactor" onSubmit={enviar}>
            <label className="oculto-visual" htmlFor="mensaje">
              Escriba un mensaje
            </label>
            <AreaTexto
              id="mensaje"
              valor={texto}
              onCambio={setTexto}
              maximo={5000}
              placeholder="Escriba aquí información adicional o responda las preguntas del equipo…"
            />
            {error && <Aviso tipo="error">{error}</Aviso>}
            <div className="redactor-pie">
              <span className="ayuda">{enviado ? "Mensaje enviado." : "Sus mensajes se guardan cifrados."}</span>
              <button type="submit" className="boton boton-primario" disabled={enviando || !texto.trim()}>
                {enviando ? <Girando /> : enviado ? <Check /> : <Send />} Enviar
              </button>
            </div>
          </form>
        </section>

        <aside className="columna">
          <section className="tarjeta tarjeta-cuerpo">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginBottom: 6 }}>
              <span className="texto-secundario texto-pequeno">Caso</span>
              <ChipEstado estado={caso.estado} />
            </div>
            <h1 className="mono" style={{ fontSize: 22, letterSpacing: 0 }}>
              {caso.codigo}
            </h1>
            <hr className="separador-h" />
            <ol className="linea-estados">
              {ordenEstados.map((estado, i) => (
                <li key={estado} data-estado={i < actual || caso.estado === "Cerrada" ? "hecho" : i === actual ? "actual" : "pendiente"}>
                  <span className="punto">{(i < actual || caso.estado === "Cerrada") && <Check strokeWidth={3} />}</span>
                  <span>
                    {estados[estado]}
                    {i === actual && <small>{descripcionEstados[estado]}</small>}
                  </span>
                </li>
              ))}
            </ol>
            <hr className="separador-h" />
            <dl className="datos-caso">
              <div>
                <dt>Recibida</dt>
                <dd>{fecha(caso.recibidaEn)}</dd>
              </div>
              {caso.venceRespuestaEn && caso.estado !== "Cerrada" && (
                <div>
                  <dt>Respuesta a más tardar</dt>
                  <dd>{fecha(caso.venceRespuestaEn)}</dd>
                </div>
              )}
              {caso.cerradaEn && (
                <div>
                  <dt>Cerrada</dt>
                  <dd>{fecha(caso.cerradaEn)}</dd>
                </div>
              )}
            </dl>
          </section>
          <button type="button" className="boton boton-secundario boton-bloque" onClick={salir}>
            <LogOut /> Cerrar sesión
          </button>
        </aside>
      </div>
    </div>
  );
}
