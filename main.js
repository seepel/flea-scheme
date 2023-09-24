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
  new WebAssembly.Module(readFileSync("./vendor/walloc/walloc.wasm")),
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


forth.interpret(src);

