# API implementada: referencia del scaffold

La referencia del **esquema real** es el documento OpenAPI generado por la aplicación en **Development**, en `/openapi/v1.json`. No se mantiene una copia manual de ese esquema aquí. Para iniciar el servicio seguí el [tutorial](../tutorials/first-local-run.md) y para elegir host/puerto consultá [cliente/API](../how-to/client-api-configuration.md).

Ambos proyectos .NET declaran `net10.0`; la API utiliza `Microsoft.AspNetCore.OpenApi` `10.0.12`.

## Superficie actual

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /WeatherForecast` | Ejemplo de plantilla: devuelve cinco pronósticos con fechas de los próximos días, temperaturas y resúmenes aleatorios; no consulta un servicio meteorológico ni datos del transporte. |
| `GET /openapi/v1.json` | Documento generado; disponible solo en Development, por `MapOpenApi()` dentro de la condición de entorno. |

Fuentes: [WeatherForecastController.cs](../../src/Api/API-Berruti/API-Berruti/Controllers/WeatherForecastController.cs), [WeatherForecast.cs](../../src/Api/API-Berruti/API-Berruti/WeatherForecast.cs) y [Program.cs](../../src/Api/API-Berruti/API-Berruti/Program.cs). Consultá el JSON servido para campos, tipos y respuestas; la variación aleatoria impide esperar valores exactos.

## Límites y evidencia

No hay endpoints de autenticación ni de dominio implementados en este scaffold; `UseAuthorization()` no acredita un sistema de sesión o protección por roles. No hay integración MySQL, pagos, QR ni clientes acreditada. Los [requisitos](requirements.md) describen lo exigido/propuesto, **no esta superficie disponible**.

La verificación previa observó HTTP 200 y cinco registros en `/weatherforecast`, y HTTP 200 con OpenAPI `3.1.1` en `/openapi/v1.json`, bajo Development. Son resultados históricos atribuidos al verificador en el [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración), no pruebas nuevas ni una promesa de idéntica salida en otro entorno. No se probó TLS ni autenticación futura.
