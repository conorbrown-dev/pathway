// Pathway calculator engine.
//
// A small, dependency-free expression evaluator for math and engineering courses.
// It never uses eval(): input is tokenized, parsed into a tree, then evaluated.
// Alongside the number it returns two teaching aids:
//   - `interpretation`: the expression with grouping made explicit, so a learner can
//     see that "2 + 3 × 4" was read as "2 + (3 × 4)";
//   - `steps`: each operation in the order it was carried out ("3 × 4 = 12").

export type AngleMode = "deg" | "rad";

export type CalculatorOptions = {
  angleMode: AngleMode;
  /** Read SI prefixes after numbers (4.7k, 10µ, 2.2nF). Off for algebra, where "5m" is ambiguous. */
  siPrefixes: boolean;
  /** Value of `ans` (the previous result). */
  ans?: number;
  /** User-defined variables, e.g. { t: 10 }. */
  variables?: Record<string, number>;
};

export type CalculatorSuccess = {
  ok: true;
  value: number;
  /** Set when the input was an assignment such as "t = 10". */
  assigned?: string;
  interpretation: string;
  steps: string[];
};

export type CalculatorFailure = { ok: false; error: string; position?: number };

export type CalculatorResult = CalculatorSuccess | CalculatorFailure;

class CalculatorError extends Error {
  readonly position?: number;
  constructor(message: string, position?: number) {
    super(message);
    this.position = position;
  }
}

// ---------------------------------------------------------------------------
// Tokens

type Token =
  | { kind: "num"; value: number; text: string; pos: number }
  | { kind: "name"; name: string; pos: number }
  | { kind: "op"; op: string; pos: number }
  | { kind: "end"; pos: number };

const SI_PREFIXES: Record<string, { factor: number; name: string }> = {
  T: { factor: 1e12, name: "tera" },
  G: { factor: 1e9, name: "giga" },
  M: { factor: 1e6, name: "mega" },
  k: { factor: 1e3, name: "kilo" },
  m: { factor: 1e-3, name: "milli" },
  u: { factor: 1e-6, name: "micro" },
  "µ": { factor: 1e-6, name: "micro" },
  "μ": { factor: 1e-6, name: "micro" },
  n: { factor: 1e-9, name: "nano" },
  p: { factor: 1e-12, name: "pico" },
};

// Unit symbols that may follow a number for readability; they don't change the value.
const UNIT_SYMBOLS = ["Ω", "ohm", "ohms", "Hz", "V", "A", "W", "F", "H", "s"];

const NORMALIZE: Record<string, string> = {
  "×": "*",
  "·": "*",
  "⋅": "*",
  "÷": "/",
  "−": "-",
  "–": "-",
  "—": "-",
  "∕": "/",
};

const isDigit = (c: string) => c >= "0" && c <= "9";
const isLetter = (c: string) => /\p{L}/u.test(c) && c !== "π" && c !== "°" && c !== "µ" && c !== "μ";

function tokenize(source: string, siPrefixes: boolean): Token[] {
  const s = [...source].map((c) => NORMALIZE[c] ?? c).join("");
  const tokens: Token[] = [];
  // Tracks whether each open parenthesis belongs to a function call, so a comma can be
  // read as an argument separator there and as a thousands separator ("1,024") elsewhere.
  const parenStack: ("call" | "group")[] = [];
  let i = 0;

  while (i < s.length) {
    const c = s[i];
    if (c === " " || c === "\t" || c === "\n") {
      i += 1;
      continue;
    }

    if (isDigit(c) || (c === "." && isDigit(s[i + 1] ?? ""))) {
      const start = i;
      let digits = "";
      const insideCall = parenStack.at(-1) === "call";
      while (i < s.length) {
        const ch = s[i];
        if (isDigit(ch)) {
          digits += ch;
          i += 1;
        } else if (
          ch === "," &&
          !insideCall &&
          !digits.includes(".") &&
          /^\d{3}(?!\d)/.test(s.slice(i + 1))
        ) {
          i += 1; // thousands separator
        } else break;
      }
      if (s[i] === ".") {
        digits += ".";
        i += 1;
        while (i < s.length && isDigit(s[i])) {
          digits += s[i];
          i += 1;
        }
      }
      // Scientific notation: 6.02e23, 1E-6. A bare "e" afterwards is Euler's number.
      const exponent = /^[eE][+-]?\d+/.exec(s.slice(i));
      if (exponent) {
        digits += exponent[0];
        i += exponent[0].length;
      }
      let value = Number(digits);
      let text = digits;
      if (siPrefixes) {
        const letters = /^[\p{L}µμΩ]+/u.exec(s.slice(i))?.[0] ?? "";
        const suffix = readSiSuffix(letters);
        if (suffix) {
          value *= suffix.factor;
          text = `${digits}${suffix.prefix}`;
          i += suffix.length;
        }
      }
      if (Number.isNaN(value)) throw new CalculatorError(`"${digits}" isn't a number I can read.`, start);
      tokens.push({ kind: "num", value, text, pos: start });
      continue;
    }

    if (c === "π") {
      tokens.push({ kind: "name", name: "pi", pos: i });
      i += 1;
      continue;
    }

    if (isLetter(c)) {
      const start = i;
      let name = "";
      while (i < s.length && (isLetter(s[i]) || s[i] === "_")) {
        name += s[i];
        i += 1;
      }
      // Digits belong to a name only when they complete a function such as log2 or atan2,
      // so "sin30" still reads as sin(30) and "x2" as x × 2.
      const digits = /^\d+/.exec(s.slice(i))?.[0];
      if (digits && FUNCTIONS[(name + digits).toLowerCase()]) {
        name += digits;
        i += digits.length;
      }
      tokens.push({ kind: "name", name, pos: start });
      continue;
    }

    if (c === "|" && s[i + 1] === "|") {
      tokens.push({ kind: "op", op: "||", pos: i });
      i += 2;
      continue;
    }

    if (c === "*" && s[i + 1] === "*") {
      tokens.push({ kind: "op", op: "^", pos: i });
      i += 2;
      continue;
    }

    if ("+-*/^()!,=°²³√∛".includes(c)) {
      if (c === "(") {
        const prev = tokens.at(-1);
        const lower = prev?.kind === "name" ? prev.name.toLowerCase() : "";
        parenStack.push(lower in FUNCTIONS || lower in FUNCTION_ALIASES ? "call" : "group");
      } else if (c === ")") {
        parenStack.pop();
      }
      tokens.push({ kind: "op", op: c, pos: i });
      i += 1;
      continue;
    }

    throw new CalculatorError(`I don't recognise "${c}".`, i);
  }

  tokens.push({ kind: "end", pos: s.length });
  return tokens;
}

function readSiSuffix(letters: string): { prefix: string; factor: number; length: number } | null {
  if (!letters) return null;
  if (UNIT_SYMBOLS.includes(letters)) return { prefix: "", factor: 1, length: letters.length };
  const prefix = [...letters][0];
  const rest = letters.slice(prefix.length);
  const si = SI_PREFIXES[prefix];
  if (si && (rest === "" || UNIT_SYMBOLS.includes(rest))) {
    return { prefix, factor: si.factor, length: letters.length };
  }
  return null;
}

// ---------------------------------------------------------------------------
// Syntax tree

type Node =
  | { type: "num"; value: number; text: string }
  | { type: "name"; name: string }
  | { type: "neg"; arg: Node }
  | { type: "bin"; op: "+" | "-" | "*" | "/" | "^" | "||"; left: Node; right: Node; implicit?: boolean }
  | { type: "post"; op: "!" | "°"; arg: Node }
  | { type: "call"; fn: string; args: Node[] };

type FunctionSpec = {
  arity: [number, number];
  label: string;
  apply: (args: number[], options: CalculatorOptions) => number;
};

const toRadians = (x: number, mode: AngleMode) => (mode === "deg" ? (x * Math.PI) / 180 : x);
const fromRadians = (x: number, mode: AngleMode) => (mode === "deg" ? (x * 180) / Math.PI : x);

// Trig at exact multiples of 90° should give exact 0 / 1, not 6.1e-17.
const cleanTrig = (x: number) => (Math.abs(x) < 1e-15 ? 0 : x);

function requireDomain(condition: boolean, message: string) {
  if (!condition) throw new CalculatorError(message);
}

const FUNCTIONS: Record<string, FunctionSpec> = {
  sqrt: {
    arity: [1, 1],
    label: "√",
    apply: ([x]) => {
      requireDomain(x >= 0, "A square root of a negative number isn't a real number.");
      return Math.sqrt(x);
    },
  },
  cbrt: { arity: [1, 1], label: "∛", apply: ([x]) => Math.cbrt(x) },
  root: {
    arity: [2, 2],
    label: "root",
    apply: ([x, n]) => {
      requireDomain(n !== 0, "A zeroth root isn't defined.");
      if (x < 0) {
        requireDomain(Number.isInteger(n) && Math.abs(n) % 2 === 1, "An even root of a negative number isn't a real number.");
        return -Math.pow(-x, 1 / n);
      }
      return Math.pow(x, 1 / n);
    },
  },
  abs: { arity: [1, 1], label: "abs", apply: ([x]) => Math.abs(x) },
  sin: { arity: [1, 1], label: "sin", apply: ([x], o) => cleanTrig(Math.sin(toRadians(x, o.angleMode))) },
  cos: { arity: [1, 1], label: "cos", apply: ([x], o) => cleanTrig(Math.cos(toRadians(x, o.angleMode))) },
  tan: {
    arity: [1, 1],
    label: "tan",
    apply: ([x], o) => {
      const r = toRadians(x, o.angleMode);
      requireDomain(Math.abs(cleanTrig(Math.cos(r))) > 0, "tan is undefined here: the angle points straight up or down (cos = 0).");
      return cleanTrig(Math.tan(r));
    },
  },
  asin: {
    arity: [1, 1],
    label: "sin⁻¹",
    apply: ([x], o) => {
      requireDomain(x >= -1 && x <= 1, "sin⁻¹ only accepts values from −1 to 1 (a sine can't be bigger than 1).");
      return fromRadians(Math.asin(x), o.angleMode);
    },
  },
  acos: {
    arity: [1, 1],
    label: "cos⁻¹",
    apply: ([x], o) => {
      requireDomain(x >= -1 && x <= 1, "cos⁻¹ only accepts values from −1 to 1 (a cosine can't be bigger than 1).");
      return fromRadians(Math.acos(x), o.angleMode);
    },
  },
  atan: { arity: [1, 1], label: "tan⁻¹", apply: ([x], o) => fromRadians(Math.atan(x), o.angleMode) },
  atan2: { arity: [2, 2], label: "atan2", apply: ([y, x], o) => fromRadians(Math.atan2(y, x), o.angleMode) },
  ln: {
    arity: [1, 1],
    label: "ln",
    apply: ([x]) => {
      requireDomain(x > 0, "Logarithms only accept positive numbers.");
      return Math.log(x);
    },
  },
  log: {
    arity: [1, 2],
    label: "log",
    apply: ([x, base = 10]) => {
      requireDomain(x > 0, "Logarithms only accept positive numbers.");
      requireDomain(base > 0 && base !== 1, "A logarithm's base must be positive and not 1.");
      if (base === 10) return Math.log10(x);
      if (base === 2) return Math.log2(x);
      return Math.log(x) / Math.log(base);
    },
  },
  log2: {
    arity: [1, 1],
    label: "log₂",
    apply: ([x]) => {
      requireDomain(x > 0, "Logarithms only accept positive numbers.");
      return Math.log2(x);
    },
  },
  exp: { arity: [1, 1], label: "exp", apply: ([x]) => Math.exp(x) },
  floor: { arity: [1, 1], label: "floor", apply: ([x]) => Math.floor(x) },
  ceil: { arity: [1, 1], label: "ceil", apply: ([x]) => Math.ceil(x) },
  round: {
    arity: [1, 2],
    label: "round",
    apply: ([x, digits = 0]) => {
      requireDomain(Number.isInteger(digits) && digits >= 0 && digits <= 12, "round's second number is how many decimal places (0–12).");
      const factor = 10 ** digits;
      return Math.round(x * factor) / factor;
    },
  },
  min: { arity: [1, 32], label: "min", apply: (xs) => Math.min(...xs) },
  max: { arity: [1, 32], label: "max", apply: (xs) => Math.max(...xs) },
  hypot: { arity: [1, 32], label: "hypot", apply: (xs) => Math.hypot(...xs) },
  rad: { arity: [1, 1], label: "rad", apply: ([x]) => (x * Math.PI) / 180 },
  deg: { arity: [1, 1], label: "deg", apply: ([x]) => (x * 180) / Math.PI },
  par: { arity: [2, 32], label: "par", apply: (xs) => parallel(xs) },
};

const FUNCTION_ALIASES: Record<string, string> = { arcsin: "asin", arccos: "acos", arctan: "atan", lg: "log10" };

const CONSTANTS: Record<string, number> = { pi: Math.PI, e: Math.E, tau: Math.PI * 2 };

function parallel(values: number[]): number {
  // Resistors (or impedances) in parallel: 1 / (1/R1 + 1/R2 + ...).
  if (values.some((v) => v === 0)) return 0; // a short circuit dominates
  const sum = values.reduce((total, v) => total + 1 / v, 0);
  requireDomain(sum !== 0, "These values cancel out in parallel, so there's no finite result.");
  return 1 / sum;
}

export const isReservedName = (name: string) => {
  const lower = name.toLowerCase();
  return lower in FUNCTIONS || lower in FUNCTION_ALIASES || lower in CONSTANTS || lower === "ans";
};

// ---------------------------------------------------------------------------
// Parser (recursive descent)
//
// Precedence, loosest to tightest:
//   + -                 addition, subtraction
//   ||                  parallel combination (EE): R1 + R2 || R3 = R1 + (R2 || R3)
//   * /                 explicit multiplication and division, left to right
//   (implicit)          juxtaposition: 2π, 3(4 + 1), 2πfC. Binds tighter than / so 1/2πfC = 1/(2πfC)
//   unary - +           so -3^2 = -(3^2) = -9, as in written math
//   ^                   powers, right to left: 2^3^2 = 2^(3^2)
//   postfix ! ° ² ³

class Parser {
  private index = 0;
  private readonly tokens: Token[];
  constructor(tokens: Token[]) {
    this.tokens = tokens;
  }

  private peek(): Token {
    return this.tokens[this.index];
  }

  private next(): Token {
    const token = this.tokens[this.index];
    this.index += 1;
    return token;
  }

  private isOp(op: string, token = this.peek()) {
    return token.kind === "op" && token.op === op;
  }

  private expect(op: string, message: string) {
    if (!this.isOp(op)) throw new CalculatorError(message, this.peek().pos);
    this.next();
  }

  parseProgram(): { assign?: string; expr: Node } {
    const first = this.peek();
    const second = this.tokens[this.index + 1];
    let assign: string | undefined;
    if (first.kind === "name" && second && this.isOp("=", second)) {
      if (isReservedName(first.name)) {
        throw new CalculatorError(`"${first.name}" is a built-in name; pick another variable name.`, first.pos);
      }
      assign = first.name;
      this.index += 2;
    }
    if (this.peek().kind === "end") throw new CalculatorError(assign ? "Give the variable a value after =." : "Type an expression.");
    const expr = this.parseExpression();
    const rest = this.peek();
    if (rest.kind !== "end") {
      if (this.isOp(")")) throw new CalculatorError("There's a ) without a matching (.", rest.pos);
      if (this.isOp("=")) throw new CalculatorError("Use = only to name a value, like t = 10.", rest.pos);
      throw new CalculatorError("I couldn't read the expression from here.", rest.pos);
    }
    return { assign, expr };
  }

  private parseExpression(): Node {
    let left = this.parseParallel();
    while (this.isOp("+") || this.isOp("-")) {
      const op = (this.next() as { op: "+" | "-" }).op;
      left = { type: "bin", op, left, right: this.parseParallel() };
    }
    return left;
  }

  private parseParallel(): Node {
    let left = this.parseMultiplicative();
    while (this.isOp("||")) {
      this.next();
      left = { type: "bin", op: "||", left, right: this.parseMultiplicative() };
    }
    return left;
  }

  private parseMultiplicative(): Node {
    let left = this.parseImplicit();
    while (this.isOp("*") || this.isOp("/")) {
      const op = (this.next() as { op: "*" | "/" }).op;
      left = { type: "bin", op, left, right: this.parseImplicit() };
    }
    return left;
  }

  private startsImplicitOperand(token: Token) {
    return token.kind === "num" || token.kind === "name" || this.isOp("(", token) || this.isOp("√", token) || this.isOp("∛", token);
  }

  private parseImplicit(): Node {
    let left = this.parseUnary();
    while (this.startsImplicitOperand(this.peek())) {
      // A number directly after a number ("2 3") is almost always a typo, not a product.
      if (this.peek().kind === "num" && endsWithNumber(left)) {
        throw new CalculatorError("Two numbers sit side by side. Add an operator between them.", this.peek().pos);
      }
      left = { type: "bin", op: "*", left, right: this.parsePower(), implicit: true };
    }
    return left;
  }

  private parseUnary(): Node {
    if (this.isOp("-")) {
      this.next();
      return { type: "neg", arg: this.parseUnary() };
    }
    if (this.isOp("+")) {
      this.next();
      return this.parseUnary();
    }
    return this.parsePower();
  }

  private parsePower(): Node {
    const base = this.parsePostfix();
    if (this.isOp("^")) {
      this.next();
      // Exponents may carry their own sign: 2^-1. Right-associative via parseUnary -> parsePower.
      return { type: "bin", op: "^", left: base, right: this.parseUnary() };
    }
    return base;
  }

  private parsePostfix(): Node {
    let node = this.parsePrimary();
    for (;;) {
      if (this.isOp("!")) {
        this.next();
        node = { type: "post", op: "!", arg: node };
      } else if (this.isOp("°")) {
        this.next();
        node = { type: "post", op: "°", arg: node };
      } else if (this.isOp("²") || this.isOp("³")) {
        const op = (this.next() as { op: string }).op;
        node = { type: "bin", op: "^", left: node, right: { type: "num", value: op === "²" ? 2 : 3, text: op === "²" ? "2" : "3" } };
      } else return node;
    }
  }

  private parsePrimary(): Node {
    const token = this.peek();
    if (token.kind === "num") {
      this.next();
      return { type: "num", value: token.value, text: token.text };
    }
    if (this.isOp("(")) {
      this.next();
      if (this.isOp(")")) throw new CalculatorError("These parentheses are empty.", token.pos);
      const inner = this.parseExpression();
      this.expect(")", "A ( is missing its closing ).");
      return inner;
    }
    if (this.isOp("√") || this.isOp("∛")) {
      const op = (this.next() as { op: string }).op;
      return { type: "call", fn: op === "√" ? "sqrt" : "cbrt", args: [this.parsePower()] };
    }
    if (token.kind === "name") {
      this.next();
      const lower = token.name.toLowerCase();
      const fnName = FUNCTION_ALIASES[lower] === "log10" ? "log" : (FUNCTION_ALIASES[lower] ?? lower);
      const fn = FUNCTIONS[fnName];
      if (fn) {
        if (!this.isOp("(")) {
          // "sin 30" — allow one operand without parentheses, like most calculators.
          if (this.peek().kind === "end" || this.peek().kind === "op") {
            throw new CalculatorError(`${token.name} needs a value in parentheses, like ${token.name}(30).`, token.pos);
          }
          return { type: "call", fn: fnName, args: [this.parsePower()] };
        }
        this.next();
        const args: Node[] = [];
        if (!this.isOp(")")) {
          args.push(this.parseExpression());
          while (this.isOp(",")) {
            this.next();
            args.push(this.parseExpression());
          }
        }
        this.expect(")", `${token.name}( is missing its closing ).`);
        const [lo, hi] = fn.arity;
        if (args.length < lo || args.length > hi) {
          const expected = lo === hi ? `${lo} value${lo === 1 ? "" : "s"}` : hi > 3 ? `at least ${lo}` : `${lo} or ${hi} values`;
          throw new CalculatorError(`${token.name} takes ${expected}, separated by commas.`, token.pos);
        }
        return { type: "call", fn: fnName, args };
      }
      return { type: "name", name: token.name };
    }
    if (token.kind === "end") throw new CalculatorError("The expression ends too early. Something is missing after the last operator.", token.pos);
    if (this.isOp(")")) throw new CalculatorError("There's a ) without a matching (.", token.pos);
    throw new CalculatorError(`I wasn't expecting "${(token as { op: string }).op}" here.`, token.pos);
  }
}

function endsWithNumber(node: Node): boolean {
  if (node.type === "num") return true;
  if (node.type === "bin" && node.implicit) return endsWithNumber(node.right);
  return false;
}

// ---------------------------------------------------------------------------
// Printing: makes grouping explicit

const OP_SYMBOL: Record<string, string> = { "+": " + ", "-": " − ", "*": " × ", "/": " ÷ ", "^": "^", "||": " ∥ " };

function show(node: Node, options: CalculatorOptions): string {
  switch (node.type) {
    case "num":
      return node.text;
    case "name":
      return node.name === "pi" ? "π" : node.name;
    case "neg":
      return `−${wrap(node.arg, options, isAtomic)}`;
    case "post":
      return `${wrap(node.arg, options, isAtomic)}${node.op}`;
    case "call": {
      const label = FUNCTIONS[node.fn]?.label ?? node.fn;
      if (label === "√" || label === "∛") return `${label}(${show(node.args[0], options)})`;
      return `${label}(${node.args.map((a) => show(a, options)).join(", ")})`;
    }
    case "bin": {
      // Make grouping explicit: a child that is itself an operation gets brackets, except a
      // left-hand chain at the same level (1 − 2 + 3, 2 × π × f), which already reads left to right.
      const left = wrap(node.left, options, (n) => isAtomic(n) || (node.op !== "^" && n.type === "bin" && LEVEL[n.op] === LEVEL[node.op]));
      const right = wrap(node.right, options, isAtomic);
      return `${left}${OP_SYMBOL[node.op]}${right}`;
    }
  }
}

const LEVEL: Record<string, number> = { "+": 1, "-": 1, "||": 2, "*": 3, "/": 3, "^": 4 };
const isAtomic = (n: Node) => n.type === "num" || n.type === "name" || n.type === "call";

function wrap(node: Node, options: CalculatorOptions, bare: (n: Node) => boolean) {
  const text = show(node, options);
  return bare(node) ? text : `(${text})`;
}

// ---------------------------------------------------------------------------
// Evaluation with a record of each step

function factorial(n: number): number {
  requireDomain(Number.isInteger(n) && n >= 0, "Factorial (!) works on whole numbers 0 and up.");
  requireDomain(n <= 170, "That factorial is too large to represent.");
  let result = 1;
  for (let k = 2; k <= n; k += 1) result *= k;
  return result;
}

function evaluate(node: Node, options: CalculatorOptions, steps: string[]): number {
  switch (node.type) {
    case "num":
      return node.value;
    case "name": {
      const lower = node.name.toLowerCase();
      if (lower === "ans") {
        if (options.ans === undefined) throw new CalculatorError("There's no previous answer yet.");
        return options.ans;
      }
      if (lower in CONSTANTS) return CONSTANTS[lower];
      const variables = options.variables ?? {};
      if (Object.hasOwn(variables, node.name)) return variables[node.name];
      throw new CalculatorError(
        node.name.length > 1
          ? `"${node.name}" isn't defined. To multiply letters write them with * (a*b); to make a variable, write ${node.name} = 5 first.`
          : node.name.toLowerCase() === "x"
            ? `To multiply, use * or × rather than the letter x. To use x as a variable, define it first, like x = 5.`
            : `"${node.name}" has no value yet. Define it first, like ${node.name} = 5.`,
      );
    }
    case "neg":
      return -evaluate(node.arg, options, steps);
    case "post": {
      const x = evaluate(node.arg, options, steps);
      if (node.op === "!") {
        const result = factorial(x);
        steps.push(`${fmt(x)}! = ${fmt(result)}`);
        return result;
      }
      if (options.angleMode === "deg") return x;
      const result = (x * Math.PI) / 180;
      steps.push(`${fmt(x)}° in radians = ${fmt(result)}`);
      return result;
    }
    case "call": {
      const fn = FUNCTIONS[node.fn];
      const args = node.args.map((a) => evaluate(a, options, steps));
      const result = fn.apply(args, options);
      checkFinite(result);
      const angleNote = ["sin", "cos", "tan"].includes(node.fn) ? (options.angleMode === "deg" ? "°" : " rad") : "";
      const shown = fn.label === "√" || fn.label === "∛" ? `${fn.label}${fmt(args[0])}` : `${fn.label}(${args.map(fmt).join(", ")}${angleNote})`;
      const unitNote = ["asin", "acos", "atan", "atan2"].includes(node.fn) ? (options.angleMode === "deg" ? "°" : " rad") : "";
      steps.push(`${shown} = ${fmt(result)}${unitNote}`);
      return result;
    }
    case "bin": {
      const a = evaluate(node.left, options, steps);
      const b = evaluate(node.right, options, steps);
      let result: number;
      switch (node.op) {
        case "+":
          result = a + b;
          break;
        case "-":
          result = a - b;
          break;
        case "*":
          result = a * b;
          break;
        case "/":
          requireDomain(b !== 0, "Division by zero isn't defined.");
          result = a / b;
          break;
        case "^":
          if (a === 0 && b < 0) throw new CalculatorError("0 to a negative power would divide by zero.");
          if (a < 0 && !Number.isInteger(b)) {
            // Odd roots of negatives are real: (−8)^(1/3) = −2.
            const reciprocal = 1 / b;
            if (Number.isInteger(Math.round(reciprocal)) && Math.abs(reciprocal - Math.round(reciprocal)) < 1e-9 && Math.round(reciprocal) % 2 !== 0) {
              result = -Math.pow(-a, b);
            } else {
              throw new CalculatorError("A negative number to a fractional power isn't a real number.");
            }
          } else {
            result = Math.pow(a, b);
          }
          break;
        case "||":
          result = parallel([a, b]);
          break;
      }
      checkFinite(result);
      steps.push(`${fmt(a)}${OP_SYMBOL[node.op]}${fmt(b)} = ${fmt(result)}`);
      return result;
    }
  }
}

function checkFinite(x: number) {
  if (!Number.isFinite(x)) throw new CalculatorError("The result is too large to represent.");
}

// ---------------------------------------------------------------------------
// Formatting

/** Rounds away binary floating-point noise: 0.1 + 0.2 shows as 0.3. */
export function clean(x: number): number {
  if (x === 0 || !Number.isFinite(x)) return x === 0 ? 0 : x;
  return Number.parseFloat(x.toPrecision(12));
}

/** Human-readable number with thousands separators; scientific notation for extreme sizes. */
export function fmt(x: number): string {
  const v = clean(x);
  if (v === 0) return "0";
  const abs = Math.abs(v);
  if (abs >= 1e12 || abs < 1e-6) {
    const [mantissa, exponent] = v.toExponential().split("e");
    const trimmed = Number.parseFloat(Number(mantissa).toPrecision(10)).toString();
    return `${trimmed.replace("-", "−")} × 10^${Number(exponent)}`;
  }
  const [whole, fraction] = v.toString().split(".");
  const grouped = whole.replace("-", "").replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  return `${v < 0 ? "−" : ""}${grouped}${fraction ? `.${fraction}` : ""}`;
}

/** Plain machine form for pasting into an answer box: no separators, ASCII minus. */
export function plain(x: number): string {
  return String(clean(x));
}

const ENGINEERING_PREFIXES: [number, string][] = [
  [12, "T"],
  [9, "G"],
  [6, "M"],
  [3, "k"],
  [0, ""],
  [-3, "m"],
  [-6, "µ"],
  [-9, "n"],
  [-12, "p"],
];

/** Engineering notation with an SI prefix, e.g. 0.0047 → "4.7 m". Returns null when no prefix applies. */
export function engineering(x: number): string | null {
  const v = clean(x);
  if (v === 0) return null;
  const exponent = Math.floor(Math.log10(Math.abs(v)) / 3) * 3;
  const match = ENGINEERING_PREFIXES.find(([e]) => e === exponent);
  if (!match || match[0] === 0) return null;
  const scaled = Number.parseFloat((v / 10 ** match[0]).toPrecision(6));
  return `${scaled.toString().replace("-", "−")} ${match[1]}`;
}

// ---------------------------------------------------------------------------
// Public entry point

export function calculate(input: string, options: CalculatorOptions): CalculatorResult {
  try {
    const tokens = tokenize(input, options.siPrefixes);
    const { assign, expr } = new Parser(tokens).parseProgram();
    const steps: string[] = [];
    const value = evaluate(expr, options, steps);
    checkFinite(value);
    return {
      ok: true,
      value: clean(value),
      assigned: assign,
      interpretation: `${assign ? `${assign} = ` : ""}${show(expr, options)}`,
      steps,
    };
  } catch (error) {
    if (error instanceof CalculatorError) return { ok: false, error: error.message, position: error.position };
    return { ok: false, error: "Something went wrong reading that expression." };
  }
}
