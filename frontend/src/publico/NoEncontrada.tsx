import { AlertTriangle, ArrowLeft, Compass } from "lucide-react";
import { Link } from "react-router";

export default function NoEncontrada() {
  return (
    <div className="contenedor contenedor-estrecho">
      <div className="vacio" style={{ padding: "96px 16px" }}>
        <Compass />
        <strong style={{ fontSize: 20 }}>Esta página no existe</strong>
        <span>Revise la dirección o vuelva al inicio.</span>
        <Link to="/" className="boton boton-secundario" style={{ marginTop: 12 }}>
          <ArrowLeft /> Volver al inicio
        </Link>
      </div>
    </div>
  );
}

/** Se muestra si una pantalla falla de forma inesperada, en lugar del error técnico. */
export function ErrorInesperado() {
  return (
    <div className="contenedor contenedor-estrecho">
      <div className="vacio" style={{ padding: "96px 16px" }}>
        <AlertTriangle />
        <strong style={{ fontSize: 20 }}>Algo salió mal</strong>
        <span>Recargue la página. Si el problema continúa, intente más tarde.</span>
        <a href="/" className="boton boton-secundario" style={{ marginTop: 12 }}>
          <ArrowLeft /> Volver al inicio
        </a>
      </div>
    </div>
  );
}
