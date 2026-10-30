/** Toggle a signed numeric value without removing the learner's existing digits. */
export function toggleNumericSign(value: string): string {
  const v = value.trim();
  if (v.startsWith("-") || v.startsWith("−")) return v.slice(1);
  return `-${v}`;
}
