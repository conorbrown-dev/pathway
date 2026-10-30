import assert from "node:assert/strict";
import { test } from "node:test";
import { toggleNumericSign } from "../src/numericAnswer.ts";
import { waveAt, waveformPath } from "../src/wave/waveMath.ts";

test("sign toggle preserves digits, allows typing a negative first, and reverses reliably", () => {
  assert.equal(toggleNumericSign(""), "-");
  assert.equal(toggleNumericSign("-"), "");
  assert.equal(toggleNumericSign("12.5"), "-12.5");
  assert.equal(toggleNumericSign("-12.5"), "12.5");
  assert.equal(toggleNumericSign("−12.5"), "12.5");
  assert.equal(toggleNumericSign("0"), "-0");
});
test("wave amplitude, period, and phase have independent mathematical meanings", () => {
  assert.ok(Math.abs(waveAt(0, 4, 2, 90) - 4) < 1e-12);
  assert.ok(Math.abs(waveAt(0.5, 2, 2, 0) - 2) < 1e-12);
  assert.ok(Math.abs(waveAt(1, 2, 4, 0) - 2) < 1e-12);
  assert.ok(Math.abs(waveAt(0, 2, 2, -90) + 2) < 1e-12);
  assert.throws(() => waveAt(1, 2, 0, 0), /Period must be positive/);
  assert.match(waveformPath(2, 2, 0), /^M0,/);
});
