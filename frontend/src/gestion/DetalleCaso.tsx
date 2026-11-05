import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import {
  ArrowLeft,
  FileText,
  Lock,
  MessagesSquare,
  NotebookPen,
  Plus,
  Send,
  ShieldAlert,
  UserMinus,
  UserRound,
} from "lucide-react";
import { Link, useParams } from "react-router";
import { api, ErrorApi, mensajeDeError } from "../api/cliente";
import {
  actores,
  categorias,
  estados,
  fecha,
  fechaHora,
  iniciales,
  modos,
  motivos,
  ordenEstados,
  relaciones,
} from "../api/textos";
import type { CasoGestion, EstadoDenuncia, GestorResumen, MotivoCierre } from "../api/tipos";
import { AreaTexto, Aviso, Cargando, ChipEstado, Girando, Modal, Plazo } from "../componentes/comunes";
import { useSesion } from "./sesion";

type Pestana = "relato" | "mensajes" | "anotaciones";

export default function DetalleCaso() {
  const { id } = useParams();
  const { sesion } = useSesion();
  const [caso, setCaso] = useState<CasoGestion | null>(null);
  const [error, setError] = useState<{ estado: number; mensaje: string } | null>(null);
  const [pestana, setPestana] = useState<Pestana>("relato");
  const [modal, setModal] = useState<"estado" | "asignar" | null>(null);
  const cargado = useRef<string | null>(null);

  // Cada consulta del detalle queda en la bitácora: se pide solo cuando hace falta.
  const cargar = useCallback(async () => {
    try {
      setCaso(await api<CasoGestion>(`/api/gestion/casos/${id}`));
      setError(null);
    } catch (e) {
      setError({ estado: e instanceof ErrorApi ? e.estado : 0, mensaje: mensajeDeError(e) });
    }
  }, [id]);

  useEffect(() => {
    if (cargado.current === id) {
      return;
    }
    cargado.current = id ?? null;
    void cargar();
  }, [id, cargar]);

  if (error && !caso) {
    return (
      <>
        <Link to="/gestion" className="migas">
          <ArrowLeft /> Casos
        </Link>
        <Aviso tipo="error">{error.estado === 404 ? "El caso no existe o no tiene acceso a él." : error.mensaje}</Aviso>
      </>
    );
  }

  if (!caso || !sesion) {
    return <Cargando texto="Abriendo el caso…" />;
  }

  const esOficial = sesion.rol === "OficialCumplimiento";
  const editable = !caso.datosEliminados;

  return (
    <>
      <Link to="/gestion" className="migas">
        <ArrowLeft /> Casos
      </Link>

      <div className="detalle-cabecera">
        <div>
          <h1>
            {caso.codigo} <ChipEstado estado={caso.estado} />
          </h1>
          <p>
            {caso.empresa} · {categorias[caso.categoria].nombre} · Recibida el {fecha(caso.recibidaEn)}
          </p>
        </div>
        <div className="botones">
          {esOficial && editable && (
            <button type="button" className="boton boton-secundario" onClick={() => setModal("asignar")}>
              <Plus /> Asignar
            </button>
          )}
          {editable && caso.estado !== "Cerrada" && (
            <button type="button" className="boton boton-primario" onClick={() => setModal("estado")}>
              Cambiar estado
            </button>
          )}
        </div>
      </div>

      {caso.datosEliminados && (
        <div style={{ marginBottom: 20 }}>
          <Aviso tipo="advertencia">
            El plazo de conservación venció y los datos del caso se eliminaron. Solo quedan el estado, las fechas y la bitácora.
          </Aviso>
        </div>
      )}

      {error && (
        <div style={{ marginBottom: 20 }}>
          <Aviso tipo="error">{error.mensaje}</Aviso>
        </div>
      )}

      <div className="detalle">
        <section className="tarjeta" style={{ overflow: "hidden" }}>
          <div className="pestanas" role="tablist">
            <button type="button" role="tab" className="pestana" aria-selected={pestana === "relato"} onClick={() => setPestana("relato")}>
              <FileText size={16} /> Relato
            </button>
            <button type="button" role="tab" className="pestana" aria-selected={pestana === "mensajes"} onClick={() => setPestana("mensajes")}>
              <MessagesSquare size={16} /> Buzón <span className="cuenta">{caso.mensajes.length}</span>
            </button>
            <button type="button" role="tab" className="pestana" aria-selected={pestana === "anotaciones"} onClick={() => setPestana("anotaciones")}>
              <NotebookPen size={16} /> Anotaciones <span className="cuenta">{caso.anotaciones.length}</span>
            </button>
          </div>

          {pestana === "relato" && <Relato caso={caso} />}
          {pestana === "mensajes" && <Buzon caso={caso} editable={editable} recargar={cargar} />}
          {pestana === "anotaciones" && <Anotaciones caso={caso} editable={editable} recargar={cargar} />}
        </section>

        <aside className="columna">
          <section className="tarjeta tarjeta-cuerpo">
            <dl className="datos-caso">
              <div>
                <dt>Modo</dt>
                <dd>{modos[caso.modo]}</dd>
              </div>
              {caso.estado !== "Cerrada" && caso.venceRespuestaEn && (
                <div>
                  <dt>Plazo de respuesta</dt>
                  <dd>
                    <Plazo vence={caso.venceRespuestaEn} cerrada={false} />
                    <span className="celda-secundaria">{fecha(caso.venceRespuestaEn)}</span>
                  </dd>
                </div>
              )}
              {caso.cerradaEn && (
                <div>
                  <dt>Cerrada</dt>
                  <dd>
                    {fecha(caso.cerradaEn)}
                    {caso.motivoCierre && <span className="celda-secundaria">Motivo: {motivos[caso.motivoCierre]}</span>}
                  </dd>
                </div>
              )}
              {caso.borrarDatosEn && !caso.datosEliminados && (
                <div>
                  <dt>Eliminación de datos</dt>
                  <dd>{fecha(caso.borrarDatosEn)}</dd>
                </div>
              )}
            </dl>
          </section>

          <section className="tarjeta">
            <div className="tarjeta-encabezado">
              <h3>Denunciante</h3>
            </div>
            <div className="tarjeta-cuerpo" style={{ paddingTop: 18 }}>
              {caso.identidad ? (
                <dl className="datos-caso">
                  <div>
                    <dt>Nombre</dt>
                    <dd>{caso.identidad.nombre}</dd>
                  </div>
                  <div>
                    <dt>Correo</dt>
                    <dd style={{ wordBreak: "break-all" }}>{caso.identidad.correo}</dd>
                  </div>
                  {caso.identidad.telefono && (
                    <div>
                      <dt>Teléfono</dt>
                      <dd>{caso.identidad.telefono}</dd>
                    </div>
                  )}
                  <div>
                    <dt>Relación</dt>
                    <dd>{relaciones[caso.identidad.relacion]}</dd>
                  </div>
                </dl>
              ) : (
                <p className="texto-secundario" style={{ display: "flex", gap: 10, fontSize: 14 }}>
                  <ShieldAlert size={18} style={{ flex: "none" }} />
                  {caso.modo === "Anonimo"
                    ? "Denuncia anónima. Solo puede comunicarse por el buzón."
                    : "Los datos personales ya se eliminaron."}
                </p>
              )}
            </div>
          </section>

          <Asignados caso={caso} esOficial={esOficial} editable={editable} recargar={cargar} />

          <section className="tarjeta">
            <div className="tarjeta-encabezado">
              <h3>Bitácora</h3>
              <Lock size={15} className="texto-secundario" aria-label="No se puede modificar" />
            </div>
            <div className="tarjeta-cuerpo" style={{ paddingTop: 18 }}>
              <ol className="bitacora">
                {caso.bitacora.map((a, i) => (
                  <li key={i} data-actor={a.actor}>
                    <strong>{a.accion}</strong>
                    {a.detalle && <span className="detalle-accion">{formatearDetalle(a.detalle)}</span>}
                    <small>
                      {a.gestor ?? actores[a.actor]} · {fechaHora(a.fecha)}
                    </small>
                  </li>
                ))}
              </ol>
            </div>
          </section>
        </aside>
      </div>

      {modal === "estado" && <CambiarEstado caso={caso} onCerrar={() => setModal(null)} recargar={cargar} />}
      {modal === "asignar" && <Asignar caso={caso} onCerrar={() => setModal(null)} recargar={cargar} />}
    </>
  );
}

/** "EnRevision → Cerrada (NoFundada)" se muestra con los nombres en español. */
function formatearDetalle(detalle: string): string {
  return detalle.replace(/\b(Recibida|EnRevision|EnInvestigacion|Cerrada|Fundada|NoFundada|InformacionInsuficiente|FueraDeAlcance)\b/g, (valor) =>
    valor in estados ? estados[valor as EstadoDenuncia] : motivos[valor as MotivoCierre],
  );
}

function Relato({ caso }: { caso: CasoGestion }) {
  if (caso.datosEliminados) {
    return (
      <div className="vacio">
        <Lock />
        <strong>Relato eliminado</strong>
        <span>Se borró al vencer el plazo de conservación.</span>
      </div>
    );
  }

  const hechos = [
    ["Cuándo", caso.fechaHechos],
    ["Dónde", caso.lugar],
    ["Personas involucradas", caso.personasInvolucradas],
  ].filter(([, valor]) => valor);

  return (
    <div className="tarjeta-cuerpo">
      {hechos.length > 0 && (
        <dl className="datos-hechos">
          {hechos.map(([titulo, valor]) => (
            <div key={titulo}>
              <dt>{titulo}</dt>
              <dd>{valor}</dd>
            </div>
          ))}
        </dl>
      )}
      <p className="relato">{caso.relato}</p>
    </div>
  );
}

function Buzon({ caso, editable, recargar }: { caso: CasoGestion; editable: boolean; recargar: () => Promise<void> }) {
  const [texto, setTexto] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const final = useRef<HTMLDivElement>(null);

  useEffect(() => {
    final.current?.scrollIntoView({ block: "nearest" });
  }, [caso.mensajes.length]);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await api(`/api/gestion/casos/${caso.id}/mensajes`, "POST", { texto });
      setTexto("");
      await recargar();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      <div className="conversacion">
        {caso.mensajes.length === 0 ? (
          <div className="vacio" style={{ margin: "auto" }}>
            <MessagesSquare />
            <strong>Sin mensajes</strong>
            <span>Lo que escriba aquí lo verá el denunciante al consultar su caso.</span>
          </div>
        ) : (
          caso.mensajes.map((m, i) => (
            <div key={i} className={`burbuja ${m.autor === "Empresa" ? "burbuja-propia" : "burbuja-otra"}`}>
              {m.texto}
              <span className="burbuja-meta">
                {m.autor === "Empresa" ? m.gestor ?? "Empresa" : "Denunciante"} · {fechaHora(m.fecha)}
              </span>
            </div>
          ))
        )}
        <div ref={final} />
      </div>
      {editable && (
        <form className="redactor" onSubmit={enviar}>
          <label className="oculto-visual" htmlFor="respuesta">
            Respuesta al denunciante
          </label>
          <AreaTexto id="respuesta" valor={texto} onCambio={setTexto} maximo={5000} placeholder="Escriba una respuesta o una pregunta para el denunciante…" />
          {error && <Aviso tipo="error">{error}</Aviso>}
          <div className="redactor-pie">
            <span className="ayuda">El denunciante verá este mensaje como «Equipo de cumplimiento».</span>
            <button type="submit" className="boton boton-primario" disabled={enviando || !texto.trim()}>
              {enviando ? <Girando /> : <Send />} Enviar
            </button>
          </div>
        </form>
      )}
    </>
  );
}

function Anotaciones({ caso, editable, recargar }: { caso: CasoGestion; editable: boolean; recargar: () => Promise<void> }) {
  const [texto, setTexto] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function guardar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await api(`/api/gestion/casos/${caso.id}/anotaciones`, "POST", { texto });
      setTexto("");
      await recargar();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="tarjeta-cuerpo">
      {caso.anotaciones.length === 0 ? (
        <div className="vacio" style={{ paddingTop: 16 }}>
          <NotebookPen />
          <strong>Sin anotaciones</strong>
          <span>Las anotaciones son internas: el denunciante nunca las ve.</span>
        </div>
      ) : (
        caso.anotaciones.map((a, i) => (
          <article key={i} className="nota">
            <p>{a.texto}</p>
            <small>
              {a.gestor} · {fechaHora(a.fecha)}
            </small>
          </article>
        ))
      )}
      {editable && (
        <form onSubmit={guardar} style={{ marginTop: 20 }}>
          <div className="campo">
            <label className="etiqueta" htmlFor="anotacion">
              Nueva anotación interna
            </label>
            <AreaTexto id="anotacion" valor={texto} onCambio={setTexto} maximo={5000} rows={4} />
          </div>
          {error && (
            <div style={{ marginTop: 12 }}>
              <Aviso tipo="error">{error}</Aviso>
            </div>
          )}
          <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 12 }}>
            <button type="submit" className="boton boton-secundario" disabled={enviando || !texto.trim()}>
              {enviando ? <Girando /> : <Plus />} Agregar anotación
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

function Asignados({
  caso,
  esOficial,
  editable,
  recargar,
}: {
  caso: CasoGestion;
  esOficial: boolean;
  editable: boolean;
  recargar: () => Promise<void>;
}) {
  const [quitando, setQuitando] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function quitar(gestorId: number) {
    setQuitando(gestorId);
    setError(null);
    try {
      await api(`/api/gestion/casos/${caso.id}/asignaciones/${gestorId}`, "DELETE");
      await recargar();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setQuitando(null);
    }
  }

  return (
    <section className="tarjeta">
      <div className="tarjeta-encabezado">
        <h3>Asignado a</h3>
      </div>
      <div className="tarjeta-cuerpo" style={{ paddingTop: 18 }}>
        {caso.asignados.length === 0 ? (
          <p className="texto-secundario" style={{ fontSize: 14 }}>
            Sin asignar.{esOficial && editable && " Use «Asignar» para delegarlo a un gestor."}
          </p>
        ) : (
          <ul className="lista-lateral">
            {caso.asignados.map((a) => (
              <li key={a.gestorId}>
                <span className="avatar-mini">{iniciales(a.nombre)}</span>
                <div>
                  {a.nombre}
                  <small>Desde el {fecha(a.asignadaEn)}</small>
                </div>
                {esOficial && editable && (
                  <button
                    type="button"
                    className="boton boton-fantasma boton-icono"
                    onClick={() => quitar(a.gestorId)}
                    disabled={quitando !== null}
                    aria-label={`Quitar a ${a.nombre}`}
                    title="Quitar asignación"
                  >
                    {quitando === a.gestorId ? <Girando /> : <UserMinus />}
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
        {error && (
          <div style={{ marginTop: 12 }}>
            <Aviso tipo="error">{error}</Aviso>
          </div>
        )}
      </div>
    </section>
  );
}

function CambiarEstado({ caso, onCerrar, recargar }: { caso: CasoGestion; onCerrar: () => void; recargar: () => Promise<void> }) {
  const opciones = ordenEstados.filter((e) => e !== caso.estado);
  const [estado, setEstado] = useState<EstadoDenuncia>(opciones[0]);
  const [motivo, setMotivo] = useState<MotivoCierre | "">("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function guardar() {
    if (estado === "Cerrada" && !motivo) {
      setError("Seleccione el motivo de cierre.");
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      await api(`/api/gestion/casos/${caso.id}/estado`, "POST", {
        estado,
        motivoCierre: estado === "Cerrada" ? motivo : null,
      });
      await recargar();
      onCerrar();
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <Modal
      titulo="Cambiar estado"
      descripcion={`Estado actual: ${estados[caso.estado]}`}
      onCerrar={onCerrar}
      pie={
        <>
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>
            Cancelar
          </button>
          <button type="button" className="boton boton-primario" onClick={guardar} disabled={enviando}>
            {enviando && <Girando />} {estado === "Cerrada" ? "Cerrar caso" : "Guardar"}
          </button>
        </>
      }
    >
      <div className="opciones" role="radiogroup" aria-label="Nuevo estado">
        {opciones.map((e) => (
          <button type="button" key={e} role="radio" aria-checked={estado === e} className="opcion" onClick={() => setEstado(e)} style={{ padding: 12 }}>
            <ChipEstado estado={e} />
          </button>
        ))}
      </div>
      {estado === "Cerrada" && (
        <>
          <div className="campo" style={{ marginTop: 18 }}>
            <label className="etiqueta" htmlFor="motivo">
              Motivo de cierre
            </label>
            <select id="motivo" className="entrada" value={motivo} onChange={(e) => setMotivo(e.target.value as MotivoCierre)}>
              <option value="">Seleccione un motivo</option>
              {(Object.keys(motivos) as MotivoCierre[]).map((m) => (
                <option key={m} value={m}>
                  {motivos[m]}
                </option>
              ))}
            </select>
          </div>
          <div style={{ marginTop: 16 }}>
            <Aviso tipo="advertencia">
              Un caso cerrado no se puede reabrir. Al cerrarlo se programa la eliminación de sus datos según el plazo de
              conservación. El denunciante podrá seguir escribiendo en el buzón.
            </Aviso>
          </div>
        </>
      )}
      {error && (
        <div style={{ marginTop: 16 }}>
          <Aviso tipo="error">{error}</Aviso>
        </div>
      )}
    </Modal>
  );
}

function Asignar({ caso, onCerrar, recargar }: { caso: CasoGestion; onCerrar: () => void; recargar: () => Promise<void> }) {
  const [gestores, setGestores] = useState<GestorResumen[] | null>(null);
  const [elegido, setElegido] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    api<GestorResumen[]>("/api/gestion/gestores")
      .then((lista) => setGestores(lista.filter((g) => g.activo && !caso.asignados.some((a) => a.gestorId === g.id))))
      .catch((e) => setError(mensajeDeError(e)));
  }, [caso.asignados]);

  async function asignar() {
    if (elegido === null) {
      return;
    }

    setEnviando(true);
    setError(null);
    try {
      await api(`/api/gestion/casos/${caso.id}/asignaciones`, "POST", { gestorId: elegido });
      await recargar();
      onCerrar();
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <Modal
      titulo="Asignar caso"
      descripcion="El gestor asignado podrá ver el caso completo, responder y cambiar su estado."
      onCerrar={onCerrar}
      pie={
        <>
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>
            Cancelar
          </button>
          <button type="button" className="boton boton-primario" onClick={asignar} disabled={enviando || elegido === null}>
            {enviando && <Girando />} Asignar
          </button>
        </>
      }
    >
      {!gestores && !error && <Cargando />}
      {gestores && gestores.length === 0 && (
        <Aviso>No hay más gestores activos para asignar. Puede crearlos en la sección «Gestores».</Aviso>
      )}
      {gestores && gestores.length > 0 && (
        <div className="opciones" role="radiogroup" aria-label="Gestor">
          {gestores.map((g) => (
            <button type="button" key={g.id} role="radio" aria-checked={elegido === g.id} className="opcion" onClick={() => setElegido(g.id)} style={{ padding: 12, alignItems: "center" }}>
              <span className="opcion-icono">
                <UserRound />
              </span>
              <span>
                <strong>{g.nombre}</strong>
                <small>{g.correo}</small>
              </span>
            </button>
          ))}
        </div>
      )}
      {error && (
        <div style={{ marginTop: 16 }}>
          <Aviso tipo="error">{error}</Aviso>
        </div>
      )}
    </Modal>
  );
}
