const task = await taskSpace(2);
const page = task.page('p1');
await page.goto('https://localhost:7054/ingreso');
console.log(await page.snapshot());
