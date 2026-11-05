import { useEffect, useMemo, useState, type FormEvent } from "react";
import { ArrowLeft, ArrowRight, Check, Download, EyeOff, Send, UserRound } from "lucide-react";
import { Link } from "react-router";
import { api, mensajeDeError } from "../api/cliente";
import { categorias, fecha, modos, relaciones } from "../api/textos";
import type {
  CategoriaConducta,
  DenunciaRecibida,
  Formulario,
  ModoDenuncia,
  NuevaDenuncia,
  RelacionDenunciante,
  TipoTextoLegal,
} from "../api/tipos";
import { AreaTexto, Aviso, Cargando, Credencial, Girando } from "../componentes/comunes";
import { iconosCategoria } from "../componentes/iconos";

// Mismos límites que valida la API.
const limites = { relato: 20_000, fechaHechos: 40, lugar: 240, personas: 2_000, nombre: 200, correo: 240, telefono: 30 };

const pasos = [
  { titulo: "Antes de empezar", detalle: "Privacidad y garantías" },
  { titulo: "Sobre la denuncia", detalle: "Empresa, tipo y modo" },
  { titulo: "Los hechos", detalle: "Qué ocurrió" },
  { titulo: "Revisar y enviar", detalle: "Confirme los datos" },
];

interface Datos {
  acepta: boolean;
  empresaId: number | null;
  categoria: CategoriaConducta | null;
  modo: ModoDenuncia | null;
  nombre: string;
  correo: string;
  telefono: string;
  relacion: RelacionDenunciante;
  relato: string;
  fechaHechos: string;
  lugar: string;
  personas: string;
}

const inicial: Datos = {
  acepta: false,
  empresaId: null,
  categoria: null,
  modo: null,
  nombre: "",
  correo: "",
  telefono: "",
  relacion: "Empleado",
  relato: "",
  fechaHechos: "",
  lugar: "",
  personas: "",
};

const correoValido = (correo: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo.trim());
const vacioANulo = (texto: string) => texto.trim() || null;

export default function Denunciar() {
  const [formulario, setFormulario] = useState<Formulario | null>(null);
  const [errorCarga, setErrorCarga] = useState<string | null>(null);
  const [paso, setPaso] = useState(0);
  const [datos, setDatos] = useState<Datos>(inicial);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [acuse, setAcuse] = useState<DenunciaRecibida | null>(null);

  useEffect(() => {
    api<Formulario>("/api/publico/formulario")
      .then((f) => {
        setFormulario(f);
        if (f.empresas.length === 1) {
          setDatos((d) => ({ ...d, empresaId: f.empresas[0].id }));
        }
      })
      .catch((e) => setErrorCarga(mensajeDeError(e)));
  }, []);

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [paso]);

  const texto = useMemo(
    () => (tipo: TipoTextoLegal) => formulario?.textos.find((t) => t.tipo === tipo),
    [formulario],
  );

  const cambiar = <K extends keyof Datos>(campo: K, valor: Datos[K]) => setDatos((d) => ({ ...d, [campo]: valor }));

  function validar(): string | null {
    if (paso === 0 && !datos.acepta) {
      return "Para continuar debe autorizar el tratamiento de sus datos.";
    }

    if (paso === 1) {
      if (!datos.empresaId) return "Seleccione la empresa en la que ocurrieron los hechos.";
      if (!datos.categoria) return "Seleccione el tipo de conducta.";
      if (!datos.modo) return "Elija si quiere denunciar de forma anónima o a nombre propio.";
      if (datos.modo === "ANombrePropio") {
        if (!datos.nombre.trim()) return "Escriba su nombre.";
        if (!correoValido(datos.correo)) return "Escriba un correo válido para poder contactarle.";
        if (datos.telefono && !/^[\d\s+\-()]+$/.test(datos.telefono)) {
          return "El teléfono solo admite números, espacios y los signos + - ( ).";
        }
      }
    }

    if (paso === 2 && !datos.relato.trim()) {
      return "Cuéntenos qué pasó.";
    }

    return null;
  }

  function siguiente(e: FormEvent) {
    e.preventDefault();
    const problema = validar();
    setError(problema);
    if (!problema) {
      setPaso((p) => p + 1);
    }
  }

  async function enviar() {
    const aviso = texto("AvisoPrivacidad");
    const autorizacion = texto("Autorizacion");
    if (!aviso || !autorizacion || !datos.empresaId || !datos.categoria || !datos.modo) {
      return;
    }

    const cuerpo: NuevaDenuncia = {
      empresaId: datos.empresaId,
      modo: datos.modo,
      categoria: datos.categoria,
      relato: datos.relato.trim(),
      fechaHechos: vacioANulo(datos.fechaHechos),
      lugar: vacioANulo(datos.lugar),
      personasInvolucradas: vacioANulo(datos.personas),
      identidad:
        datos.modo === "ANombrePropio"
          ? {
              nombre: datos.nombre.trim(),
              correo: datos.correo.trim(),
              telefono: vacioANulo(datos.telefono),
              relacion: datos.relacion,
            }
          : null,
      textoAvisoId: aviso.id,
      textoAutorizacionId: autorizacion.id,
      aceptaTratamiento: datos.acepta,
    };

    setEnviando(true);
    setError(null);
    try {
      setAcuse(await api<DenunciaRecibida>("/api/publico/denuncias", "POST", cuerpo));
      setDatos(inicial);
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  if (acuse) {
    return <Acuse acuse={acuse} />;
  }

  if (errorCarga) {
    return (
      <div className="contenedor contenedor-estrecho" style={{ padding: "64px 24px" }}>
        <Aviso tipo="error">{errorCarga}</Aviso>
      </div>
    );
  }

  if (!formulario) {
    return <Cargando texto="Preparando el formulario…" />;
  }

  const empresa = formulario.empresas.find((e) => e.id === datos.empresaId);

  return (
    <div className="contenedor">
      <div className="cabecera-pagina">
        <Link to="/" className="migas">
          <ArrowLeft /> Inicio
        </Link>
        <h1>Hacer una denuncia</h1>
        <p>Toda la información viaja y se guarda cifrada. Puede detenerse en cualquier momento antes de enviar.</p>
      </div>

      <div className="asistente">
        <ol className="pasos-asistente" aria-label="Pasos">
          {pasos.map((p, i) => (
            <li
              key={p.titulo}
              className="paso-asistente"
              data-estado={i < paso ? "hecho" : i === paso ? "actual" : "pendiente"}
              aria-current={i === paso ? "step" : undefined}
            >
              <span className="numero">{i < paso ? <Check strokeWidth={3} /> : i + 1}</span>
              <span>
                {p.titulo}
                <small>{p.detalle}</small>
              </span>
            </li>
          ))}
        </ol>

        <form className="tarjeta tarjeta-cuerpo" onSubmit={siguiente} noValidate>
          <div className="progreso-movil" aria-hidden="true">
            Paso {paso + 1} de {pasos.length} · {pasos[paso].titulo}
            <div className="barra-progreso">
              <span style={{ width: `${((paso + 1) / pasos.length) * 100}%` }} />
            </div>
          </div>

          {paso === 0 && (
            <>
              <div className="encabezado-paso">
                <span>Paso 1 de 4</span>
                <h2>Antes de empezar</h2>
                <p>Lea cómo protegemos su información y sus derechos.</p>
              </div>

              <div className="bloque">
                <h3 className="bloque-titulo">Política de no represalias</h3>
                <div className="texto-legal">{texto("PoliticaNoRepresalias")?.contenido}</div>
              </div>

              <div className="bloque">
                <h3 className="bloque-titulo">Aviso de privacidad</h3>
                <div className="texto-legal">{texto("AvisoPrivacidad")?.contenido}</div>
              </div>

              <div className="bloque">
                <h3 className="bloque-titulo">Autorización para el tratamiento de datos</h3>
                <div className="texto-legal">{texto("Autorizacion")?.contenido}</div>
                <label className="casilla" style={{ marginTop: 16 }}>
                  <input type="checkbox" checked={datos.acepta} onChange={(e) => cambiar("acepta", e.target.checked)} />
                  <span>He leído el aviso de privacidad y autorizo el tratamiento de mis datos para gestionar esta denuncia.</span>
                </label>
              </div>
            </>
          )}

          {paso === 1 && (
            <>
              <div className="encabezado-paso">
                <span>Paso 2 de 4</span>
                <h2>Sobre la denuncia</h2>
                <p>Estos datos nos ayudan a dirigir el caso a las personas correctas.</p>
              </div>

              {formulario.empresas.length > 1 && (
                <div className="campo">
                  <label className="etiqueta" htmlFor="empresa">
                    ¿En qué empresa ocurrieron los hechos?
                  </label>
                  <select
                    id="empresa"
                    className="entrada"
                    value={datos.empresaId ?? ""}
                    onChange={(e) => cambiar("empresaId", Number(e.target.value) || null)}
                  >
                    <option value="">Seleccione una empresa</option>
                    {formulario.empresas.map((e) => (
                      <option key={e.id} value={e.id}>
                        {e.nombre}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="bloque">
                <h3 className="bloque-titulo" id="titulo-categoria">
                  ¿Qué tipo de conducta quiere reportar?
                </h3>
                <div className="opciones opciones-3" role="radiogroup" aria-labelledby="titulo-categoria">
                  {(Object.keys(categorias) as CategoriaConducta[]).map((clave) => {
                    const Icono = iconosCategoria[clave];
                    return (
                      <button
                        type="button"
                        key={clave}
                        role="radio"
                        aria-checked={datos.categoria === clave}
                        className="opcion"
                        onClick={() => cambiar("categoria", clave)}
                      >
                        <span className="opcion-icono">
                          <Icono />
                        </span>
                        <span>
                          <strong>{categorias[clave].nombre}</strong>
                        </span>
                      </button>
                    );
                  })}
                </div>
                {datos.categoria && <p className="ayuda" style={{ marginTop: 10 }}>{categorias[datos.categoria].descripcion}</p>}
              </div>

              <div className="bloque">
                <h3 className="bloque-titulo" id="titulo-modo">
                  ¿Cómo quiere denunciar?
                </h3>
                <div className="opciones opciones-2" role="radiogroup" aria-labelledby="titulo-modo">
                  <button
                    type="button"
                    role="radio"
                    aria-checked={datos.modo === "Anonimo"}
                    className="opcion"
                    onClick={() => cambiar("modo", "Anonimo")}
                  >
                    <span className="opcion-icono">
                      <EyeOff />
                    </span>
                    <span>
                      <strong>De forma anónima</strong>
                      <small>No le pediremos ningún dato. Seguirá el caso solo con su código y contraseña.</small>
                    </span>
                  </button>
                  <button
                    type="button"
                    role="radio"
                    aria-checked={datos.modo === "ANombrePropio"}
                    className="opcion"
                    onClick={() => cambiar("modo", "ANombrePropio")}
                  >
                    <span className="opcion-icono">
                      <UserRound />
                    </span>
                    <span>
                      <strong>A nombre propio</strong>
                      <small>Sus datos se guardan cifrados y solo los ve el equipo de cumplimiento.</small>
                    </span>
                  </button>
                </div>
              </div>

              {datos.modo === "ANombrePropio" && (
                <div className="bloque">
                  <h3 className="bloque-titulo">Sus datos de contacto</h3>
                  <div className="fila-campos">
                    <div className="campo">
                      <label className="etiqueta" htmlFor="nombre">
                        Nombre completo
                      </label>
                      <input
                        id="nombre"
                        className="entrada"
                        autoComplete="name"
                        maxLength={limites.nombre}
                        value={datos.nombre}
                        onChange={(e) => cambiar("nombre", e.target.value)}
                      />
                    </div>
                    <div className="campo">
                      <label className="etiqueta" htmlFor="relacion">
                        Su relación con la empresa
                      </label>
                      <select
                        id="relacion"
                        className="entrada"
                        value={datos.relacion}
                        onChange={(e) => cambiar("relacion", e.target.value as RelacionDenunciante)}
                      >
                        {(Object.keys(relaciones) as RelacionDenunciante[]).map((r) => (
                          <option key={r} value={r}>
                            {relaciones[r]}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                  <div className="fila-campos">
                    <div className="campo">
                      <label className="etiqueta" htmlFor="correo">
                        Correo electrónico
                      </label>
                      <input
                        id="correo"
                        type="email"
                        className="entrada"
                        autoComplete="email"
                        maxLength={limites.correo}
                        value={datos.correo}
                        onChange={(e) => cambiar("correo", e.target.value)}
                      />
                    </div>
                    <div className="campo">
                      <label className="etiqueta" htmlFor="telefono">
                        Teléfono <span className="opcional">Opcional</span>
                      </label>
                      <input
                        id="telefono"
                        type="tel"
                        className="entrada"
                        autoComplete="tel"
                        maxLength={limites.telefono}
                        value={datos.telefono}
                        onChange={(e) => cambiar("telefono", e.target.value)}
                      />
                    </div>
                  </div>
                </div>
              )}
            </>
          )}

          {paso === 2 && (
            <>
              <div className="encabezado-paso">
                <span>Paso 3 de 4</span>
                <h2>Los hechos</h2>
                <p>Escriba con sus palabras. Mientras más detalle, mejor podremos investigar.</p>
              </div>

              <div className="campo">
                <label className="etiqueta" htmlFor="relato">
                  ¿Qué pasó?
                </label>
                <span className="ayuda" id="ayuda-relato">
                  Qué ocurrió, quiénes participaron, cómo se enteró y si hay pruebas o testigos.
                  {datos.modo === "Anonimo" && " Evite incluir datos que le identifiquen."}
                </span>
                <AreaTexto
                  id="relato"
                  valor={datos.relato}
                  onCambio={(v) => cambiar("relato", v)}
                  maximo={limites.relato}
                  rows={9}
                  aria-describedby="ayuda-relato"
                />
              </div>

              <div className="fila-campos">
                <div className="campo">
                  <label className="etiqueta" htmlFor="fecha">
                    ¿Cuándo ocurrió? <span className="opcional">Opcional</span>
                  </label>
                  <input
                    id="fecha"
                    className="entrada"
                    placeholder="Ej.: marzo de 2026, la semana pasada"
                    maxLength={limites.fechaHechos}
                    value={datos.fechaHechos}
                    onChange={(e) => cambiar("fechaHechos", e.target.value)}
                  />
                </div>
                <div className="campo">
                  <label className="etiqueta" htmlFor="lugar">
                    ¿Dónde ocurrió? <span className="opcional">Opcional</span>
                  </label>
                  <input
                    id="lugar"
                    className="entrada"
                    placeholder="Sede, área, ciudad o proyecto"
                    maxLength={limites.lugar}
                    value={datos.lugar}
                    onChange={(e) => cambiar("lugar", e.target.value)}
                  />
                </div>
              </div>

              <div className="campo">
                <label className="etiqueta" htmlFor="personas">
                  Personas involucradas <span className="opcional">Opcional</span>
                </label>
                <AreaTexto
                  id="personas"
                  valor={datos.personas}
                  onCambio={(v) => cambiar("personas", v)}
                  maximo={limites.personas}
                  rows={3}
                  placeholder="Nombres, cargos o cualquier dato que ayude a identificarlas"
                />
              </div>
            </>
          )}

          {paso === 3 && (
            <>
              <div className="encabezado-paso">
                <span>Paso 4 de 4</span>
                <h2>Revisar y enviar</h2>
                <p>Revise que todo esté bien. Después de enviar no podrá editar el relato, pero sí agregar información.</p>
              </div>

              <dl className="resumen">
                <div>
                  <dt>Empresa</dt>
                  <dd>{empresa?.nombre}</dd>
                </div>
                <div>
                  <dt>Tipo de conducta</dt>
                  <dd>{datos.categoria && categorias[datos.categoria].nombre}</dd>
                </div>
                <div>
                  <dt>Modo</dt>
                  <dd>{datos.modo && modos[datos.modo]}</dd>
                </div>
                {datos.modo === "ANombrePropio" && (
                  <div>
                    <dt>Sus datos</dt>
                    <dd>
                      {datos.nombre} · {datos.correo}
                      {datos.telefono && ` · ${datos.telefono}`} · {relaciones[datos.relacion]}
                    </dd>
                  </div>
                )}
                <div>
                  <dt>Qué pasó</dt>
                  <dd>{datos.relato}</dd>
                </div>
                {datos.fechaHechos && (
                  <div>
                    <dt>Cuándo</dt>
                    <dd>{datos.fechaHechos}</dd>
                  </div>
                )}
                {datos.lugar && (
                  <div>
                    <dt>Dónde</dt>
                    <dd>{datos.lugar}</dd>
                  </div>
                )}
                {datos.personas && (
                  <div>
                    <dt>Personas involucradas</dt>
                    <dd>{datos.personas}</dd>
                  </div>
                )}
              </dl>
            </>
          )}

          {error && (
            <div style={{ marginTop: 20 }}>
              <Aviso tipo="error">{error}</Aviso>
            </div>
          )}

          <div className="acciones-formulario">
            {paso > 0 ? (
              <button type="button" className="boton boton-secundario" onClick={() => (setError(null), setPaso((p) => p - 1))}>
                <ArrowLeft /> Atrás
              </button>
            ) : (
              <span />
            )}
            {paso < 3 ? (
              <button type="submit" className="boton boton-primario">
                Continuar <ArrowRight />
              </button>
            ) : (
              <button type="button" className="boton boton-primario" onClick={enviar} disabled={enviando}>
                {enviando ? <Girando /> : <Send />} Enviar denuncia
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}

function Acuse({ acuse }: { acuse: DenunciaRecibida }) {
  // Advierte antes de salir: el código y la contraseña no se pueden recuperar.
  useEffect(() => {
    const alSalir = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener("beforeunload", alSalir);
    return () => window.removeEventListener("beforeunload", alSalir);
  }, []);

  function descargar() {
    const contenido = [
      "LÍNEA ÉTICA · GRUPO LONDOÑO GÓMEZ",
      "Comprobante de denuncia",
      "",
      `Código del caso: ${acuse.codigo}`,
      `Contraseña:      ${acuse.clave}`,
      `Respuesta a más tardar: ${fecha(acuse.venceRespuestaEn)}`,
      "",
      "Guarde este archivo en un lugar seguro. Con estos datos puede consultar",
      "el estado de su caso y conversar con el equipo de cumplimiento.",
      "No se pueden recuperar: la empresa no los conoce.",
    ].join("\r\n");
    const enlace = document.createElement("a");
    enlace.href = URL.createObjectURL(new Blob([contenido], { type: "text/plain;charset=utf-8" }));
    enlace.download = `denuncia-${acuse.codigo}.txt`;
    enlace.click();
    URL.revokeObjectURL(enlace.href);
  }

  return (
    <div className="contenedor contenedor-estrecho acuse">
      <div className="acuse-sello">
        <Check strokeWidth={3} />
      </div>
      <h1>Recibimos su denuncia</h1>
      <p className="texto-secundario" style={{ marginTop: 12, fontSize: 16, whiteSpace: "pre-line" }}>
        {acuse.textoAcuse}
      </p>
      <p style={{ marginTop: 12, fontSize: 15 }}>
        Le daremos respuesta a más tardar el <strong>{fecha(acuse.venceRespuestaEn)}</strong>.
      </p>

      <div className="credenciales">
        <Credencial titulo="Código del caso" valor={acuse.codigo} />
        <Credencial titulo="Contraseña" valor={acuse.clave} />
      </div>

      <Aviso tipo="advertencia">
        <strong>Guárdelos ahora.</strong> Es la única vez que los verá. No los enviamos por correo y no se pueden
        recuperar, porque la empresa no los conoce.
      </Aviso>

      <div className="acciones-formulario">
        <button type="button" className="boton boton-secundario" onClick={descargar}>
          <Download /> Descargar comprobante
        </button>
        <Link to="/seguimiento" className="boton boton-primario">
          Ir al seguimiento <ArrowRight />
        </Link>
      </div>
    </div>
  );
}
