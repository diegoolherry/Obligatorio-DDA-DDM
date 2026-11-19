# JWT, roles y Postman: material docente y límites

## Fuente y alcance

`01 - Presentación lunes - JWT, roles y Postman.pdf`, DDA CTC 2026, semana 10, 45 diapositivas. El usuario aportó el PDF y se inspeccionó directamente su texto extraído; no es un extracto NotebookLM. No se ejecutó el código de las diapositivas. Los fragmentos se transcriben desde la extracción de texto; verificá contra el PDF antes de copiarlos literalmente.

Caso docente: API de Equipments con `EquipmentController` (`/api/Equipment`), login público en `POST /api/auth/login`, MySQL y .NET 10. Roles del ejemplo: `Administrador` (todo), `Operador` (consulta, crea y modifica), `Consulta` (solo lectura).

## Decisión del proyecto

Berruti **adoptó JWT sin estado** en `FIN-005`. Fuente: Diego, 2026-11-18, en el chat de trabajo («Migramos a JWT», «JWT puro, sin estado», «7 días, como hoy», «Mantener la actual»); registro en [contratos](../../../../docs/reference/auth-contracts.md#decisiones-de-fin-005-jwt-y-hash-de-contraseñas) e [informe](../../../../docs/project-report.md). Reemplaza las sesiones opacas con huella en MySQL.

| Tema | Decisión vigente | Desvío respecto de la clase |
| --- | --- | --- |
| Token | JWT HS256 firmado por la API, validado con `AddJwtBearer` (emisor, audiencia, clave y vencimiento; solo HS256). | Igual a la clase. |
| Vigencia | **7 días**, `exp` = `venceEnUtc`. | La clase usa 60 minutos y las diapositivas móviles recomiendan tokens cortos. Riesgo aceptado abajo. |
| Logout | `POST /api/autenticacion/salida` responde `204` sin estado: el cliente descarta el token. | Igual a la clase (la presentación no cubre revocación). |
| Contrato de ingreso | Se mantiene `{identificadorUsuario, roles[], token, venceEnUtc}`; solo cambia el formato del token. | La clase devuelve `AccessToken`, `ExpiresAtUtc`, `Nombre`, `Rol`. |
| Roles | `roles[]` (multirrol), un claim `ClaimTypes.Role` por rol, tomados de MySQL al ingresar. | La clase usa un único `Rol`. |
| `sub` | Identificador público opaco (`IdentificadorPublico`), no el ID correlativo ni el correo; el token no lleva correo. | La clase usa `Id` y `Email`. |
| Clave | `Jwt__Key` (≥ 32 bytes) fuera de Git; `Issuer` y `Audience` en `appsettings.json`. La API no arranca sin clave válida. | La clase usa `Jwt_Key`/`Jwt_Issuer`/`Jwt_Audience` en `.env`. |
| Contraseñas | Solo hashes de `IPasswordHasher<Usuario>`; ya no hay texto plano. | Igual a la clase. Es solo el hasher del framework compartido: Identity y OpenIddict **no** se adoptan. |

**Riesgo aceptado (Diego, FIN-005).** Cerrar sesión solo descarta el token en el cliente. Un token robado, un cambio de rol o una cuenta desactivada siguen surtiendo efecto hasta el vencimiento (7 días). No hay lista de revocación ni refresh tokens. Cambiar esta decisión requiere una decisión explícita del equipo.

Sigue aplicando: separación 401/403, `[Authorize]` y roles en la API, rol tomado del servidor y nunca del cliente, y `UseAuthentication` → `UseAuthorization`. El cliente no decodifica ni confía en el payload.

## Enseñado/aplicado en la presentación

### Conceptos (diap. 2–13)

- La arquitectura no cambia: Controller aplica la frontera de acceso; Service conserva las reglas de negocio; Repository persiste; DTO define contratos. JWT se integra al pipeline sin reemplazar esas responsabilidades.
- Autenticación (¿quién llama?) se separa de autorización (¿puede hacer esta acción?), que se evalúa después.
- JWT = `header.payload.signature`. Header: `alg` (`HS256`) y `typ`; no contiene la clave. Payload: claims legibles (`sub`, `email`, `role`, `exp`); nunca contraseñas. Firma: detecta alteraciones; **firmado no es cifrado**.
- Quien copia un token puede leerlo y usarlo mientras esté vigente: usar HTTPS, expiración razonable y no registrar tokens completos.

### Login y credenciales (diap. 14–16)

```csharp
public record LoginRequestDto(string Email, string Password);
public record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc, string Nombre, string Rol);
```

- Solo el login lleva `[AllowAnonymous]`. El controller llama a `_authService.LoginAsync(request)` y traduce `UnauthorizedAccessException` a `Unauthorized(ex.Message)`, coherente con «Service valida y lanza; Controller traduce» de [curso.md](curso.md).
- Repository: `GetByEmailAsync(email)`. Service: rechaza usuarios inexistentes o inactivos y verifica `PasswordHash` con `IPasswordHasher<Usuario>`; contraseña incorrecta → `UnauthorizedAccessException`.
- **Nunca guardar ni comparar contraseñas en texto plano** y no revelar qué dato de acceso falló.

### Emisión del token (diap. 17–22)

```csharp
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
    new(JwtRegisteredClaimNames.Email, usuario.Email),
    new(ClaimTypes.Role, usuario.Rol),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};
```

- El rol sale del `Usuario` consultado en MySQL, nunca del JSON del login. `ClaimTypes.Role` es lo que lee `[Authorize(Roles = ...)]`; el valor debe coincidir exactamente (`Administrador` ≠ `administrador`).
- Firma con `SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText))` y `SecurityAlgorithms.HmacSha256`; si falta `Jwt_Key`, lanza `InvalidOperationException`. La clave nunca viaja al cliente.
- `JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(60), signingCredentials)` y `JwtSecurityTokenHandler().WriteToken(token)`.
- Configuración en `.env` fuera de Git: `Jwt_Key`, `Jwt_Issuer`, `Jwt_Audience`. `Jwt_Key` y `Jwt:Key` son nombres distintos. No publicar la clave real.
- Paquete: `Microsoft.AspNetCore.Authentication.JwtBearer` versión `10.0.0`.

### Validación y pipeline (diap. 23–27)

- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` y `AddAuthorization()`.
- `TokenValidationParameters`: `ValidateIssuer`/`ValidIssuer`, `ValidateAudience`/`ValidAudience`, `ValidateIssuerSigningKey`/`IssuerSigningKey`, `ValidateLifetime = true`. Los valores deben coincidir con los de emisión.
- Orden: `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers`. Sin `UseAuthentication` no se construye la identidad.

### Autorización por rol (diap. 28–32)

- `[Authorize]` en el controller exige identidad en todos sus endpoints; `[Authorize(Roles = "...")]` en el endpoint restringe operaciones sensibles (ejemplo: `DELETE` solo `Administrador`).
- Usar `User.IsInRole(...)` y `User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value` desde la identidad validada; nunca un rol o ID de usuario enviado en el body. La auditoría por usuario se menciona como ejemplo futuro, no implementado.

### 401, 403 y comprobación (diap. 33–44)

- **401**: sin identidad válida (falta header, esquema no reconocido, firma, issuer/audience o vencimiento inválidos).
- **403**: identidad válida sin el rol requerido; no es fallo de contraseña.
- Matriz mínima: sin token → 401; token alterado → 401; token vencido → 401; rol permitido → 200; rol no permitido → 403.
- Postman: `POST /api/auth/login`, copiar `accessToken`, Authorization → Type: Bearer Token, pegar el valor sin comillas ni prefijo `Bearer`.
- Diagnóstico: 401 inesperado → header, issuer/audience, clave, vencimiento u orden del pipeline; 403 inesperado → tipo o capitalización del claim, nombre del atributo o token emitido antes de cambiar el rol. Diagnosticar por etapas **sin desactivar validaciones** y sin exponer el token completo.
- Criterios de revisión docentes: claims y vencimiento, clave fuera de Git, validación de firma/emisor/audiencia/tiempo, login público, roles coherentes por endpoint, 401/403 comprobados y reglas de negocio en el Service.

## Lo que la presentación no demuestra

Refresh tokens, revocación o logout de JWT, almacenamiento del token en Blazor o Mobile (la próxima etapa anunciada es Blazor consumiendo la API), políticas o claims personalizados más allá de roles, rotación de clave y pruebas automatizadas. Postman es una comprobación manual: no reemplaza las pruebas automatizadas que [AGENTS.md](../../../../AGENTS.md) exige para los invariantes de autorización.

## Uso en este proyecto

- Clasificá JWT, `[Authorize]`, roles, 401/403 e `IPasswordHasher<T>` como **enseñados/aplicados en la presentación**.
- `IPasswordHasher<Usuario>` ya está aplicado en la API (FIN-005): las contraseñas se guardan solo como hash y una fila heredada en texto plano se rechaza con el mismo `401`. Las bases locales son descartables: se recrean con `--reset-local`.
- Los roles de Berruti (`Pasajero`, `Cobrador`, `Administrador`) son los del proyecto; los de Equipments pertenecen al ejemplo.
