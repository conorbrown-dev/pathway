/** Angles are entered in degrees; time and period use the same time unit. */
export function waveAt(time: number, amplitude: number, period: number, phaseDegrees: number): number {
  if (!Number.isFinite(period) || period <= 0) throw new RangeError("Period must be positive.");
  return amplitude * Math.sin((2 * Math.PI * time) / period + (phaseDegrees * Math.PI) / 180);
}
export function waveformPath(
  amplitude: number, period: number, phaseDegrees: number,
  width = 640, height = 240, timeSpan = 8, maxAmplitude = 5,
): string {
  return Array.from({ length: width + 1 }, (_, px) => {
    const time = (px / width) * timeSpan;
    const y = height / 2 - (waveAt(time, amplitude, period, phaseDegrees) / maxAmplitude) * (height / 2 - 16);
    return `${px === 0 ? "M" : "L"}${px},${y.toFixed(2)}`;
  }).join(" ");
}
