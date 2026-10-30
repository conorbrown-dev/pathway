# tests: 4
def main():
    ns = load()
    mse, ce, ce_logits, avg_ce = (need(ns, n) for n in ("mean_squared_error", "cross_entropy", "cross_entropy_from_logits", "average_cross_entropy"))

    check(close(call(mse, [3, 5, 9], [2, 5, 13]), 17 / 3), "mean_squared_error([3, 5, 9], [2, 5, 13]) should be (1 + 0 + 16) / 3.")
    check(close(call(mse, [0, 0], [3, -3]), 9.0), "Squared errors are always positive: [0, 0] vs [3, -3] should be 9.")
    check(raises(mse, ValueError, [], []), "mean_squared_error([], []) should raise ValueError.")
    check(raises(mse, ValueError, [1, 2], [1]), "mean_squared_error should raise ValueError for lists of different lengths.")
    test("mean squared error")

    check(close(call(ce, [0.7, 0.2, 0.1], 0), -math.log(0.7), 1e-9), "cross_entropy([0.7, 0.2, 0.1], 0) should be -log(0.7), about 0.357.")
    check(close(call(ce, [0.6, 0.3, 0.1], 2), -math.log(0.1), 1e-9), "cross_entropy([0.6, 0.3, 0.1], 2) should be -log(0.1), about 2.303: only the correct answer's probability counts.")
    check(close(call(ce, [1.0, 0.0], 0), 0.0, 1e-12), "A certain, correct prediction has zero loss.")
    check(call(ce, [1.0, 0.0], 1) == math.inf, "cross_entropy with probability 0 on the correct answer should return math.inf.")
    test("cross-entropy from probabilities")

    logits = [3.0, 1.0, 0.0]
    m = max(logits)
    expected = (m + math.log(sum(math.exp(z - m) for z in logits))) - logits[1]
    check(close(call(ce_logits, logits, 1), expected, 1e-9), "cross_entropy_from_logits([3, 1, 0], 1) should be log-sum-exp minus the correct logit, about 2.170.")
    try:
        big = call(ce_logits, [1000.0, 0.0], 1)
    except GraderFailure:
        fail("cross_entropy_from_logits([1000, 0], 1) crashed. Subtract the largest logit before exponentiating.")
    check(close(big, 1000.0, 1e-9), "cross_entropy_from_logits([1000, 0], 1) should be about 1000: a confident wrong answer, not inf or an error. Use log-sum-exp, not log(softmax).")
    check(close(call(ce_logits, [1000.0, 0.0], 0), 0.0, 1e-9), "cross_entropy_from_logits([1000, 0], 0) should be about 0.")
    test("stable cross-entropy from logits")

    batch = [[2.0, 0.0], [0.0, 2.0], [1.0, 1.0]]
    targets = [0, 0, 1]
    value = call(avg_ce, batch, targets)
    reference = sum((max(z) + math.log(sum(math.exp(v - max(z)) for v in z))) - z[t] for z, t in zip(batch, targets)) / 3
    check(close(value, reference, 1e-9), f"average_cross_entropy gave {show(value)}; expected the mean of the three examples' losses, about {reference:.4f}.")
    test("batch average")


run(main)
