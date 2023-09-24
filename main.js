import { readFileSync } from "fs";
import * as WAForth from "./waforth.js";

const withLineBuffer = WAForth.withLineBuffer;
const forth = new WAForth.default();
forth.onEmit = withLineBuffer((c) => {
  process.stdout.write(c);
});

const src = readFileSync("./main.fs", "utf8");

await forth.load();

forth.interpret(src);

