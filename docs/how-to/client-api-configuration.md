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

Un túnel Expo sirve al desarrollo de Mobile/Metro; **no convierte automáticamente la API local en una API pública**. No se verificaron exposición LAN ni reglas de firewall.

### Probar en el celular con un túnel de Cloudflare

Las pruebas en iPhone usan un túnel temporal de `cloudflared` hacia la API local. **El túnel debe apuntar al perfil `https`**: el ingreso y las operaciones con sesión exigen HTTPS en la API ([`LimiteAutenticacion`](../../src/Api/API-Berruti/API-Berruti/Security/Authentication/LimiteAutenticacion.cs), [`SesionBearerHandler`](../../src/Api/API-Berruti/API-Berruti/Security/Authentication/SesionBearerHandler.cs)). Con el perfil `http`, la búsqueda anónima funciona pero el ingreso responde `400 httpsRequerido` y la app muestra «Respuesta inválida del servidor» (observado el 2026-11-12).

```powershell
.\scripts\start-api-local.ps1 -Profile https                      # terminal 1: https://localhost:7053
cloudflared tunnel --no-tls-verify --url https://localhost:7053   # terminal 2: copiar la URL https://….trycloudflare.com
cd src\Mobile                                                    # terminal 3
$env:EXPO_PUBLIC_API_URL = "https://<subdominio>.trycloudflare.com"
npx expo start --tunnel -c
```

- `--no-tls-verify` solo afecta el tramo local entre `cloudflared` y la API, que usa el certificado de desarrollo; el iPhone recibe el certificado público de Cloudflare.
- La URL del túnel cambia en cada arranque: volvé a configurar `EXPO_PUBLIC_API_URL` y reiniciá Metro con `-c`.
- La URL es pública mientras el túnel está abierto: cerralo con Ctrl+C al terminar y usá solo cuentas ficticias.

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

Nota: la búsqueda pública de Mobile consulta `GET /api/paradas` y `GET /api/itinerarios` con la misma `EXPO_PUBLIC_API_URL`, de forma anónima (sin token). Sigue sin verificarse contra una API real ni en dispositivo.

## Credencial privada de Mercado Pago TEST

La API tiene endpoints de compra/pago y los clientes Web y Mobile incluyen un flujo local conectado a ellos. Esto refleja la implementación en código, no una verificación integrada. Consultá la [guía de verificaciones de compra](purchase-test-checks.md) para distinguir pruebas offline, persistencia MySQL y operaciones reales pendientes. Una forma de token no demuestra que la credencial sea de prueba: las credenciales TEST de Checkout Pro Orders pueden usar el prefijo `APP_USR` ([credenciales oficiales](https://www.mercadopago.com.uy/developers/en/docs/checkout-pro-orders/resources/credentials)). El backend requiere, fuera del repositorio, `MercadoPago__AccessToken`, `MercadoPago__TestCredentialOriginConfirmed=true`, `MercadoPago__ExpectedSellerId` y `MercadoPago__ExpectedSiteId`. El flag expresa confirmación humana de que se seleccionó una credencial TEST en el panel; además, antes de cada operación el servidor consulta `GET https://api.mercadolibre.com/users/me` y exige el tag exacto `test_user`, el ID de vendedor esperado y el `site_id` esperado ([identidad de usuario](https://developers.mercadolibre.com.uy/es_co/servicios-consulta-usuarios)). `site_id` identifica el mercado esperado y no se infiere ni se fija en el código. Si falta cualquier setting o la identidad no coincide, no se envía la orden, consulta, búsqueda ni devolución. En desarrollo usa solo credenciales ficticias de prueba: nunca las pongas en `EXPO_PUBLIC_*`, código, `appsettings.json` versionado, URLs, logs ni mensajes. La verificación de identidad no se ejecuta al arrancar la API.

El cliente crea órdenes con importe y referencia del servidor, consulta su estado autoritativo y puede solicitar una devolución completa. Antes de cada una verifica la identidad TEST con una llamada de solo lectura; una respuesta ausente, inválida, redirigida o diferente falla de forma segura. El cliente de órdenes usa únicamente el host oficial fijo, bloquea redirecciones HTTP y devuelve errores sanitizados. La URL de checkout debe ser HTTPS en `www.mercadopago.com.uy`, sin credenciales. Solo `processed/accredited` con el importe pagado completo se considera aprobado; `processed/refunded` y `processed/partially_refunded` nunca se aprueban. La solicitud de devolución no demuestra que el proveedor la haya completado: una consulta posterior debe confirmarlo. Las pruebas de esta prevalidación son HTTP simuladas; no validan una cuenta o credencial real ni ejecutan operaciones en TEST. No se admite producción.
