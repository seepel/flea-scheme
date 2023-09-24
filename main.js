import { readFileSync } from "fs";
import * as WAForth from "./waforth.js";

const withLineBuffer = WAForth.withLineBuffer;
const forth = new WAForth.default();
forth.onEmit = withLineBuffer((c) => {
  process.stdout.write(c);
});

const src = readFileSync("./main.fs", "utf8");

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

forth.bind("ALLOCATE", (forth) => {
  const addr = walloc.exports.walloc(forth.pop());
  forth.push(addr)
});

forth.bind("FREE",  (forth) => { 
  const result = walloc.exports.wfree(forth.pop());
  forth.push(result);
});

forth.interpret(readFileSync("./memory.fs", "utf8"));

forth.bind("SP", (forth) => {
  forth.push(forth.core.exports.tos());
});
forth.bind("RP", (forth) => {
  forth.push(forth.core.exports.tors());
});

forth.bind("SETSP", (forth) => {
  forth.core.exports.settos(forth.pop());
});

forth.bind("SETRP", (forth) => {
  forth.core.exports.settors(forth.pop());
});

forth.interpret(`
: SP@ S" SP" SCALL ;
: RP@ S" RP" SCALL ;
: SP! S" SETSP" SCALL ;
: RP! S" SETRP" SCALL ;
`);


forth.interpret(src);

