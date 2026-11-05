import {
  ArrowRight,
  EyeOff,
  FileLock2,
  HeartHandshake,
  KeyRound,
  Lock,
  MessageSquareText,
  ShieldCheck,
} from "lucide-react";
import { Link } from "react-router";
import { categorias } from "../api/textos";
import type { CategoriaConducta } from "../api/tipos";
import { iconosCategoria } from "../componentes/iconos";

export default function Inicio() {
  return (
    <>
      <section className="heroe">
        <div className="contenedor">
          <span className="heroe-etiqueta">
            <span>
              <ShieldCheck strokeWidth={2.5} />
            </span>
            Canal confidencial del Grupo Londoño Gómez
          </span>
          <h1>
            Lo que usted vio importa. <em>Cuéntenoslo con confianza.</em>
          </h1>
          <p>
            Reporte fraude, soborno, corrupción, conflictos de intereses o acoso laboral. Puede hacerlo a nombre propio o
            de forma anónima, sin crear una cuenta.
          </p>
        </div>
      </section>

      <div className="contenedor">
        <div className="accesos">
          <Link to="/denunciar" className="acceso">
            <span className="acceso-icono">
              <MessageSquareText />
            </span>
            <h2>Hacer una denuncia</h2>
            <p>Toma unos minutos. Al terminar recibirá un código y una contraseña para seguir su caso.</p>
            <span className="acceso-enlace">
              Empezar <ArrowRight />
            </span>
          </Link>
          <Link to="/seguimiento" className="acceso acceso-seguir">
            <span className="acceso-icono">
              <KeyRound />
            </span>
            <h2>Consultar mi caso</h2>
            <p>Vea el estado de su denuncia, lea las respuestas del equipo y aporte nueva información.</p>
            <span className="acceso-enlace">
              Ingresar <ArrowRight />
            </span>
          </Link>
        </div>

        <section className="seccion" aria-labelledby="como-funciona">
          <div className="seccion-titulo">
            <span className="antetitulo">Cómo funciona</span>
            <h2 id="como-funciona">Tres pasos, y usted decide cuánto compartir</h2>
          </div>
          <div className="pasos-inicio">
            <article className="paso-inicio">
              <h3>Cuéntenos qué pasó</h3>
              <p>Describa los hechos con el detalle que tenga. Puede dejar en blanco lo que no sepa.</p>
            </article>
            <article className="paso-inicio">
              <h3>Guarde su código</h3>
              <p>Le daremos un código y una contraseña. Son la única forma de volver a su caso: no los conocemos.</p>
            </article>
            <article className="paso-inicio">
              <h3>Siga la gestión</h3>
              <p>El equipo de cumplimiento le responderá por el buzón del caso, sin que usted tenga que identificarse.</p>
            </article>
          </div>
        </section>

        <section className="seccion" aria-labelledby="que-reportar">
          <div className="seccion-titulo">
            <span className="antetitulo">Qué puede reportar</span>
            <h2 id="que-reportar">Conductas contrarias a la ética o a la ley</h2>
            <p>Si no está seguro de la categoría, elija «Otra conducta». El equipo la revisará igual.</p>
          </div>
          <div className="categorias">
            {(Object.keys(categorias) as CategoriaConducta[]).map((clave) => {
              const Icono = iconosCategoria[clave];
              return (
                <article className="categoria" key={clave}>
                  <span className="categoria-icono">
                    <Icono />
                  </span>
                  <div>
                    <h3>{categorias[clave].nombre}</h3>
                    <p>{categorias[clave].descripcion}</p>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <section className="seccion" aria-labelledby="proteccion">
          <div className="garantias">
            <div>
              <h2 id="proteccion">Su protección es lo primero</h2>
              <p>
                Este canal está diseñado para que nadie pueda identificarle si usted no lo decide. La empresa prohíbe
                cualquier represalia contra quien denuncia de buena fe.
              </p>
            </div>
            <ul>
              <li>
                <EyeOff aria-hidden="true" />
                <div>
                  <strong>Sin rastros</strong>
                  <span>No guardamos su dirección IP ni información de su dispositivo.</span>
                </div>
              </li>
              <li>
                <FileLock2 aria-hidden="true" />
                <div>
                  <strong>Información cifrada</strong>
                  <span>Su relato y sus datos se guardan cifrados. Solo el equipo de cumplimiento los lee.</span>
                </div>
              </li>
              <li>
                <Lock aria-hidden="true" />
                <div>
                  <strong>Acceso registrado</strong>
                  <span>Cada consulta de su caso queda en una bitácora que no se puede modificar.</span>
                </div>
              </li>
              <li>
                <HeartHandshake aria-hidden="true" />
                <div>
                  <strong>Sin represalias</strong>
                  <span>Denunciar de buena fe está protegido por la política del grupo.</span>
                </div>
              </li>
            </ul>
          </div>
        </section>
      </div>
    </>
  );
}
