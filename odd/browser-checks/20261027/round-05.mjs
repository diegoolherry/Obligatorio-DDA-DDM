const task = await taskSpace(2);
await task.handOff();
console.log('Private password entry is still required; no password has been entered and no sign-in was submitted.');
