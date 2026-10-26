# tests: 4
def main():
    ns = load()
    sigmoid, train, confusion, pr = (need(ns, n) for n in ("sigmoid", "train_logistic", "confusion_counts", "precision_recall"))

    check(close(call(sigmoid, 0.0), 0.5), "sigmoid(0) should be 0.5.")
    check(close(call(sigmoid, 4.0), 1 / (1 + math.exp(-4)), 1e-9), "sigmoid(4) should be about 0.982.")
    try:
        low, high = call(sigmoid, -1000.0), call(sigmoid, 1000.0)
    except GraderFailure:
        fail("sigmoid crashed for a large score. For negative z compute math.exp(z) / (1 + math.exp(z)).")
    check(0.0 <= low < 1e-300 and close(high, 1.0, 1e-12), "sigmoid(-1000) should be about 0 and sigmoid(1000) about 1.")
    check(close(call(sigmoid, -2.0) + call(sigmoid, 2.0), 1.0, 1e-12), "sigmoid(-z) + sigmoid(z) should be 1.")
    test("stable sigmoid")

    rng = random.Random(21)
    rows, labels = [], []
    for _ in range(80):
        a, b = rng.uniform(-3, 3), rng.uniform(-3, 3)
        rows.append([a, b])
        labels.append(1 if 1.5 * a - b + 0.5 > 0 else 0)
    weights, bias = call(train, rows, labels, 0.5, 400)
    check(isinstance(weights, list) and len(weights) == 2, "train_logistic should return (weights, bias) with one weight per feature.")
    sig = lambda z: 1 / (1 + math.exp(-z)) if z >= 0 else math.exp(z) / (1 + math.exp(z))
    correct = sum((sig(sum(w * x for w, x in zip(weights, row)) + bias) >= 0.5) == (label == 1) for row, label in zip(rows, labels))
    check(correct >= 76, f"After training, at least 76 of 80 points should be classified correctly; got {correct}.")
    check(weights[0] > 0 and weights[1] < 0, "The learned weights should push in the directions of the true rule (positive for the first feature, negative for the second).")
    w1, b1 = call(train, [[1.0], [2.0]], [0, 1], 0.1, 1)
    check(close(w1[0], 0.025) and close(b1, 0.0), f"One step from zeros on rows [[1], [2]], labels [0, 1], learning rate 0.1 should give weights [0.025], bias 0.0; got {show((w1, b1))}. The gradient is the average of (p - y) * x.")
    test("training separates the classes")

    counts = call(confusion, [1, 1, 0, 0, 1, 0], [1, 0, 0, 1, 1, 0])
    check(counts == {"tp": 2, "fp": 1, "fn": 1, "tn": 2}, f"confusion_counts gave {show(counts)}; expected tp 2, fp 1, fn 1, tn 2.")
    test("confusion counts")

    p, r = call(pr, {"tp": 8, "fp": 4, "fn": 2, "tn": 86})
    check(close(p, 8 / 12) and close(r, 0.8), f"precision_recall should be (0.667, 0.8) for the moderation example; got {show((p, r))}.")
    check(tuple(call(pr, {"tp": 0, "fp": 0, "fn": 3, "tn": 5})) == (0.0, 0.0), "When nothing is flagged, precision has a zero denominator: report 0.0.")
    check(tuple(call(pr, {"tp": 0, "fp": 2, "fn": 0, "tn": 5})) == (0.0, 0.0), "When nothing was really positive, recall has a zero denominator: report 0.0.")
    test("precision and recall with empty cases")


run(main)
