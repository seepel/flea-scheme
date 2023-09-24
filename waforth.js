import { readFileSync } from "fs";
const wasmModule = readFileSync("./waforth.wasm");

const isSafari =
  typeof navigator != "undefined" &&
  /^((?!chrome|android).)*safari/i.test(navigator.userAgent);

const PAD_OFFSET = 400;

// eslint-disable-next-line no-unused-vars, @typescript-eslint/no-unused-vars
const arrayToBase64 =
  typeof Buffer === "undefined"
    ? function arrayToBase64(bytes) {
        let binary = "";
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
      }
    : function arrayToBase64(s) {
        return Buffer.from(s).toString("base64");
      };

function loadString(memory, addr, len) {
  return new TextDecoder().decode(new Uint8Array(memory.buffer, addr, len));
}

function saveString(s, memory, addr) {
  const encoded = new TextEncoder().encode(s);
  const len = encoded.length;
  const a = new Uint8Array(memory.buffer, addr, len);
  for (let i = 0; i < len; ++i) {
    a[i] = encoded[i];
  }
  return len;
}

/**
 * Creates a function that accepts character codes in UTF-8 encoding, and calls
 * the callback whenever a complete character is received.
 */
export function withCharacterBuffer(fn) {
  let pending = 0;
  let buffer = [];
  const decoder = new TextDecoder();
  return (c) => {
    if (pending > 0) {
      buffer.push(c);
      pending -= 1;
      if (pending == 0) {
        fn(decoder.decode(Uint8Array.from(buffer)));
        buffer = [];
      }
    } else {
      if ((c & 0x80) === 0) {
        fn(String.fromCharCode(c));
      } else {
        buffer = [c];
        if ((c & 0xe0) === 0xc0) {
          pending = 1;
        } else if ((c & 0xf0) == 0xe0) {
          pending = 2;
        } else if ((c & 0xf8) == 0xf0) {
          pending = 3;
        } else if ((c & 0xfc) === 0xf8) {
          pending = 4;
        } else if ((c & 0xfe) == 0xfc) {
          pending = 5;
        }
      }
    }
  };
}

/**
 * Creates a function that accepts character codes in UTF-8 encoding, and calls
 * the callback whenever a complete newline-delimited line is received.
 *
 * The resulting function also has a `flush()` function to flush any remaining output.
 */
export function withLineBuffer(fn) {
  let buffer = [];
  const flush = () => {
    if (buffer.length > 0) {
      fn(new TextDecoder().decode(Uint8Array.from(buffer)));
      buffer = [];
    }
  };
  const r = (c) => {
    buffer.push(c);
    if (c == 0xa) {
      flush();
    }
  };
  r.flush = flush;
  return r;
}

export var ErrorCode;
(function (ErrorCode) {
  ErrorCode[(ErrorCode["Unknown"] = 1)] = "Unknown";
  ErrorCode[(ErrorCode["Quit"] = 2)] = "Quit";
  ErrorCode[(ErrorCode["Abort"] = 3)] = "Abort";
  ErrorCode[(ErrorCode["EOI"] = 4)] = "EOI";
  ErrorCode[(ErrorCode["Bye"] = 5)] = "Bye";
})(ErrorCode || (ErrorCode = {}));

export function isSuccess(code) {
  return code !== ErrorCode.Abort && code !== ErrorCode.Unknown;
}

/**
 * JavaScript shell around the WAForth WebAssembly module.
 *
 * Provides higher-level functions to interact with the WAForth WebAssembly module.
 *
 * To the WebAssembly module, provides the infrastructure to dynamically load WebAssembly modules and
 * the I/O primitives with the UI.
 * */
class WAForth {
  // core?: WebAssembly.Instance;
  #buffer;
  #fns;

  /**
   * Callback that is called when a character needs to be emitted.
   *
   * `c` is the single-character string that is emitted
   */
  // onEmit?: (c: number) => void;
  // key: () => number;

  constructor() {
    this.#fns = {};
    this.onEmit = withLineBuffer(console.log);

    const keyBuffer = [];
    this.key = () => {
      while (keyBuffer.length === 0) {
        const c = window.prompt("Enter text");
        if (c == null) {
          continue;
        }
        keyBuffer.push(...c.split(""));
        if (c.length === 0 || c.length > 1) {
          keyBuffer.push("\n");
        }
      }
      return keyBuffer.shift().charCodeAt(0);
    };
  }

  /**
   * Initialize WAForth.
   *
   * Needs to be called before interpret().
   */
  async load() {
    this.#buffer = "";
    const memory = new WebAssembly.Memory({ initial: 1601, maximum: 65535 });

    const instance = await WebAssembly.instantiate(wasmModule, {
      env: { memory },
      shell: {
        ////////////////////////////////////////
        // I/O
        ////////////////////////////////////////

        emit: (c) => {
          if (this.onEmit) {
            this.onEmit(c);
          }
        },

        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        read: (addr, length) => {
          let input;
          const i = this.#buffer.indexOf("\n");
          if (i === -1) {
            input = this.#buffer;
            this.#buffer = "";
          } else {
            input = this.#buffer.substring(0, i + 1);
            this.#buffer = this.#buffer.substring(i + 1);
          }
          // console.log("read: %s (%d remaining)", input, this.#buffer.length);
          return saveString(input, this.core.exports.memory, addr);
        },

        key: () => {
          return this.key();
        },

        ////////////////////////////////////////
        // Loader
        ////////////////////////////////////////

        load: (offset, length) => {
          let data = new Uint8Array(
            this.core.exports.memory.buffer,
            offset,
            length
          );
          if (isSafari) {
            // On Safari, using the original Uint8Array triggers a bug.
            // Taking an element-by-element copy of the data first.
            const dataCopy = [];
            for (let i = 0; i < length; ++i) {
              dataCopy.push(data[i]);
            }
            data = new Uint8Array(dataCopy);
          }
          // console.log("Load", arrayToBase64(data));
          try {
            const module = new WebAssembly.Module(data);
            new WebAssembly.Instance(module, {
              env: { table, memory },
            });
          } catch (e) {
            console.error(e);
            throw e;
          }
        },

        ////////////////////////////////////////
        // Generic call
        ////////////////////////////////////////

        call: () => {
          const len = this.pop();
          const addr = this.pop();
          const fname = loadString(memory, addr, len);
          const fn = this.#fns[fname];
          if (!fn) {
            console.error("Unbound SCALL: %s", fname);
          } else {
            fn(this);
          }
        },
      },
    });
    this.core = instance.instance;
    const table = this.core.exports.table;
    return this;
  }

  memory() {
    return this.core.exports.memory;
  }

  here() {
    return this.core.exports.here();
  }

  pop() {
    return this.core.exports.pop();
  }

  popString() {
    const len = this.pop();
    const addr = this.pop();
    return loadString(this.memory(), addr, len);
  }

  push(n) {
    this.core.exports.push(n);
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  pushString(s, offset = 0) {
    const addr = this.here() + PAD_OFFSET;
    const len = saveString(s, this.memory(), addr);
    this.push(addr);
    this.push(len);
    return addr + PAD_OFFSET;
  }

  /**
   * Read data `s` into the input buffer without interpreting it.
   */
  read(s) {
    this.#buffer = this.#buffer + s;
  }

  /**
   * Read data `s` into the input buffer, and start interpreter.
   */
  interpret(s, silent = false) {
    if (!s.endsWith("\n")) {
      s = s + "\n";
    }
    this.read(s);
    try {
      return this.core.exports.run(silent);
    } catch (e) {
      // Exceptions thrown from the core means QUIT or ABORT is called, or an error
      // has occurred.
      if (this.core.exports.error() === ErrorCode.Unknown) {
        console.error(e);
      }
    }
    return this.core.exports.error();
  }

  /**
   * Bind `name` to SCALL in Forth.
   *
   * When an SCALL is done with `name` on the top of the stack, `fn` will be called (with the name popped off the stack).
   * Use `stack` to pop parameters off the stack, and push results back on the stack.
   */
  bind(name, fn) {
    this.#fns[name] = fn;
  }

  /**
   * Bind async `name` to SCALL in Forth.
   *
   * When an SCALL is done with `name` on the top of the stack, `fn` will be called (with the name popped off the stack).
   * Expects an execution token on the top of the stack, which will be called when the async callback is finished.
   * The execution parameter will be called with the success flag set.
   */
  bindAsync(name, fn) {
    this.#fns[name] = async () => {
      const cbxt = this.pop();
      try {
        await fn(this);
        this.push(-1);
      } catch (e) {
        console.error(e);
        this.push(0);
      } finally {
        this.push(cbxt);
        this.interpret("EXECUTE");
      }
    };
  }
}

export default WAForth;
