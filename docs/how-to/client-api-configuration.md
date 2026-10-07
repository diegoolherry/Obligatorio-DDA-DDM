# Elegir direcciones locales de cliente y API

Usá esta guía para distinguir direcciones de escucha y direcciones alcanzables desde cada dispositivo. **Todavía no hay integración cliente/API acreditada ni una opción de URL API consumida por el scaffold Mobile**. No agregues una variable o un archivo de configuración suponiendo que el código lo leerá.

## Identificá cómo se inició el servidor

Los perfiles de [API](../../src/Api/API-Berruti/API-Berruti/Properties/launchSettings.json) y [Web](../../src/Web/Web-App/Web-App/Properties/launchSettings.json) declaran:

| Servicio | Perfil `http` | Perfil `https` | Entorno de ambos perfiles |
| --- | --- | --- | --- |
| API | `http://localhost:5095` | `https://localhost:7053;http://localhost:5095` | `ASPNETCORE_ENVIRONMENT=Development` |
| Web | `http://localhost:5025` | `https://localhost:7054;http://localhost:5025` | `ASPNETCORE_ENVIRONMENT=Development` |

El [tutorial](../tutorials/first-local-run.md) usa el perfil `http`. La verificación previa utilizó `--no-build --no-launch-profile --urls http://127.0.0.1:5095` para API y `--urls http://127.0.0.1:5025` para Web, con entorno Development explícito. Esos son **overrides de aquella verificación**, aunque los números coincidan con los perfiles; sin perfil no se heredan sus variables. Comprobá siempre el mensaje de escucha y el entorno del proceso real.

API expone OpenAPI solo en Development. Ambos servidores incluyen redirección HTTPS; un arranque HTTP sin puerto HTTPS conocido puede advertir `Failed to determine the https port for redirect`. La comprobación HTTP previa no valida certificados ni TLS; ver [problemas conocidos](troubleshooting.md).

## Elegí una dirección alcanzable

| Desde dónde se haría la solicitud | Significado de `localhost` / `127.0.0.1` |
| --- | --- |
| Navegador en la misma computadora | Esa computadora; puede alcanzar los servidores locales que escuchan allí. |
| Celular físico | El propio celular, **no** la computadora que ejecuta la API. |
| Emulador | El entorno emulado; el acceso al host depende del emulador y su red. No se verificó uno en este proyecto. |

Para una futura prueba desde un celular, será necesario acordar una dirección LAN alcanzable del host, una escucha que admita esa interfaz y las reglas de firewall/red correspondientes. No basta con sustituir `localhost`: los perfiles actuales son locales. No se verificó aquí exposición LAN, CORS, certificados de dispositivo ni conexión cliente/API. Un túnel Expo sirve al desarrollo de Mobile/Metro; **no convierte automáticamente la API local en una API pública**.

## Antes de implementar configuración cliente

1. Revisá el [contrato implementado](../reference/api.md) y los [requisitos](../reference/requirements.md); no presupongas endpoints de dominio.
2. Acordá con el equipo el mecanismo mínimo y quién consumirá la configuración. No hay una clave de URL API implementada que documentar en este scaffold.
3. No pongas secretos en clientes. En Expo, las variables `EXPO_PUBLIC_*` son públicas y pueden incorporarse al bundle: no sirven para tokens privados, claves de proveedor ni credenciales de base de datos. Esta advertencia no introduce una variable consumida por el código.
4. Cuando exista integración, validá alcance de red, contrato, errores y autorización con pruebas; una dirección escrita en documentación no los demuestra.

Las decisiones y límites de arquitectura están en el [MVP](../explanation/mvp.md); la evidencia de ejecución y los pendientes están en el [informe](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración).
