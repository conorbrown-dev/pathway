# tests: 4
def reference_softmax(scores):
    m = max(scores)
    exps = [math.exp(s - m) for s in scores]
    total = sum(exps)
    return [e / total for e in exps]


def main():
    ns = load()
    softmax, lse, log_softmax, seq = (need(ns, n) for n in ("softmax", "log_sum_exp", "log_softmax", "sequence_log_prob"))

    p = call(softmax, [2.0, 1.0, 0.0])
    check(close_list(p, [0.66524096, 0.24472847, 0.09003057], 1e-6), f"softmax([2, 1, 0]) gave {show(p)}; expected about [0.665, 0.245, 0.090].")
    check(close(sum(call(softmax, [0.3, -1.2, 4.0, 2.2])), 1.0, 1e-9), "softmax values should add up to 1.")
    check(close_list(call(softmax, [5.0, 5.0]), [0.5, 0.5]), "Equal scores should give equal probabilities.")
    check(close_list(call(softmax, [1.0, 2.0, 3.0]), call(softmax, [101.0, 102.0, 103.0]), 1e-9), "Adding the same number to every score should not change softmax.")
    test("softmax values and their sum")

    try:
        big = call(softmax, [1000.0, 1001.0])
    except GraderFailure:
        fail("softmax([1000, 1001]) crashed. Subtract the largest score before calling math.exp.")
    check(close_list(big, [0.26894142, 0.73105858], 1e-6), f"softmax([1000, 1001]) should be about [0.269, 0.731], got {show(big)}.")
    check(close_list(call(softmax, [-1000.0, -1001.0]), [0.73105858, 0.26894142], 1e-6), "softmax([-1000, -1001]) should be about [0.731, 0.269], not a division by zero.")
    test("no overflow for large scores")

    check(close(call(lse, [0.0, 0.0]), math.log(2), 1e-9), "log_sum_exp([0, 0]) should be log(2).")
    check(close(call(lse, [1000.0, 1000.0]), 1000 + math.log(2), 1e-9), "log_sum_exp([1000, 1000]) should be 1000 + log(2), without overflowing.")
    ls = call(log_softmax, [3.0, 1.0, 0.0])
    check(close_list(ls, [math.log(v) for v in reference_softmax([3.0, 1.0, 0.0])], 1e-9), f"log_softmax([3, 1, 0]) gave {show(ls)}; expected about [-0.170, -2.170, -3.170].")
    tiny = call(log_softmax, [0.0, -800.0])
    check(close(tiny[1], -800.0, 1e-9), "log_softmax([0, -800])[1] should be about -800. Computing log(softmax(...)) loses it to underflow (log of 0); subtract log_sum_exp instead.")
    test("log-sum-exp and log-softmax")

    check(close(call(seq, [0.5, 0.25]), math.log(0.125), 1e-9), "sequence_log_prob([0.5, 0.25]) should be log(0.125), the log of the product.")
    long_value = call(seq, [0.1] * 400)
    check(close(long_value, 400 * math.log(0.1), 1e-9), "sequence_log_prob of 400 tokens at 0.1 each should be 400 * log(0.1), about -921. Multiplying first underflows to 0; add logs instead.")
    test("log-probability of a sequence")


run(main)
