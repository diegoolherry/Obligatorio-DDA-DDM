# Elegir direcciones locales de cliente y API

Usá esta guía para distinguir direcciones de escucha y direcciones alcanzables desde cada dispositivo. Los clientes ya consumen opciones concretas de URL, pero **la integración real cliente/API todavía no está acreditada**. Una configuración escrita no prueba conexión, permisos ni TLS.

## Identificá cómo se inició el servidor

Los perfiles de [API](../../src/Api/API-Berruti/API-Berruti/Properties/launchSettings.json) y [Web](../../src/Web/Web-App/Web-App/Properties/launchSettings.json) declaran:

| Servicio | Perfil `http` | Perfil `https` | Entorno de ambos perfiles |
| --- | --- | --- | --- |
| API | `http://localhost:5095` | `https://localhost:7053;http://localhost:5095` | `ASPNETCORE_ENVIRONMENT=Development` |
| Web | `http://localhost:5025` | `https://localhost:7054;http://localhost:5025` | `ASPNETCORE_ENVIRONMENT=Development` |

El [tutorial](../tutorials/first-local-run.md) usa el perfil `http`. La verificación previa utilizó `--no-build --no-launch-profile --urls http://127.0.0.1:5095` para API y `--urls http://127.0.0.1:5025` para Web, con entorno Development explícito. Esos son **overrides de aquella verificación**, aunque los números coincidan con los perfiles; sin perfil no se heredan sus variables. Comprobá siempre el mensaje de escucha y el entorno del proceso real.

API expone OpenAPI solo en Development. Ambos servidores incluyen redirección HTTPS; un arranque HTTP sin puerto HTTPS conocido puede advertir `Failed to determine the https port for redirect`. La comprobación HTTP previa no valida certificados ni TLS; ver [problemas conocidos](troubleshooting.md). El ingreso requiere HTTPS en ambos tramos: navegador→Web (`7054`) y Web/Mobile→API (`7053`). `/ingreso` rechaza HTTP antes de renderizar; las operaciones sensibles de API también lo rechazan. No confiar solo en una redirección cuando no hay puerto HTTPS conocido.

## Elegí una dirección alcanzable

| Desde dónde se haría la solicitud | Significado de `localhost` / `127.0.0.1` |
| --- | --- |
| Navegador en la misma computadora | Esa computadora; puede alcanzar los servidores locales que escuchan allí. |
| Celular físico | El propio celular, **no** la computadora que ejecuta la API. |
| Emulador | El entorno emulado; el acceso al host depende del emulador y su red. No se verificó uno en este proyecto. |

Para una futura prueba desde un celular, será necesario acordar una dirección LAN alcanzable del host, una escucha que admita esa interfaz y las reglas de firewall/red correspondientes. No basta con sustituir `localhost`: los perfiles actuales son locales. No se verificó aquí exposición LAN, CORS, certificados de dispositivo ni conexión cliente/API. Un túnel Expo sirve al desarrollo de Mobile/Metro; **no convierte automáticamente la API local en una API pública**.

## Opciones consumidas por los clientes

| Consumidor | Opción pública | Valor esperado |
| --- | --- | --- |
| Servicio Web | `Api:BaseUrl` (entorno `Api__BaseUrl`) | Raíz HTTPS de API; en la misma computadora, el perfil existente declara `https://localhost:7053/`. |
| `App.tsx` de Mobile | `EXPO_PUBLIC_API_URL` | Raíz/origen HTTPS alcanzable desde el iPhone, sin credenciales, ruta, query ni fragmento. No usar localhost del host ni el túnel Metro como sustituto automático. |

Ambas opciones son URLs públicas, no secretos ni tokens. Los servicios las validan antes de enviar credenciales. Para alcance/custodia y errores consultá las guías [Web](web-passenger-login.md) y [Mobile](mobile-passenger-login.md).

1. Revisá el [contrato implementado](../reference/api.md); no presupongas endpoints de transporte.
2. La conexión MySQL privada pertenece únicamente a la API mediante `ConnectionStrings:Berruti`; no colocar su valor en URL, cliente, bundle, chat ni archivos versionados. La [guía API](api-session-checks.md) distingue modos y preparación pendiente.
3. `EXPO_PUBLIC_*` puede incorporarse al bundle: nunca sirve para credenciales, tokens ni claves de proveedor. No hay almacenamiento persistente de sesión implementado en los clientes.
4. Validá alcance de red, contratos, errores, permisos y certificados con pruebas reales. No se acreditaron aquí LAN/iPhone/TLS ni flujo de ingreso; no desactivar validación TLS ni cambiar firewall/certificados por defecto.

Las decisiones y límites de arquitectura están en el [MVP](../explanation/mvp.md); la evidencia de ejecución y los pendientes están en el [informe](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración).
