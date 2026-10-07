# Primer arranque local de los scaffolds

Objetivo: iniciar las bases existentes de API, Web y Mobile y reconocer sus pantallas/respuestas de plantilla. No es un tutorial de autenticación, compras ni integración entre las tres partes.

## Antes de empezar

- Partí de la raíz del repositorio. Cada bloque de arranque comienza **desde la raíz, en una terminal separada**; no encadenes sus `cd`.
- API y Web declaran `net10.0` en [API-Berruti.csproj](../../src/Api/API-Berruti/API-Berruti/API-Berruti.csproj) y [Web-App.csproj](../../src/Web/Web-App/Web-App/Web-App.csproj). Usá SDK .NET 10; la verificación previa usó `10.0.401`. Si preferís Visual Studio, necesitás compatibilidad con ese SDK y `.slnx`; el IDE no se verificó.
- Mobile usa Expo `57.0.27`, React `19.2.3`, RN `0.86.3` y TypeScript `6.0.3`, fijados en [package.json](../../src/Mobile/package.json) y [package-lock.json](../../src/Mobile/package-lock.json). Node `24.11.1` y npm `11.19.0` son el entorno observado, no un mandato de actualización. Usá Node compatible con esas dependencias.
- Para ver Mobile, necesitás un celular con un cliente Expo compatible y conectividad con Metro. La compatibilidad del dispositivo debe comprobarse; no presupongas que cualquier versión de Expo Go acepta el SDK.
- La instalación/restauración necesita acceso a los registros de paquetes. No se requieren MySQL, credenciales, archivos de secretos ni URL API para estas plantillas.

## 1. Iniciá API

PowerShell o Bash, desde la raíz:

```sh
cd src/Api/API-Berruti
dotnet build API-Berruti.slnx --nologo
dotnet run --project API-Berruti/API-Berruti.csproj --no-build --launch-profile http
```

El build restaura dependencias y debe finalizar sin errores. Esperá el mensaje de escucha en `http://localhost:5095` y entorno `Development`, según el perfil existente. Abrí `http://localhost:5095/weatherforecast`: se esperan cinco registros JSON de ejemplo. El [contrato real](../reference/api.md) se consulta en `http://localhost:5095/openapi/v1.json`.

## 2. Iniciá Web

En otra terminal PowerShell o Bash, desde la raíz:

```sh
cd src/Web/Web-App
dotnet build Web-App.slnx --nologo
dotnet run --project Web-App/Web-App.csproj --no-build --launch-profile http
```

Esperá escucha en `http://localhost:5025`. El perfil puede abrir el navegador; si no lo hace, abrí esa dirección. Las rutas `/`, `/counter` y `/weather` son páginas de plantilla: Counter declara InteractiveServer por página, no global; Weather genera datos aleatorios locales, no consume la API. Recibir HTML no prueba la interactividad de Blazor ni consumo de la API.

## 3. Iniciá Mobile

En otra terminal PowerShell o Bash, desde la raíz:

```sh
cd src/Mobile
npm ci
npm run typecheck
npm start
```

`npm ci` usa el lockfile existente; no ejecutes `npm audit fix` para resolver este arranque. Esperá el mensaje de Metro y el QR/dirección de conexión. Conectá el cliente compatible del celular en la misma red. El código de [App.tsx](../../src/Mobile/App.tsx) muestra «Base móvil» y «Expo · React Native · TypeScript», con componentes nativos y StyleSheet; no contiene llamadas API.

Si necesitás túnel o aparece el error de ngrok de Windows, seguí [resolución de problemas](../how-to/troubleshooting.md); el túnel de Metro no publica la API. Para direcciones de servicios consultá [cliente/API](../how-to/client-api-configuration.md).

`npm run android` / `npm run ios` requieren un entorno compatible; `npm run export:android` exporta JavaScript, no compila una aplicación Android nativa.

Web conserva solo Bootstrap CSS `5.3.3` y su LICENSE del PR #20, no las 43 variantes/scripts/mapas excluidos. El trailer `sourceMappingURL` se mantiene, pero no se distribuye el mapa ni se promete depuración con sourcemaps en DevTools.

## 4. Cerrá lo que iniciaste

Presioná **Ctrl+C en cada una de tus tres terminales** y esperá el regreso al prompt. Detené solo los procesos que iniciaste; no cierres procesos ajenos por número de puerto. Si interrumpiste una ejecución, confirmá que tu terminal ya no mantiene el servidor antes de reintentarlo.

## Qué demuestra este recorrido

Estos comandos son instrucciones para tu ejecución, no resultados nuevos de este cambio documental. El [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración) distingue los builds y HTTP observados previamente por un verificador, los timeouts históricos de Metro y el arranque en celular reportado después por el usuario. Aquella comprobación .NET usó direcciones explícitas con `--no-launch-profile`, no estos perfiles. No acreditó TLS, interactividad del navegador ni integración Web/Mobile/API. La revisión cruzada de Enzo y la auditoría de dependencias siguen pendientes allí documentadas.
