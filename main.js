// @ts-check
import { readFileSync } from "fs";
import { forth } from "./node-shell.js";

const src = readFileSync("./main.fs", "utf8");

forth.interpret(src);

