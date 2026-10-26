# tests: 4
def ref_softmax(z):
    m = max(z)
    e = [math.exp(v - m) for v in z]
    t = sum(e)
    return [x / t for x in e]


def main():
    ns = load()
    temp, top_k, top_p, sample_next = (need(ns, n) for n in ("apply_temperature", "top_k_filter", "top_p_filter", "sample_next"))
    logits = [2.0, 1.0, 0.5, -1.0]

    check(close_list(call(temp, logits, 0.5), [4.0, 2.0, 1.0, -2.0]), "apply_temperature(logits, 0.5) should divide every logit by 0.5.")
    sharp = ref_softmax(call(temp, logits, 0.5))
    flat = ref_softmax(call(temp, logits, 2.0))
    check(sharp[0] > ref_softmax(logits)[0] > flat[0], "Lower temperature should give the top token more probability; higher, less.")
    test("temperature scaling")

    k2 = call(top_k, logits, 2)
    check(k2[:2] == [2.0, 1.0] and k2[2] == -math.inf and k2[3] == -math.inf, f"top_k_filter(logits, 2) should keep the two highest and set the rest to -inf; got {show(k2)}.")
    k1 = call(top_k, [1.0, 3.0, 3.0, 0.0], 1)
    check(k1 == [-math.inf, 3.0, -math.inf, -math.inf], "On a tie at the cut-off, top_k_filter keeps the lower token ID.")
    check(call(top_k, logits, 10) == logits, "With k at least the vocabulary size, nothing is removed.")
    test("top-k")

    p8 = call(top_p, logits, 0.8)
    check(p8[:2] == [2.0, 1.0] and p8[2] == -math.inf and p8[3] == -math.inf, f"top_p_filter(logits, 0.8) should keep the first two tokens (0.609 + 0.224 = 0.833 ≥ 0.8); got {show(p8)}.")
    p5 = call(top_p, logits, 0.5)
    check(p5[0] == 2.0 and all(v == -math.inf for v in p5[1:]), "top_p_filter(logits, 0.5): the top token alone already has 0.609, so keep only it.")
    check(call(top_p, logits, 1.0) == logits, "top_p_filter with p = 1 keeps every token.")
    unsorted = [0.5, 2.0, -1.0, 1.0]
    up = call(top_p, unsorted, 0.8)
    check(up[1] == 2.0 and up[3] == 1.0 and up[0] == -math.inf and up[2] == -math.inf, "top_p_filter must rank by probability, not by position in the list.")
    test("top-p")

    check(call(sample_next, [0.1, 3.0, 2.9], 0, None, None, 0.99) == 1, "With temperature 0, sample_next should return the argmax, whatever u is.")
    check(call(sample_next, logits, 1.0, None, 0.8, 0.75) == 1, "Temperature 1, top-p 0.8, u = 0.75 should pick token 1 (renormalized: [0.731, 0.269]).")
    check(call(sample_next, logits, 1.0, None, 0.8, 0.70) == 0, "Temperature 1, top-p 0.8, u = 0.70 should pick token 0.")
    check(call(sample_next, logits, 1.0, 1, None, 0.999) == 0, "With top_k = 1, only the top token can be sampled.")
    for u in [0.05, 0.3, 0.55, 0.8, 0.95]:
        probs = ref_softmax([v / 0.7 for v in logits])
        running, want = 0.0, len(probs) - 1
        for i, p in enumerate(probs):
            running += p
            if u < running:
                want = i
                break
        check(call(sample_next, logits, 0.7, None, None, u) == want, f"Plain sampling at temperature 0.7 with u = {u} should pick token {want}.")
    test("the full pipeline, including greedy")


run(main)
