# Requisitos del canal de denuncias

Fuente: propuesta revisada por la asesoría jurídica (respuestas del 21/10/2026, documento devuelto el 03/11/2026).

## Definido

| Tema | Decisión |
|---|---|
| Alcance | Todas las empresas del grupo Londoño Gómez (multiempresa) |
| Idioma | Español |
| Quién denuncia | Empleados, proveedores, clientes y cualquier tercero, sin crear cuenta |
| Modos | **A nombre propio** (nombre, correo y tipo de relación) y **anónimo**. Sin modo anonimizado |
| Conductas | Fraude, soborno, corrupción, conflicto de intereses, acoso laboral, otras |
| Acuse de recibo | Inmediato, con código y contraseña de seguimiento y mensaje de "recibido" |
| Responsable | Oficial de cumplimiento |
| Denuncia contra un responsable | **No** se reasigna automáticamente; el oficial asigna a mano |
| Retención | Los datos personales se borran tras un plazo contado desde el cierre del caso |
| Textos legales | Aviso de privacidad, autorización de tratamiento de datos y política de no represalias |
| Normativa base | Capítulo IX de la Circular Básica Jurídica. Ley 1581/2012, Decreto 1377/2013 y Ley 1010/2006 a definir a futuro |

## Pendiente de la asesoría jurídica (configurable, no bloquea el desarrollo)

- Plazo de respuesta al denunciante → `Canal__PlazoRespuestaDias`
- Plazo de retención de datos personales → `Canal__RetencionDiasTrasCierre`
- Texto del mensaje de acuse → `Canal__TextoAcuse`
- Redacción final del aviso de privacidad, autorización y política de no represalias → `frontend/src/textos.ts`
- Qué hacer cuando la denuncia involucra al oficial de cumplimiento
- Superintendencia cuya Circular Básica Jurídica aplica
- Lista de empresas del grupo y de gestores

## Estado de la implementación

Hecho en esta primera entrega:
- Formulario público (anónimo / a nombre propio), acuse con código y contraseña.
- Seguimiento con código y contraseña, buzón bidireccional, aportar información nueva.
- Panel de gestión: inicio de sesión, lista de casos, detalle, cambio de estado, asignación manual (solo oficial), mensajes, anotaciones.
- Bitácora de toda actuación (incluye cada consulta del caso).
- Cifrado AES-256-GCM de relato, mensajes y datos personales; datos personales en tabla aparte.
- Sin registro de IP ni dispositivo; límite de intentos contra fuerza bruta.
- Borrado automático diario de datos personales vencidos; se conserva la bitácora.

Siguiente:
- Adjuntos con limpieza de metadatos (autor, GPS) y almacenamiento cifrado en Azure Blob Storage.
- Avisos por correo a gestores y al denunciante a nombre propio (sin contenido de la denuncia).
- Descarga del expediente (PDF) e informes estadísticos (Excel).
- Administración de empresas y gestores desde el panel.
- Alertas de vencimiento según el plazo de respuesta.
- Despliegue en Azure (App Service + Azure SQL + Key Vault) y pipeline en Azure DevOps.
