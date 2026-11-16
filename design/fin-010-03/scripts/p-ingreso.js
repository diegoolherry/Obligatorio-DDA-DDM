// W04 / W05 · Ingreso (normal y credenciales rechazadas)
function Ingreso(name, x, rechazo) {
  const { root, main } = DeskPage(name, x, "ingreso", "visitante");
  const row = H(main, { gap: 64, alignItems: "start", padding: [24, 0, 24, 0] });
  const l = V(row, { name: "Texto", gap: 16 });
  Title(l, "Ingresar", "Con tu cuenta podés consultar la ubicación de un servicio. Los administradores acceden además a la administración operativa.", { s: 40, ss: 18 });
  const note = H(l, { gap: 12, alignItems: "start", padding: 16, cornerRadius: 12, fill: F("info-100"), name: "Aviso de sesión" });
  IC(note, "info", 22, "info-700");
  T(note, "La sesión se mantiene solo mientras esta pestaña está abierta. Si recargás la página, tenés que ingresar de nuevo.", { s: 14, c: "info-700", fill: true, lh: 1.45 });
  const c = Card(row, { name: "Formulario de ingreso", width: 480, padding: 32, gap: 20, effects: SH.md });
  T(c, "Datos de tu cuenta", { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
  if (rechazo) StateBox(c, "error", "No pudimos ingresar", "Correo o contraseña incorrectos. Revisá los datos e intentá de nuevo.");
  Field(c, "Correo", "lucia.pereyra@ejemplo.com", { icon: "mail", st: rechazo ? "default" : "focus" });
  Field(c, "Contraseña", rechazo ? "" : "", { icon: "lock", placeholder: rechazo ? "" : "Tu contraseña", st: "default" });
  Btn(c, "Ingresar", { icon: "log-in" });
  T(c, rechazo ? "Conservamos tu correo; la contraseña se borra por seguridad." : "Enter envía el formulario. Con Tab pasás de un campo al siguiente.", { s: 14, c: "ink-500", fill: true, lh: 1.4 });
  Footer(root, false);
}
Ingreso("W04-ingreso", 2800, false);
Ingreso("W05-ingreso-rechazado", 4200, true);
