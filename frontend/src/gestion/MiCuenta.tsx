import { useState } from "react";
import { roles } from "../api/textos";
import { Aviso } from "../componentes/comunes";
import { CambiarClave } from "./Activacion";
import { useSesion } from "./sesion";

export default function MiCuenta() {
  const { sesion } = useSesion();
  const [cambiada, setCambiada] = useState(false);

  if (!sesion) {
    return null;
  }

  return (
    <>
      <div className="titulo-panel">
        <div>
          <h1>Mi cuenta</h1>
          <p>Sus datos de acceso al panel.</p>
        </div>
      </div>

      <div style={{ display: "grid", gap: 20, maxWidth: 620 }}>
        <section className="tarjeta tarjeta-cuerpo">
          <dl className="datos-caso">
            <div>
              <dt>Nombre</dt>
              <dd>{sesion.nombre}</dd>
            </div>
            <div>
              <dt>Correo</dt>
              <dd>{sesion.correo}</dd>
            </div>
            <div>
              <dt>Rol</dt>
              <dd>{roles[sesion.rol]}</dd>
            </div>
            <div>
              <dt>Segundo factor</dt>
              <dd>Activo. Si cambia de celular, pida al oficial de cumplimiento que lo restablezca.</dd>
            </div>
          </dl>
        </section>

        {cambiada && <Aviso tipo="exito">Su contraseña se cambió. Las sesiones en otros equipos se cerraron.</Aviso>}

        <section className="tarjeta">
          <CambiarClave alTerminar={() => setCambiada(true)} key={cambiada ? "cambiada" : "inicial"} />
        </section>
      </div>
    </>
  );
}
