# tests: 4
def comb_pass_at_k(n, c, k):
    if n - c < k:
        return 1.0
    return 1.0 - math.comb(n - c, k) / math.comb(n, k)


def main():
    ns = load()
    pass_at_k, acc_ci, overlap, consistency = (need(ns, n) for n in ("pass_at_k", "accuracy_with_interval", "ngram_overlap", "judge_consistency"))

    check(close(call(pass_at_k, 10, 3, 1), 0.3, 1e-9), "pass_at_k(10, 3, 1) should be 0.3.")
    check(close(call(pass_at_k, 10, 3, 5), 1 - 21 / 252, 1e-9), "pass_at_k(10, 3, 5) should be 1 - 21/252 ≈ 0.917.")
    check(call(pass_at_k, 10, 0, 5) == 0.0, "With no passing samples, pass@k is 0.")
    check(close(call(pass_at_k, 10, 8, 5), 1.0), "With only 2 failures, any 5 samples include a pass: pass@k is 1.")
    for n, c, k in [(200, 7, 10), (200, 150, 100), (1000, 1, 100)]:
        check(close(call(pass_at_k, n, c, k), comb_pass_at_k(n, c, k), 1e-9), f"pass_at_k({n}, {c}, {k}) disagrees with the exact combinatorial value.")
    test("pass@k")

    p, low, high = call(acc_ci, [True] * 80 + [False] * 20)
    check(close(p, 0.8) and close(low, 0.8 - 1.96 * 0.04, 1e-9) and close(high, 0.8 + 1.96 * 0.04, 1e-9), f"80 of 100 correct should give (0.8, 0.7216, 0.8784); got {show((p, low, high))}.")
    p, low, high = call(acc_ci, [True] * 10)
    check(p == 1.0 and high <= 1.0 and low <= 1.0, "The interval must be clipped to [0, 1].")
    p, low, high = call(acc_ci, [True, False, False, False, False, False])
    check(low >= 0.0, "The lower end must not go below 0.")
    test("accuracy with a confidence interval")

    train = "the quick brown fox jumps over the lazy dog every single morning"
    check(close(call(overlap, "The quick brown fox sleeps", train, 3), 2 / 3), "Of the 3-grams in 'The quick brown fox sleeps', 2 of 3 appear in the training text (case-insensitive).")
    check(close(call(overlap, "completely new words here", train, 2), 0.0), "No shared n-grams gives 0.")
    check(call(overlap, "too short", train, 5) == 0.0, "A test text with no n-grams of that length gives 0.0.")
    test("n-gram contamination check")

    first = ["A", "B", "tie", "A", "B", "A"]
    swapped = ["B", "A", "tie", "A", "A", "B"]
    check(close(call(consistency, first, swapped), 5 / 6), "Undo the swap (A↔B) on the second list before comparing: then every item except the fourth agrees, so 5/6.")
    check(close(call(consistency, ["A"], ["A"]), 0.0), "A judge that always picks the first position is inconsistent once you swap: 0.")
    test("judge consistency under swapping")


run(main)
