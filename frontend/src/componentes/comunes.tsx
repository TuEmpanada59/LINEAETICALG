import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import {
  AlertTriangle,
  Check,
  CheckCircle2,
  Clock,
  Copy,
  Info,
  Loader2,
  ShieldCheck,
  X,
  XCircle,
} from "lucide-react";
import { Link } from "react-router";
import { diasHasta, estados, fechaCorta } from "../api/textos";
import type { EstadoDenuncia } from "../api/tipos";

/** Marca del canal. Cuando llegue el logo oficial, se reemplaza el ícono de este componente. */
export function Marca({ destino = "/", subtitulo = "Grupo Londoño Gómez" }: { destino?: string; subtitulo?: string }) {
  return (
    <Link to={destino} className="marca" aria-label="Línea ética, inicio">
      <span className="marca-simbolo" aria-hidden="true">
        <ShieldCheck strokeWidth={2.2} />
      </span>
      <span className="marca-texto">
        <strong>Línea ética</strong>
        <small>{subtitulo}</small>
      </span>
    </Link>
  );
}

type TipoAviso = "info" | "error" | "exito" | "advertencia";

const iconosAviso = { info: Info, error: XCircle, exito: CheckCircle2, advertencia: AlertTriangle };

export function Aviso({ tipo = "info", children }: { tipo?: TipoAviso; children: ReactNode }) {
  const Icono = iconosAviso[tipo];
  return (
    <div className={`aviso aviso-${tipo}`} role={tipo === "error" ? "alert" : undefined}>
      <Icono aria-hidden="true" />
      <div>{children}</div>
    </div>
  );
}

export function ChipEstado({ estado }: { estado: EstadoDenuncia }) {
  return <span className={`chip chip-${estado.toLowerCase()}`}>{estados[estado]}</span>;
}

/** Plazo de respuesta con alerta cuando está próximo o vencido. */
export function Plazo({ vence, cerrada }: { vence: string | null; cerrada: boolean }) {
  if (!vence || cerrada) {
    return <span className="texto-secundario">—</span>;
  }

  const dias = diasHasta(vence);
  if (dias < 0) {
    return (
      <span className="plazo plazo-vencido">
        <AlertTriangle aria-hidden="true" /> Vencido hace {-dias} {-dias === 1 ? "día" : "días"}
      </span>
    );
  }

  return (
    <span className={`plazo ${dias <= 3 ? "plazo-proximo" : ""}`} title={fechaCorta(vence)}>
      <Clock aria-hidden="true" />
      {dias === 0 ? "Vence hoy" : `${dias} ${dias === 1 ? "día" : "días"}`}
    </span>
  );
}

export function Cargando({ texto = "Cargando…" }: { texto?: string }) {
  return (
    <div className="cargando" role="status">
      <Loader2 className="girando" aria-hidden="true" />
      {texto}
    </div>
  );
}

export function Girando() {
  return <Loader2 className="girando" aria-hidden="true" />;
}

export function Modal({
  titulo,
  descripcion,
  onCerrar,
  children,
  pie,
}: {
  titulo: string;
  descripcion?: ReactNode;
  onCerrar: () => void;
  children?: ReactNode;
  pie?: ReactNode;
}) {
  const id = useId();

  useEffect(() => {
    const alPresionar = (e: KeyboardEvent) => e.key === "Escape" && onCerrar();
    document.addEventListener("keydown", alPresionar);
    const desplazamiento = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", alPresionar);
      document.body.style.overflow = desplazamiento;
    };
  }, [onCerrar]);

  return (
    <div className="modal-fondo" onMouseDown={(e) => e.target === e.currentTarget && onCerrar()}>
      <div className="modal" role="dialog" aria-modal="true" aria-labelledby={id}>
        <div className="modal-encabezado">
          <div>
            <h2 id={id}>{titulo}</h2>
            {descripcion && <p>{descripcion}</p>}
          </div>
          <button type="button" className="boton boton-fantasma boton-icono" onClick={onCerrar} aria-label="Cerrar">
            <X />
          </button>
        </div>
        {children && <div className="modal-cuerpo">{children}</div>}
        {pie && <div className="modal-pie">{pie}</div>}
      </div>
    </div>
  );
}

export function BotonCopiar({ texto, etiqueta = "Copiar" }: { texto: string; etiqueta?: string }) {
  const [copiado, setCopiado] = useState(false);

  async function copiar() {
    try {
      await navigator.clipboard.writeText(texto);
      setCopiado(true);
      setTimeout(() => setCopiado(false), 1800);
    } catch {
      // Sin permiso para el portapapeles: el usuario puede seleccionar el texto a mano.
    }
  }

  return (
    <button type="button" className="boton boton-pequeno" onClick={copiar} aria-live="polite">
      {copiado ? <Check /> : <Copy />}
      {copiado ? "Copiado" : etiqueta}
    </button>
  );
}

export function Credencial({ titulo, valor }: { titulo: string; valor: string }) {
  return (
    <div className="credencial">
      <div>
        <small>{titulo}</small>
        <code>{valor}</code>
      </div>
      <BotonCopiar texto={valor} />
    </div>
  );
}

/** Seis casillas para el código de la aplicación de autenticación. Acepta pegar el código completo. */
export function CodigoOtp({
  valor,
  onCambio,
  autoFocus,
  etiqueta,
}: {
  valor: string;
  onCambio: (valor: string) => void;
  autoFocus?: boolean;
  etiqueta: string;
}) {
  const casillas = useRef<(HTMLInputElement | null)[]>([]);
  const digitos = valor.padEnd(6, " ").slice(0, 6).split("");

  function escribir(indice: number, texto: string) {
    const limpio = texto.replace(/\D/g, "");
    if (!limpio) {
      return;
    }

    const nuevo = (valor.slice(0, indice) + limpio).slice(0, 6);
    onCambio(nuevo);
    casillas.current[Math.min(nuevo.length, 5)]?.focus();
  }

  function borrar(indice: number) {
    if (valor.length > indice) {
      onCambio(valor.slice(0, indice));
    } else if (indice > 0) {
      onCambio(valor.slice(0, indice - 1));
      casillas.current[indice - 1]?.focus();
    }
  }

  return (
    <div className="otp" role="group" aria-label={etiqueta}>
      {digitos.map((digito, i) => (
        <span key={i} style={{ display: "contents" }}>
          {i === 3 && <span className="separador" aria-hidden="true" />}
          <input
            ref={(el) => {
              casillas.current[i] = el;
            }}
            inputMode="numeric"
            autoComplete={i === 0 ? "one-time-code" : "off"}
            maxLength={6}
            aria-label={`Dígito ${i + 1}`}
            autoFocus={autoFocus && i === 0}
            value={digito.trim()}
            onFocus={(e) => e.target.select()}
            onChange={(e) => escribir(i, e.target.value)}
            onPaste={(e) => {
              e.preventDefault();
              escribir(0, e.clipboardData.getData("text"));
            }}
            onKeyDown={(e) => {
              if (e.key === "Backspace") {
                e.preventDefault();
                borrar(i);
              } else if (e.key === "ArrowLeft" && i > 0) {
                casillas.current[i - 1]?.focus();
              } else if (e.key === "ArrowRight" && i < 5) {
                casillas.current[i + 1]?.focus();
              }
            }}
          />
        </span>
      ))}
    </div>
  );
}

/** Caja de texto con contador de caracteres. */
export function AreaTexto({
  id,
  valor,
  onCambio,
  maximo,
  ...resto
}: {
  id: string;
  valor: string;
  onCambio: (valor: string) => void;
  maximo: number;
  placeholder?: string;
  required?: boolean;
  rows?: number;
  "aria-describedby"?: string;
}) {
  return (
    <>
      <textarea
        id={id}
        className="entrada"
        value={valor}
        maxLength={maximo}
        onChange={(e) => onCambio(e.target.value)}
        {...resto}
      />
      <span className="contador">
        {valor.length.toLocaleString("es-CO")} / {maximo.toLocaleString("es-CO")}
      </span>
    </>
  );
}
