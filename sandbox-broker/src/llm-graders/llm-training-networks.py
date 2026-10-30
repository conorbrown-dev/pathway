# tests: 4
def reference_adam(params, grads_seq, lr, b1=0.9, b2=0.999, eps=1e-8):
    m = [0.0] * len(params)
    v = [0.0] * len(params)
    params = list(params)
    for t, grads in enumerate(grads_seq, start=1):
        m = [b1 * mi + (1 - b1) * g for mi, g in zip(m, grads)]
        v = [b2 * vi + (1 - b2) * g * g for vi, g in zip(v, grads)]
        params = [p - lr * (mi / (1 - b1 ** t)) / (math.sqrt(vi / (1 - b2 ** t)) + eps) for p, mi, vi in zip(params, m, v)]
    return params


def main():
    ns = load()
    minibatches, adam_step, clip, schedule = (need(ns, n) for n in ("minibatches", "adam_step", "clip_by_global_norm", "warmup_cosine"))

    batches = call(minibatches, 10, 4, 7)
    check([len(b) for b in batches] == [4, 4, 2], f"minibatches(10, 4, seed) should have sizes [4, 4, 2]; got {show([len(b) for b in batches])}.")
    flat = [i for b in batches for i in b]
    check(sorted(flat) == list(range(10)), "Every index 0..9 must appear exactly once across the batches.")
    check(call(minibatches, 10, 4, 7) == batches, "The same seed must give the same batches.")
    check(call(minibatches, 10, 4, 8) != batches, "Different seeds should give a different order.")
    check(flat != list(range(10)), "The indexes should be shuffled, not left in order.")
    test("minibatches cover every example once")

    state = {}
    p = call(adam_step, [1.0], [0.5], state, 0.1)
    check(close_list(p, [0.9], 1e-6), f"The first Adam step from 1.0 with gradient 0.5 and lr 0.1 should give about 0.9; got {show(p)}. Did you apply bias correction with t = 1?")
    state = {}
    params = [0.5, -1.0]
    grads_seq = [[0.2, -0.4], [0.1, 0.3], [-0.5, 0.05]]
    for grads in grads_seq:
        params = call(adam_step, params, grads, state, 0.01)
    check(close_list(params, reference_adam([0.5, -1.0], grads_seq, 0.01), 1e-9), "After three Adam steps your parameters differ from the reference. Keep t, m and v in `state` between calls.")
    state, x = {}, [0.0, 0.0]
    for _ in range(600):
        g = [2 * (x[0] - 3), 20 * (x[1] + 1)]
        x = call(adam_step, x, g, state, 0.05)
    check(close(x[0], 3.0, 0.02) and close(x[1], -1.0, 0.02), f"Adam should find the bottom of (x - 3)^2 + 10(y + 1)^2 near (3, -1); got {show([round(v, 3) for v in x])}.")
    test("Adam matches a reference and minimizes a bowl")

    clipped = call(clip, [3.0, 4.0], 1.0)
    check(close_list(clipped, [0.6, 0.8]), f"clip_by_global_norm([3, 4], 1) should scale to length 1: [0.6, 0.8]; got {show(clipped)}.")
    check(close_list(call(clip, [0.3, 0.4], 1.0), [0.3, 0.4]), "Gradients already within the limit should be unchanged.")
    check(close_list(call(clip, [0.0, 0.0], 1.0), [0.0, 0.0]), "All-zero gradients should stay zero (no division by zero).")
    clipped = call(clip, [30.0, -40.0, 0.0], 5.0)
    check(close_list(clipped, [3.0, -4.0, 0.0]), "Clipping must keep the direction and signs: [30, -40, 0] with max 5 gives [3, -4, 0].")
    test("clipping keeps direction and caps length")

    args = (1000, 100, 3e-4, 3e-5)
    check(close(call(schedule, 0, *args), 0.0, 1e-12), "At step 0 the warmup learning rate should be 0.")
    check(close(call(schedule, 50, *args), 1.5e-4, 1e-9), "Halfway through warmup the rate should be half the peak.")
    check(close(call(schedule, 100, *args), 3e-4, 1e-9), "At the end of warmup the rate should equal the peak.")
    check(close(call(schedule, 550, *args), (3e-4 + 3e-5) / 2, 1e-9), "Halfway through the decay the cosine schedule is midway between peak and floor.")
    check(close(call(schedule, 1000, *args), 3e-5, 1e-9), "At the final step the rate should reach min_lr.")
    rates = [call(schedule, s, *args) for s in range(100, 1001, 50)]
    check(all(later <= earlier + 1e-15 for earlier, later in zip(rates, rates[1:])), "After warmup the rate should only decrease.")
    test("warmup then cosine decay")


run(main)
