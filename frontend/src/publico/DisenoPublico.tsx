import { useEffect } from "react";
import { NavLink, Outlet, useLocation } from "react-router";
import { Marca } from "../componentes/comunes";

export default function DisenoPublico() {
  const { pathname } = useLocation();
  const enInicio = pathname === "/";

  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);

  return (
    <div className="publico">
      <header className={`barra ${enInicio ? "barra-oscura" : ""}`}>
        <div className="contenedor">
          <Marca />
          <nav aria-label="Principal">
            <NavLink to="/denunciar">
              <span className="largo">Hacer una denuncia</span>
              <span className="corto">Denunciar</span>
            </NavLink>
            <NavLink to="/seguimiento">
              <span className="largo">Consultar mi caso</span>
              <span className="corto">Mi caso</span>
            </NavLink>
          </nav>
        </div>
      </header>

      <main>
        <Outlet />
      </main>

      <footer className="pie">
        <div className="contenedor">
          <span>© {new Date().getFullYear()} Grupo Londoño Gómez · Línea ética</span>
          <span>No registramos su dirección IP ni datos de su dispositivo.</span>
        </div>
      </footer>
    </div>
  );
}
