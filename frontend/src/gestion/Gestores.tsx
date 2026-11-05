import { useEffect, useState, type FormEvent } from "react";
import { KeyRound, Smartphone, UserPlus, Users } from "lucide-react";
import { api, mensajeDeError } from "../api/cliente";
import { iniciales, roles } from "../api/textos";
import type { ClaveTemporal, GestorResumen, RolGestor } from "../api/tipos";
import { Aviso, Cargando, Credencial, Girando, Modal } from "../componentes/comunes";
import { useSesion } from "./sesion";

type Accion =
  | { tipo: "crear" }
  | { tipo: "clave"; gestor: GestorResumen }
  | { tipo: "totp"; gestor: GestorResumen }
  | { tipo: "temporal"; nombre: string; correo: string; clave: string };

export default function Gestores() {
  const { sesion } = useSesion();
  const [gestores, setGestores] = useState<GestorResumen[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [accion, setAccion] = useState<Accion | null>(null);
  const [exito, setExito] = useState<string | null>(null);

  async function cargar() {
    try {
      setGestores(await api<GestorResumen[]>("/api/gestion/gestores"));
    } catch (e) {
      setError(mensajeDeError(e));
    }
  }

  useEffect(() => {
    void cargar();
  }, []);

  function estadoCuenta(g: GestorResumen) {
    if (!g.activo) return <span className="chip chip-neutro">Inactivo</span>;
    if (g.bloqueadoHasta && new Date(g.bloqueadoHasta) > new Date()) return <span className="chip chip-alerta">Bloqueado</span>;
    if (g.debeCambiarClave || !g.totpConfirmado) return <span className="chip chip-ambar">Pendiente de activar</span>;
    return <span className="chip chip-cerrada">Activo</span>;
  }

  return (
    <>
      <div className="titulo-panel">
        <div>
          <h1>Gestores</h1>
          <p>Personas con acceso al panel. Solo usted, como oficial de cumplimiento, las administra.</p>
        </div>
        <button type="button" className="boton boton-primario" onClick={() => setAccion({ tipo: "crear" })}>
          <UserPlus /> Nuevo gestor
        </button>
      </div>

      {error && <Aviso tipo="error">{error}</Aviso>}
      {exito && (
        <div style={{ marginBottom: 16 }}>
          <Aviso tipo="exito">{exito}</Aviso>
        </div>
      )}
      {!gestores && !error && <Cargando />}

      {gestores && (
        <div className="tabla-contenedor">
          {gestores.length === 0 ? (
            <div className="vacio">
              <Users />
              <strong>Sin gestores</strong>
            </div>
          ) : (
            <table className="tabla">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th className="ocultar-movil">Rol</th>
                  <th>Estado</th>
                  <th style={{ textAlign: "right" }}>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {gestores.map((g) => {
                  const esUsted = g.correo === sesion?.correo;
                  return (
                    <tr key={g.id}>
                      <td>
                        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                          <span className="asignados">
                            <span className="avatar-mini">{iniciales(g.nombre)}</span>
                          </span>
                          <div>
                            {g.nombre} {esUsted && <span className="texto-secundario">(usted)</span>}
                            <span className="celda-secundaria">{g.correo}</span>
                          </div>
                        </div>
                      </td>
                      <td className="ocultar-movil">{roles[g.rol]}</td>
                      <td>{estadoCuenta(g)}</td>
                      <td>
                        {!esUsted && (
                          <div className="acciones">
                            <button
                              type="button"
                              className="boton boton-secundario boton-pequeno"
                              onClick={() => (setExito(null), setAccion({ tipo: "clave", gestor: g }))}
                              title="Generar una contraseña temporal"
                            >
                              <KeyRound /> <span className="ocultar-movil">Contraseña</span>
                            </button>
                            <button
                              type="button"
                              className="boton boton-secundario boton-pequeno"
                              onClick={() => (setExito(null), setAccion({ tipo: "totp", gestor: g }))}
                              title="Restablecer el segundo factor"
                              disabled={!g.totpConfirmado}
                            >
                              <Smartphone /> <span className="ocultar-movil">2.º factor</span>
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      )}

      {accion?.tipo === "crear" && (
        <NuevoGestor
          onCerrar={() => setAccion(null)}
          alCrear={(nombre, correo, clave) => {
            setAccion({ tipo: "temporal", nombre, correo, clave });
            void cargar();
          }}
        />
      )}

      {accion?.tipo === "clave" && (
        <Confirmar
          titulo="Restablecer contraseña"
          texto={`Se generará una contraseña temporal para ${accion.gestor.nombre} y se cerrarán sus sesiones abiertas. Su segundo factor se conserva.`}
          boton="Generar contraseña"
          onCerrar={() => setAccion(null)}
          ejecutar={async () => {
            const temporal = await api<ClaveTemporal>(`/api/gestion/gestores/${accion.gestor.id}/restablecer-clave`, "POST");
            setAccion({ tipo: "temporal", nombre: accion.gestor.nombre, correo: accion.gestor.correo, clave: temporal.clave });
            void cargar();
          }}
        />
      )}

      {accion?.tipo === "totp" && (
        <Confirmar
          titulo="Restablecer segundo factor"
          texto={`Úselo si ${accion.gestor.nombre} perdió o cambió el celular. Se cerrarán sus sesiones y, en su próximo ingreso, deberá vincular de nuevo la aplicación de autenticación.`}
          boton="Restablecer"
          onCerrar={() => setAccion(null)}
          ejecutar={async () => {
            await api(`/api/gestion/gestores/${accion.gestor.id}/restablecer-totp`, "POST");
            setAccion(null);
            setExito(`Se restableció el segundo factor de ${accion.gestor.nombre}.`);
            void cargar();
          }}
        />
      )}

      {accion?.tipo === "temporal" && (
        <Modal
          titulo="Contraseña temporal"
          descripcion={`Entréguela a ${accion.nombre} por un canal seguro. No se volverá a mostrar.`}
          onCerrar={() => setAccion(null)}
          pie={
            <button type="button" className="boton boton-primario" onClick={() => setAccion(null)}>
              Listo
            </button>
          }
        >
          <dl className="datos-caso" style={{ marginBottom: 16 }}>
            <div>
              <dt>Usuario</dt>
              <dd>{accion.correo}</dd>
            </div>
          </dl>
          <Credencial titulo="Contraseña temporal" valor={accion.clave} />
          <p className="ayuda" style={{ marginTop: 12 }}>
            Al ingresar deberá cambiarla y vincular su aplicación de autenticación.
          </p>
        </Modal>
      )}
    </>
  );
}

function NuevoGestor({ onCerrar, alCrear }: { onCerrar: () => void; alCrear: (nombre: string, correo: string, clave: string) => void }) {
  const [nombre, setNombre] = useState("");
  const [correo, setCorreo] = useState("");
  const [rol, setRol] = useState<RolGestor>("Gestor");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function crear(e?: FormEvent) {
    e?.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      const temporal = await api<ClaveTemporal>("/api/gestion/gestores", "POST", { nombre, correo, rol });
      alCrear(nombre.trim(), correo.trim().toLowerCase(), temporal.clave);
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <Modal
      titulo="Nuevo gestor"
      descripcion="Recibirá una contraseña temporal para su primer ingreso."
      onCerrar={onCerrar}
      pie={
        <>
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>
            Cancelar
          </button>
          <button type="button" className="boton boton-primario" onClick={() => crear()} disabled={enviando}>
            {enviando && <Girando />} Crear gestor
          </button>
        </>
      }
    >
      <form onSubmit={crear} noValidate>
        <div className="campo">
          <label className="etiqueta" htmlFor="nombre-gestor">
            Nombre completo
          </label>
          <input id="nombre-gestor" className="entrada" maxLength={200} value={nombre} onChange={(e) => setNombre(e.target.value)} autoFocus />
        </div>
        <div className="campo">
          <label className="etiqueta" htmlFor="correo-gestor">
            Correo corporativo
          </label>
          <input id="correo-gestor" type="email" className="entrada" maxLength={240} value={correo} onChange={(e) => setCorreo(e.target.value)} />
        </div>
        <div className="campo">
          <label className="etiqueta" htmlFor="rol-gestor">
            Rol
          </label>
          <select id="rol-gestor" className="entrada" value={rol} onChange={(e) => setRol(e.target.value as RolGestor)}>
            <option value="Gestor">Gestor · ve solo los casos que se le asignan</option>
            <option value="OficialCumplimiento">Oficial de cumplimiento · ve y asigna todos los casos</option>
          </select>
        </div>
        {error && (
          <div style={{ marginTop: 16 }}>
            <Aviso tipo="error">{error}</Aviso>
          </div>
        )}
        <button type="submit" hidden />
      </form>
    </Modal>
  );
}

function Confirmar({
  titulo,
  texto,
  boton,
  onCerrar,
  ejecutar,
}: {
  titulo: string;
  texto: string;
  boton: string;
  onCerrar: () => void;
  ejecutar: () => Promise<void>;
}) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function confirmar() {
    setEnviando(true);
    setError(null);
    try {
      await ejecutar();
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <Modal
      titulo={titulo}
      descripcion={texto}
      onCerrar={onCerrar}
      pie={
        <>
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>
            Cancelar
          </button>
          <button type="button" className="boton boton-primario" onClick={confirmar} disabled={enviando}>
            {enviando && <Girando />} {boton}
          </button>
        </>
      }
    >
      {error && <Aviso tipo="error">{error}</Aviso>}
    </Modal>
  );
}
