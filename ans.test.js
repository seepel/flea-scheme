// @ts-check
import fs from "fs";
import process from "process";
import { forth } from "./node-shell.js";

process.chdir("./vendor/forth2012-test-suite/src");
console.log(process.cwd());
const src = fs.readFileSync("./runtests.fth", "utf8");
forth.interpret(src, true);

