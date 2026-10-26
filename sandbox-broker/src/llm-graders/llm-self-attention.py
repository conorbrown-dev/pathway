# tests: 4
def ref_attention(Q, K, V, causal):
    d = len(Q[0])
    out, weights = [], []
    for i, q in enumerate(Q):
        scores = [sum(a * b for a, b in zip(q, k)) / math.sqrt(d) for k in K]
        if causal:
            scores = [s if j <= i else -math.inf for j, s in enumerate(scores)]
        m = max(scores)
        exps = [math.exp(s - m) for s in scores]
        total = sum(exps)
        w = [e / total for e in exps]
        weights.append(w)
        out.append([sum(w[j] * V[j][c] for j in range(len(V))) for c in range(len(V[0]))])
    return out, weights


def main():
    ns = load()
    scores_fn, mask_fn, softmax_fn, attention = (need(ns, n) for n in ("attention_scores", "causal_mask", "row_softmax", "attention"))
    Q = [[1.0, 0.0], [1.0, 1.0]]
    K = [[1.0, 0.0], [0.0, 1.0]]
    V = [[10.0, 0.0], [0.0, 10.0]]

    s = call(scores_fn, Q, K)
    r = 1 / math.sqrt(2)
    check(close_matrix(s, [[r, 0.0], [r, r]], 1e-9), f"attention_scores(Q, K) should be the dot products divided by √2: [[0.707, 0], [0.707, 0.707]]; got {show(s)}.")
    rng = random.Random(8)
    Q4 = [[rng.uniform(-1, 1) for _ in range(4)] for _ in range(3)]
    K4 = [[rng.uniform(-1, 1) for _ in range(4)] for _ in range(3)]
    expected = [[sum(a * b for a, b in zip(q, k)) / 2 for k in K4] for q in Q4]
    check(close_matrix(call(scores_fn, Q4, K4), expected, 1e-9), "With 4-number queries, scores should be divided by √4 = 2.")
    test("scaled scores")

    m = call(mask_fn, [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0]])
    check(m[0][1] == -math.inf and m[0][2] == -math.inf and m[1][2] == -math.inf, "causal_mask should set every entry with j > i to -inf.")
    check(m[0][0] == 1.0 and m[1][0] == 4.0 and m[1][1] == 5.0 and m[2] == [7.0, 8.0, 9.0], "causal_mask should leave entries with j <= i unchanged.")
    original = [[1.0, 2.0], [3.0, 4.0]]
    call(mask_fn, original)
    check(original == [[1.0, 2.0], [3.0, 4.0]], "causal_mask should return a copy, not change its input.")
    test("causal mask")

    w = call(softmax_fn, [[0.5, -math.inf, -math.inf], [1.0, 1.0, -math.inf], [1000.0, 999.0, 0.0]])
    check(w[0] == [1.0, 0.0, 0.0] or close_list(w[0], [1.0, 0.0, 0.0], 1e-12), "A row with only one unmasked score puts all its weight there.")
    check(w[1][2] == 0.0 and close_list(w[1][:2], [0.5, 0.5]), "Masked entries must get weight exactly 0.")
    check(all(close(sum(row), 1.0, 1e-12) for row in w), "Each row of weights should add up to 1, even with large scores.")
    test("softmax rows add up to 1")

    out, weights = call(attention, Q, K, V, True)
    check(close_matrix(out, [[10.0, 0.0], [5.0, 5.0]], 1e-9), f"Causal attention on the lesson's example should give [[10, 0], [5, 5]]; got {show(out)}.")
    check(close_matrix(weights, [[1.0, 0.0], [0.5, 0.5]], 1e-9), "Causal weights for the lesson's example should be [[1, 0], [0.5, 0.5]].")
    rng = random.Random(3)
    for causal in (True, False):
        Qr = [[rng.uniform(-2, 2) for _ in range(3)] for _ in range(5)]
        Kr = [[rng.uniform(-2, 2) for _ in range(3)] for _ in range(5)]
        Vr = [[rng.uniform(-2, 2) for _ in range(4)] for _ in range(5)]
        got_out, got_w = call(attention, Qr, Kr, Vr, causal)
        want_out, want_w = ref_attention(Qr, Kr, Vr, causal)
        check(close_matrix(got_w, want_w, 1e-9) and close_matrix(got_out, want_out, 1e-9), f"attention with causal={causal} on random 5-position inputs disagrees with the reference.")
    test("attention output, with and without the mask")


run(main)
