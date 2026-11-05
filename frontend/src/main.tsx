import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, Outlet } from "react-router";
import { RouterProvider } from "react-router/dom";
import "@fontsource-variable/plus-jakarta-sans";
import "./estilos/base.css";
import "./estilos/publico.css";
import "./estilos/panel.css";
import Activacion from "./gestion/Activacion";
import Casos from "./gestion/Casos";
import DetalleCaso from "./gestion/DetalleCaso";
import DisenoPanel from "./gestion/DisenoPanel";
import Gestores from "./gestion/Gestores";
import Ingreso from "./gestion/Ingreso";
import MiCuenta from "./gestion/MiCuenta";
import { ProveedorSesion } from "./gestion/sesion";
import Denunciar from "./publico/Denunciar";
import DisenoPublico from "./publico/DisenoPublico";
import Inicio from "./publico/Inicio";
import NoEncontrada, { ErrorInesperado } from "./publico/NoEncontrada";
import Seguimiento from "./publico/Seguimiento";

const rutas = createBrowserRouter([
  {
    element: <DisenoPublico />,
    errorElement: <ErrorInesperado />,
    children: [
      { path: "/", element: <Inicio /> },
      { path: "/denunciar", element: <Denunciar /> },
      { path: "/seguimiento", element: <Seguimiento /> },
      { path: "*", element: <NoEncontrada /> },
    ],
  },
  {
    path: "/gestion",
    errorElement: <ErrorInesperado />,
    element: (
      <ProveedorSesion>
        <Outlet />
      </ProveedorSesion>
    ),
    children: [
      { path: "ingresar", element: <Ingreso /> },
      { path: "activar", element: <Activacion /> },
      {
        element: <DisenoPanel />,
        children: [
          { index: true, element: <Casos /> },
          { path: "casos/:id", element: <DetalleCaso /> },
          { path: "gestores", element: <Gestores /> },
          { path: "cuenta", element: <MiCuenta /> },
        ],
      },
    ],
  },
]);

createRoot(document.getElementById("raiz")!).render(
  <StrictMode>
    <RouterProvider router={rutas} />
  </StrictMode>,
);
