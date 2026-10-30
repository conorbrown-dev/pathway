import assert from "node:assert/strict";
import { test } from "node:test";
import { calculate, engineering, fmt, plain, type CalculatorOptions } from "../src/calculator/engine.ts";

const math: CalculatorOptions = { angleMode: "deg", siPrefixes: false };
const ee: CalculatorOptions = { angleMode: "deg", siPrefixes: true };

function value(input: string, options: CalculatorOptions = math): number {
  const result = calculate(input, options);
  if (!result.ok) assert.fail(`${input}: ${result.error}`);
  return result.value;
}

function error(input: string, options: CalculatorOptions = math): string {
  const result = calculate(input, options);
  if (result.ok) assert.fail(`${input} should fail but gave ${result.value}`);
  return result.error;
}

test("order of operations matches written math", () => {
  assert.equal(value("2 + 3 * 4"), 14);
  assert.equal(value("(25 + 15) × 2 − 8"), 72);
  assert.equal(value("-3^2"), -9);
  assert.equal(value("(-3)^2"), 9);
  assert.equal(value("2^3^2"), 512);
  assert.equal(value("2^-1"), 0.5);
  assert.equal(value("10 - 4 - 3"), 3);
  assert.equal(value("100 / 10 / 5"), 2);
  assert.equal(value("−350 − (−90)"), -260);
});

test("implicit multiplication binds tighter than division", () => {
  assert.equal(value("2(3 + 4)"), 14);
  assert.equal(value("(1+2)(3+4)"), 21);
  assert.ok(Math.abs(value("2π") - 2 * Math.PI) < 1e-12);
  // Capacitive reactance is written 1/2πfC and means 1/(2πfC).
  const xc = value("1/2π f c", { ...math, variables: { f: 60, c: 0.0001 } });
  assert.ok(Math.abs(xc - 1 / (2 * Math.PI * 60 * 0.0001)) < 1e-9);
  assert.match(error("2 3"), /side by side/);
});

test("powers, roots and unicode from lesson text", () => {
  assert.equal(value("12² + 5²"), 169);
  assert.equal(value("√169"), 13);
  assert.equal(value("√(12^2 + 5^2)"), 13);
  assert.equal(value("sqrt(144) + cbrt(27)"), 15);
  assert.equal(value("(-8)^(1/3)"), -2);
  assert.equal(value("root(81, 4)"), 3);
  assert.equal(value("5!"), 120);
  assert.equal(value("2 ** 10"), 1024);
});

test("precalculus course answers", () => {
  assert.equal(Math.round(value("2000(1.06)^10")), 3582);
  assert.equal(value("log2(1024)"), 10);
  assert.equal(value("log(1024, 2)"), 10);
  assert.equal(value("log(1000)"), 3);
  assert.ok(Math.abs(value("ln(4) / ln(1.15)") - 9.919) < 0.001);
  assert.equal(value("100 × 1.5^5"), 759.375);
  assert.equal(value("12 + 0.09 × 400"), 48);
  assert.equal(value("1.8 × 35 + 32"), 95);
  assert.equal(value("(300 + 2 × 50) / 50"), 8);
});

test("thousands separators outside function calls, argument separators inside", () => {
  assert.equal(value("1,024 / 2"), 512);
  assert.equal(value("max(1,000)"), 1); // inside a call, the comma separates arguments
  assert.equal(value("max(2, 7, 3)"), 7);
});

test("angles follow the mode, and ° always means degrees", () => {
  assert.equal(value("sin(30)"), 0.5);
  assert.equal(value("cos(90)"), 0);
  assert.equal(value("sin 30"), 0.5);
  assert.equal(value("sin30"), 0.5);
  assert.ok(Math.abs(value("20 sin(70)") - 18.794) < 0.001);
  assert.ok(Math.abs(value("50 + 10cos(30)") - 58.660) < 0.001);
  const rad: CalculatorOptions = { angleMode: "rad", siPrefixes: false };
  assert.equal(value("sin(pi/2)", rad), 1);
  assert.equal(value("sin(30°)", rad), 0.5);
  assert.ok(Math.abs(value("135°", rad) - 2.35619) < 1e-5);
  assert.ok(Math.abs(value("rad(135)") - 2.35619) < 1e-5);
  assert.equal(value("deg(pi)"), 180);
  assert.equal(value("asin(0.5)"), 30);
  assert.equal(value("atan2(1, 1)"), 45);
  assert.match(error("tan(90)"), /undefined/);
  assert.match(error("asin(2)"), /−1 to 1/);
});

test("SI prefixes and parallel resistance for electrical engineering", () => {
  assert.equal(value("4.7k", ee), 4700);
  assert.equal(value("10µ", ee), 0.00001);
  assert.equal(value("10u", ee), 0.00001);
  assert.equal(value("2.2nF", ee), 2.2e-9);
  assert.equal(value("20mA × 220Ω", ee), 4.4);
  assert.equal(value("1M", ee), 1e6);
  assert.equal(value("5V / 1kΩ", ee), 0.005);
  assert.equal(value("10k || 10k", ee), 5000);
  assert.equal(value("par(30, 60, 20)", ee), 10);
  assert.equal(value("100 + 10k || 10k", ee), 5100); // series + parallel, as written in EE
  assert.ok(Math.abs(value("2pi", ee) - 2 * Math.PI) < 1e-12);
  // Without SI prefixes, "5m" is not silently 0.005.
  assert.match(error("5m"), /no value yet/);
});

test("ans and variables", () => {
  assert.equal(value("ans × 2", { ...math, ans: 21 }), 42);
  const assigned = calculate("t = 10", math);
  assert.ok(assigned.ok && assigned.assigned === "t" && assigned.value === 10);
  assert.equal(value("400(1.15)^t", { ...math, variables: { t: 2 } }), 529);
  assert.match(error("sin = 3"), /built-in/);
  assert.match(error("ans + 1"), /no previous answer/);
});

test("interpretation makes grouping explicit", () => {
  const show = (input: string, options = math) => {
    const result = calculate(input, options);
    assert.ok(result.ok);
    return result.interpretation;
  };
  assert.equal(show("2 + 3 * 4"), "2 + (3 × 4)");
  assert.equal(show("-3^2"), "−(3^2)");
  assert.equal(show("1 - 2 + 3"), "1 − 2 + 3");
  assert.equal(show("1 - (2 + 3)"), "1 − (2 + 3)");
  assert.equal(show("1/2π f", { ...math, variables: { f: 1 } }), "1 ÷ (2 × π × f)");
  assert.equal(show("12² + 5²"), "(12^2) + (5^2)");
});

test("steps follow evaluation order", () => {
  const result = calculate("(25 + 15) × 2 − 8", math);
  assert.ok(result.ok);
  assert.deepEqual(result.steps, ["25 + 15 = 40", "40 × 2 = 80", "80 − 8 = 72"]);
});

test("helpful errors", () => {
  assert.match(error("1/0"), /Division by zero/);
  assert.match(error("(1 + 2"), /closing/);
  assert.match(error("1 + 2)"), /without a matching/);
  assert.match(error("3 x 4"), /use \* or ×/);
  assert.match(error("2 +"), /ends too early/);
  assert.match(error("sqrt(-4)"), /negative/);
  assert.match(error("log(0)"), /positive/);
  assert.match(error("(-8)^(1/2)"), /fractional power/);
  assert.match(error("()"), /empty/);
  assert.match(error(""), /Type an expression/);
});

test("formatting hides floating-point noise", () => {
  assert.equal(value("0.1 + 0.2"), 0.3);
  assert.equal(fmt(1234567.5), "1,234,567.5");
  assert.equal(fmt(-0.25), "−0.25");
  assert.equal(fmt(6.02e23), "6.02 × 10^23");
  assert.equal(plain(-1234.5), "-1234.5");
  assert.equal(engineering(0.0047), "4.7 m");
  assert.equal(engineering(4700), "4.7 k");
  assert.equal(engineering(12), null);
});
