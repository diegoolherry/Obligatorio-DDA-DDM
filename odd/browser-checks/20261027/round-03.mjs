const task = await taskSpace(2);
const page = task.page('p1');
await page.fill('loc=css:input[name="_solicitud.Correo"]', 'pasajero.demo@example.test');
await task.handOff();
console.log('Browser handed to user for private passenger password entry and sign-in.');
