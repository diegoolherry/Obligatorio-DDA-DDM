const task = await takeOverTaskSpace(2);
const page = task.userPage();
console.log({ url: await page.url(), state: await page.evaluate(() => ({
  headings: [...document.querySelectorAll('h1,h2,h3')].map(x => x.textContent),
  statuses: [...document.querySelectorAll('[role="status"],[role="alert"]')].map(x => x.textContent),
  buttons: [...document.querySelectorAll('button')].map(x => ({ text: x.textContent, disabled: x.disabled })),
  passwordEntered: [...document.querySelectorAll('input[type="password"]')].some(x => x.value.length > 0)
})) });
