# tests: 4
def main():
    ns = load()
    score, paired, sign_p, summarize = (need(ns, n) for n in ("score_output", "paired_counts", "sign_test_p", "summarize"))

    check(call(score, "fix(auth): refresh tokens", "  fix(auth): refresh tokens\n", "exact") == 1.0, "Exact mode should ignore surrounding whitespace.")
    check(call(score, "fix: a", "Fix: a", "exact") == 0.0, "Exact mode is case-sensitive.")
    expected = {"label": "billing", "severity": 2}
    check(call(score, expected, '{"severity": 2, "label": "billing", "note": "extra"}', "json") == 1.0, "JSON mode should accept extra keys as long as every expected key matches.")
    check(call(score, expected, '{"label": "billing", "severity": "2"}', "json") == 0.0, "JSON mode should compare values exactly: \"2\" is not 2.")
    check(call(score, expected, 'label: billing', "json") == 0.0, "Output that isn't JSON scores 0 in JSON mode, without crashing.")
    check(call(score, expected, '["billing", 2]', "json") == 0.0, "JSON that isn't an object scores 0.")
    test("exact and JSON scoring")

    a = [1, 1, 0, 0, 1, 0]
    b = [1, 0, 1, 0, 0, 0]
    check(tuple(call(paired, a, b)) == (2, 1, 3), f"paired_counts should be (2 wins, 1 loss, 3 ties) here; got {show(call(paired, a, b))}.")
    test("paired wins, losses and ties")

    check(close(call(sign_p, 18, 3), 2 * (1 + 21 + 210 + 1330) / 2 ** 21, 1e-12), "sign_test_p(18, 3) should be about 0.0015.")
    check(close(call(sign_p, 10, 10), 1.0), "Equal wins and losses give p = 1.0 (capped).")
    check(close(call(sign_p, 9, 11), 2 * sum(math.comb(20, j) for j in range(10)) / 2 ** 20, 1e-12), "sign_test_p(9, 11) should be about 0.82.")
    check(call(sign_p, 0, 0) == 1.0, "With no non-tied items there's no evidence either way: p = 1.0.")
    check(close(call(sign_p, 6, 0), 2 / 64, 1e-12), "sign_test_p(6, 0) should be 2 × (1/64).")
    test("exact sign test")

    acc, low, high = call(summarize, [1.0] * 66 + [0.0] * 14)
    p = 66 / 80
    m = 1.96 * math.sqrt(p * (1 - p) / 80)
    check(close(acc, p) and close(low, p - m, 1e-9) and close(high, p + m, 1e-9), f"summarize should give accuracy 0.825 with a 95% interval; got {show((acc, low, high))}.")
    check(call(summarize, [1.0] * 5)[2] <= 1.0, "The interval must be clipped to [0, 1].")
    test("accuracy with an interval")


run(main)
