import { useEffect, useMemo, useState } from "react";
import { Inbox, RefreshCw, Search } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router";
import { api, mensajeDeError } from "../api/cliente";
import { categorias, diasHasta, estados, fechaCorta, iniciales, modos, ordenEstados } from "../api/textos";
import type { CasoResumen, EstadoDenuncia } from "../api/tipos";
import { Aviso, Cargando, ChipEstado, Plazo } from "../componentes/comunes";
import { useSesion } from "./sesion";

const coloresEstado: Record<EstadoDenuncia, string> = {
  Recibida: "var(--plata)",
  EnRevision: "var(--celeste)",
  EnInvestigacion: "var(--azul)",
  Cerrada: "var(--menta)",
};

const plurales: Record<EstadoDenuncia, string> = {
  Recibida: "Recibidas",
  EnRevision: "En revisión",
  EnInvestigacion: "En investigación",
  Cerrada: "Cerradas",
};

type Filtro = EstadoDenuncia | "Abiertos" | "Vencidos" | "Todos";

export default function Casos() {
  const { sesion } = useSesion();
  const navegar = useNavigate();
  const [parametros, setParametros] = useSearchParams();
  const [casos, setCasos] = useState<CasoResumen[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actualizando, setActualizando] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const [empresa, setEmpresa] = useState("");
  const filtro = (parametros.get("filtro") as Filtro | null) ?? "Abiertos";

  async function cargar() {
    setActualizando(true);
    try {
      setCasos(await api<CasoResumen[]>("/api/gestion/casos"));
      setError(null);
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setActualizando(false);
    }
  }

  useEffect(() => {
    void cargar();
  }, []);

  const vencido = (c: CasoResumen) => c.estado !== "Cerrada" && !!c.venceRespuestaEn && diasHasta(c.venceRespuestaEn) < 0;

  const conteos = useMemo(() => {
    const lista = casos ?? [];
    return {
      ...Object.fromEntries(ordenEstados.map((e) => [e, lista.filter((c) => c.estado === e).length])),
      Vencidos: lista.filter(vencido).length,
    } as Record<EstadoDenuncia | "Vencidos", number>;
  }, [casos]);

  const empresas = useMemo(() => [...new Set((casos ?? []).map((c) => c.empresa))].sort(), [casos]);

  const visibles = useMemo(() => {
    const texto = busqueda.trim().toUpperCase();
    return (casos ?? []).filter(
      (c) =>
        (filtro === "Todos" ||
          (filtro === "Abiertos" && c.estado !== "Cerrada") ||
          (filtro === "Vencidos" && vencido(c)) ||
          c.estado === filtro) &&
        (!empresa || c.empresa === empresa) &&
        (!texto || c.codigo.includes(texto) || categorias[c.categoria].nombre.toUpperCase().includes(texto)),
    );
  }, [casos, filtro, busqueda, empresa]);

  const cambiarFiltro = (nuevo: Filtro) => setParametros(nuevo === "Abiertos" ? {} : { filtro: nuevo }, { replace: true });

  return (
    <>
      <div className="titulo-panel">
        <div>
          <h1>Casos</h1>
          <p>
            {sesion?.rol === "OficialCumplimiento"
              ? "Todos los casos del canal."
              : "Los casos que el oficial de cumplimiento le asignó."}
          </p>
        </div>
        <button type="button" className="boton boton-secundario" onClick={cargar} disabled={actualizando}>
          <RefreshCw className={actualizando ? "girando" : undefined} /> Actualizar
        </button>
      </div>

      {error && <Aviso tipo="error">{error}</Aviso>}

      {!casos && !error && <Cargando texto="Cargando casos…" />}

      {casos && (
        <>
          <div className="metricas">
            {(["Recibida", "EnRevision", "EnInvestigacion"] as EstadoDenuncia[]).map((e) => (
              <button
                type="button"
                key={e}
                className="metrica"
                aria-pressed={filtro === e}
                onClick={() => cambiarFiltro(filtro === e ? "Abiertos" : e)}
              >
                <span>
                  <i style={{ background: coloresEstado[e] }} /> {plurales[e]}
                </span>
                <b>{conteos[e]}</b>
              </button>
            ))}
            <button
              type="button"
              className="metrica"
              aria-pressed={filtro === "Vencidos"}
              onClick={() => cambiarFiltro(filtro === "Vencidos" ? "Abiertos" : "Vencidos")}
            >
              <span>
                <i style={{ background: "var(--alerta)" }} /> Plazo vencido
              </span>
              <b style={{ color: conteos.Vencidos > 0 ? "var(--alerta)" : undefined }}>{conteos.Vencidos}</b>
            </button>
          </div>

          <div className="filtros">
            <div className="buscador">
              <Search aria-hidden="true" />
              <input
                className="entrada"
                placeholder="Buscar por código o categoría"
                aria-label="Buscar casos"
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
              />
            </div>
            <select className="entrada" aria-label="Estado" value={filtro} onChange={(e) => cambiarFiltro(e.target.value as Filtro)}>
              <option value="Abiertos">Abiertos</option>
              <option value="Todos">Todos los estados</option>
              {ordenEstados.map((e) => (
                <option key={e} value={e}>
                  {estados[e]}
                </option>
              ))}
              <option value="Vencidos">Plazo vencido</option>
            </select>
            {empresas.length > 1 && (
              <select className="entrada" aria-label="Empresa" value={empresa} onChange={(e) => setEmpresa(e.target.value)}>
                <option value="">Todas las empresas</option>
                {empresas.map((e) => (
                  <option key={e}>{e}</option>
                ))}
              </select>
            )}
          </div>

          <div className="tabla-contenedor">
            {visibles.length === 0 ? (
              <div className="vacio">
                <Inbox />
                <strong>{casos.length === 0 ? "Aún no hay casos" : "Ningún caso coincide con el filtro"}</strong>
                <span>
                  {casos.length === 0
                    ? sesion?.rol === "OficialCumplimiento"
                      ? "Las denuncias nuevas aparecerán aquí."
                      : "Cuando el oficial le asigne un caso, aparecerá aquí."
                    : "Pruebe con otro estado o borre la búsqueda."}
                </span>
              </div>
            ) : (
              <table className="tabla tabla-casos">
                <thead>
                  <tr>
                    <th>Código</th>
                    <th>Categoría</th>
                    <th>Estado</th>
                    <th>Plazo</th>
                    <th className="ocultar-movil">Recibida</th>
                    <th className="ocultar-movil">Asignado a</th>
                  </tr>
                </thead>
                <tbody>
                  {visibles.map((c) => (
                    <tr
                      key={c.id}
                      className="fila-enlace"
                      tabIndex={0}
                      onClick={() => navegar(`/gestion/casos/${c.id}`)}
                      onKeyDown={(e) => e.key === "Enter" && navegar(`/gestion/casos/${c.id}`)}
                    >
                      <td>
                        <span className="codigo">{c.codigo}</span>
                        <span className="celda-secundaria">{c.empresa}</span>
                      </td>
                      <td>
                        {categorias[c.categoria].nombre}
                        <span className="celda-secundaria">{modos[c.modo]}</span>
                      </td>
                      <td>
                        <ChipEstado estado={c.estado} />
                      </td>
                      <td>
                        <Plazo vence={c.venceRespuestaEn} cerrada={c.estado === "Cerrada"} />
                      </td>
                      <td className="ocultar-movil">{fechaCorta(c.recibidaEn)}</td>
                      <td className="ocultar-movil">
                        {c.asignados.length === 0 ? (
                          <span className="texto-secundario">Sin asignar</span>
                        ) : (
                          <span className="asignados" title={c.asignados.join(", ")}>
                            {c.asignados.slice(0, 3).map((nombre) => (
                              <span key={nombre} className="avatar-mini">
                                {iniciales(nombre)}
                              </span>
                            ))}
                            {c.asignados.length === 1 && <span style={{ marginLeft: 8 }}>{c.asignados[0]}</span>}
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </>
  );
}
