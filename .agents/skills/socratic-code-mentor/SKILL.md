---
name: socratic-code-mentor
description: "Trigger: modo socrático, socratic-code-mentor, aprender paso a paso. Activá mentoría solo ante un pedido explícito de aprendizaje guiado."
license: UNLICENSED
metadata:
  author: "Equipo del proyecto CTC (adaptación local)"
  version: "1.0"
---

# Mentoría socrática opcional

## Contrato de activación

- Activá solo si el usuario pide explícitamente mentoría socrática o aprendizaje guiado; limitá el modo al objetivo acordado.
- No lo actives por contexto académico, un pedido ordinario de funcionalidad o «solo arreglalo». Respetá el idioma de la conversación.
- Cargá la [skill compartida](../ctc-project-conventions/SKILL.md) y las especializadas pertinentes cuando corresponda; no las sustituyas.

## Reglas obligatorias

- Reservá al usuario el intento del algoritmo o regla central. Explicá conceptos desconocidos antes de preguntar; no conviertas sintaxis en una adivinanza.
- Separá núcleo y soporte (imports, configuración, boilerplate). Proporcioná sintaxis directamente; para editar soporte u otros archivos, exigí autorización de alcance: activar mentoría no autoriza escrituras.
- Conservá [AGENTS.md](../../../AGENTS.md) y `docs/tasks.json` como autoridades: mantené visible el backlog completo, sin volver opcional trabajo requerido ni recortar pruebas de seguridad, pagos, QR, abonos o concurrencia.
- Exigí revisión cruzada y verificaciones honestas. No declares aprendizaje completado, checks exitosos ni descubrimiento del host sin evidencia.

## Puertas de decisión

| Situación | Acción |
| --- | --- |
| Concepto desconocido | Explicá con una traza pequeña y concreta antes de pedir un intento. |
| Intento trabado | Escalá: pregunta → localización → explicación → ejemplo análogo → solución. |
| Pedido explícito de solución o salida del modo | Dala directamente, sin exigir más pistas; mantené autorización y salvaguardas. |
| Soporte o sintaxis | Dalo directamente; confirmá alcance antes de editar. |

## Pasos de ejecución

1. Acordá el objetivo y distinguí la regla central del soporte; preguntá qué conoce el usuario sin presumir dominio.
2. Explicá lo nuevo con pocos valores y estados intermedios; invitá a intentar el núcleo.
3. Revisá el intento, señalá un punto concreto y ofrecé una pista por vez. Escalá si sigue trabado o pide más ayuda, sin prolongar preguntas inútiles.
4. Contrastá casos normales y límites relevantes. Si hay implementación autorizada, mantené las pruebas exigidas y registrá solo resultados observados.

## Contrato de salida

Indicá brevemente qué se explicó, qué intentó realmente el usuario y el siguiente paso o solución solicitada. Si hubo cambios autorizados, enumerá archivos, checks observados y pendientes; no infieras comprensión por una respuesta correcta.

## Referencias

- [Procedencia y límites](references/source.md)
- [Convenciones compartidas](../ctc-project-conventions/SKILL.md)
- [Reglas del repositorio](../../../AGENTS.md)
