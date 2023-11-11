import { describe, test, expect } from "@jest/globals";
import {readFileSync} from "fs";
import wabt from 'wabt';

async function wat2wasm(watSource) {
  // Load the Wabt module
  const wabtInterface = await wabt();

  // Parse and convert the WAT source to a binary Wasm module
  const wasmModule = wabtInterface.parseWat('forth_interpreter.wat', watSource);
  const { buffer } = wasmModule.toBinary({});

  return buffer;
}

const src = readFileSync('./forth32.wat', 'utf8');
const bin = await wat2wasm(src);
const memory = new WebAssembly.Memory({ initial: 1601 })
const memData = new Uint32Array(memory.buffer)
const debug = console.log
const mod = await WebAssembly.instantiate(bin, {
  env: { 
    memory,
    debug,
  }
});
const inst = mod.instance;
// @typedef {Object} Forth
// @property {function(number): void} push
// @property {function(): number} pop
// @type {Forth}
const forth = inst.exports;

// Single-length signed number
const x = 42;
const y = 100;
const z = 142;
const n = -42;
const n0 = 0;
const n1 = 1;
const n2 = 2;
const n3 = 3;
const nmin = -2147483648;
const nmax = 2147483647;

describe("forth32.test.js", () => {
  const x = 42
  test("push(x)", () => {
    const oldSP = forth.sp()
    forth.push(x)
    const newSP = forth.sp()
    expect(newSP).toBe(oldSP - 4)
    expect(forth.at(newSP)).toBe(x)
  })
  test("pop()", () => {
    forth.push(x)
    const oldSP = forth.sp()
    const y = forth.pop()
    const newSP = forth.sp()
    expect(newSP).toBe(oldSP + 4)
    expect(y).toBe(x)
  })

  test("x DUP", () => {
    forth.push(x)
    const oldSP = forth.sp()
    forth.DUP()
    const newSP = forth.sp()
    expect(newSP).toBe(oldSP - 4)
    expect(forth.at(newSP)).toBe(x)
  })
  test("x y +", () => {
    const x = 42
    const y = 100
    const z = x + y
    forth.push(x)
    forth.push(y)
    const oldSP = forth.sp()
    forth.ADD()
    const newSP = forth.sp()
    expect(newSP).toBe(oldSP + 4)
    expect(forth.at(newSP)).toBe(z)
  })
  test("x y *", () => {
    const x = 42
    const y = 100
    const z = x * y
    forth.push(x)
    forth.push(y)
    const oldSP = forth.sp()
    forth.MUL()
    const newSP = forth.sp()
    expect(newSP).toBe(oldSP + 4)
    expect(forth.at(newSP)).toBe(z)
  })
})
