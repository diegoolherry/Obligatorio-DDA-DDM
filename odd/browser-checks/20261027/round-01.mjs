const task = await taskSpace('CTC passenger login');
console.log({ spaceId: task.spaceId });
const page = task.page('p1');
await page.goto('https://localhost:7054/ingreso');
console.log(await page.snapshot());
