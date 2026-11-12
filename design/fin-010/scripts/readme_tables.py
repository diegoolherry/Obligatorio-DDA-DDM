# Genera las tablas Markdown del README desde tokens.py (misma fórmula WCAG 2.1). Uso: python -I scripts/readme_tables.py
import io, os, runpy, contextlib
D = os.path.dirname(os.path.abspath(__file__))
with contextlib.redirect_stdout(io.StringIO()):
    g = runpy.run_path(os.path.join(D, "tokens.py"))
T, cr = g["T"], g["cr"]
f = lambda r: f"{r:.2f}".replace(".", ",")
use = {
 "primary-50":"Fondo de aviso/botón secundario pulsado","primary-100":"Fondo suave rojo","primary-500":"Rojo intermedio (iconos, gráficos)","primary-600":"Botón primario, foco","primary-700":"Enlaces, botón primario pulsado, texto rojo","primary-900":"Rojo muy oscuro (reserva)",
 "accent-100":"Fondo de etiqueta naranja","accent-500":"Relleno naranja con texto ink-900 (acento, pestaña activa, franja). Nunca texto sobre blanco","accent-700":"Texto naranja sobre blanco / accent-100; acento pulsado",
 "ink-900":"Texto principal, encabezado","ink-700":"Texto secundario fuerte","ink-500":"Texto de ayuda, iconos inactivos","ink-300":"Solo bordes de campos (UI 3:1), no texto",
 "surface-0":"Tarjetas, campos","surface-50":"Fondo de pantalla","surface-100":"Deshabilitado, Utilizado","border-200":"Separadores decorativos (no señal única)",
 "success-100":"Fondo Válido","success-700":"Texto/icono éxito","error-100":"Fondo de error","error-700":"Texto/icono error, botón destructivo","error-800":"Destructivo pulsado",
 "warning-100":"Fondo Pendiente / desactualizado","warning-500":"Relleno/icono (texto ink-900)","warning-800":"Texto advertencia","info-100":"Fondo informativo","info-700":"Texto/icono informativo"}
print("| Token | Hex | Uso | Blanco | ink-900 |\n|---|---|---|---|---|")
for k, v in T.items():
    print(f"| `{k}` | `{v}` | {use[k]} | {f(cr(v,'#FFFFFF'))}:1 | {f(cr(v,T['ink-900']))}:1 |")
pairs = [("ink-900","surface-0"),("ink-900","surface-50"),("ink-700","surface-0"),("ink-500","surface-0"),("ink-500","surface-50"),("ink-500","surface-100"),("ink-700","surface-100"),
("surface-0","primary-600"),("surface-0","primary-700"),("primary-700","surface-0"),("primary-700","primary-50"),("ink-900","accent-500"),("surface-0","accent-700"),("accent-700","surface-0"),("accent-700","accent-100"),
("surface-0","error-700"),("surface-0","error-800"),("error-700","error-100"),("success-700","success-100"),("warning-800","warning-100"),("info-700","info-100"),("surface-0","ink-900"),("ink-900","warning-500")]
print("\n| Texto | Fondo | Ratio | Resultado |\n|---|---|---|---|")
for a,b in pairs:
    r=cr(T[a],T[b]); print(f"| `{a}` {T[a]} | `{b}` {T[b]} | {f(r)}:1 | {'AA (4,5:1)' if r>=4.5 else 'falla'} |")
print("\nNo usar: accent-500 sobre surface-0:", f(cr(T['accent-500'],T['surface-0'])), "· sobre surface-50:", f(cr(T['accent-500'],T['surface-50'])), "· ink-300 sobre surface-0:", f(cr(T['ink-300'],T['surface-0'])), "· border-200 sobre surface-0:", f(cr(T['border-200'],T['surface-0'])))
