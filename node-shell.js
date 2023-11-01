// @ts-check
import * as fs from "fs";
import * as WAForth from "./waforth.js";

const { readFileSync } = fs;

const withLineBuffer = WAForth.withLineBuffer;
export const forth = new WAForth.default();
forth.onEmit = withLineBuffer((c) => {
  process.stdout.write(c);
});

await forth.load();

// Instantiate the memory module
const walloc = await WebAssembly.instantiate(
  new WebAssembly.Module(readFileSync("./walloc.wasm")),
  {
    env: {
      memory: forth.core.exports.memory,
    },
  }
);

// +------------------------------------------------------------------+
// Memory words
// +------------------------------------------------------------------+
forth.bind("ALLOCATE", (forth) => {
  const addr = walloc.exports.walloc(forth.pop());
  forth.push(addr)
});

forth.bind("FREE",  (forth) => { 
  const result = walloc.exports.wfree(forth.pop());
  forth.push(result);
});

// TODO: We'll get to this one
// forth.bind("RESIZE",  (forth) => { });

forth.interpret(readFileSync("./memory.fs", "utf8"), true);

// +------------------------------------------------------------------+
// File words
// +------------------------------------------------------------------+
forth.bind("OPEN-FILE", (forth) => {
  const mode = forth.pop();
  const filename = forth.popString();
  fs.openSync(filename, "rw")
})

forth.bind("INCLUDED", (forth) => {
  const filename = forth.popString();
  const src = readFileSync(filename, "utf8");
  forth.interpret(src, true);
})

forth.interpret(`: INCLUDED S" INCLUDED" SCALL ;`, true);

// Stack words
// Just realized these don't work...
// forth.bind("SP", (forth) => {
//   forth.push(forth.core.exports.tos());
// });
// forth.bind("RP", (forth) => {
//   forth.push(forth.core.exports.tors());
// });
// 
// forth.bind("SETSP", (forth) => {
//   forth.core.exports.settos(forth.pop());
// });
// 
// forth.bind("SETRP", (forth) => {
//   forth.core.exports.settors(forth.pop());
// });
// 
// forth.interpret(`
// : SP@ S" SP" SCALL ;
// : RP@ S" RP" SCALL ;
// : SP! S" SETSP" SCALL ;
// : RP! S" SETRP" SCALL ;
// `, true);

