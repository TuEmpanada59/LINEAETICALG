import { useEffect, useState } from "react";
import { FolderKanban, LogOut, Menu, UserCog, Users } from "lucide-react";
import { Navigate, NavLink, Outlet, useLocation } from "react-router";
import { iniciales, roles } from "../api/textos";
import { Cargando, Marca } from "../componentes/comunes";
import { useSesion } from "./sesion";

export default function DisenoPanel() {
  const { sesion, cargando, salir } = useSesion();
  const [menuAbierto, setMenuAbierto] = useState(false);
  const { pathname } = useLocation();

  useEffect(() => {
    setMenuAbierto(false);
  }, [pathname]);

  if (cargando) {
    return <Cargando />;
  }

  if (!sesion) {
    return <Navigate to="/gestion/ingresar" replace />;
  }

  if (!sesion.completa) {
    return <Navigate to="/gestion/activar" replace />;
  }

  const esOficial = sesion.rol === "OficialCumplimiento";

  return (
    <div className="panel" data-menu={menuAbierto ? "abierto" : "cerrado"}>
      <aside className="lateral">
        <Marca destino="/gestion" subtitulo="Panel de gestión" />
        <nav aria-label="Panel">
          <NavLink to="/gestion" end>
            <FolderKanban /> Casos
          </NavLink>
          {esOficial && (
            <NavLink to="/gestion/gestores">
              <Users /> Gestores
            </NavLink>
          )}
          <NavLink to="/gestion/cuenta">
            <UserCog /> Mi cuenta
          </NavLink>
        </nav>
        <div className="usuario-panel">
          <div className="fila">
            <span className="avatar" aria-hidden="true">
              {iniciales(sesion.nombre)}
            </span>
            <div>
              <strong>{sesion.nombre}</strong>
              <small>{roles[sesion.rol]}</small>
            </div>
          </div>
          <button type="button" className="boton boton-fantasma boton-pequeno" onClick={salir}>
            <LogOut /> Cerrar sesión
          </button>
        </div>
      </aside>

      {menuAbierto && <div className="velo" onClick={() => setMenuAbierto(false)} />}

      <div style={{ minWidth: 0 }}>
        <header className="barra-movil">
          <button type="button" className="boton boton-fantasma boton-icono" onClick={() => setMenuAbierto(true)} aria-label="Abrir menú">
            <Menu />
          </button>
          <Marca destino="/gestion" subtitulo="Panel de gestión" />
        </header>
        <main className="contenido-panel">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
