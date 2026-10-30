# tests: 4
def main():
    ns = load()
    layer_norm, rms_norm, block_forward, gpt_parameters = (need(ns, n) for n in ("layer_norm", "rms_norm", "block_forward", "gpt_parameters"))

    out = call(layer_norm, [2.0, 4.0, 6.0], [1.0, 1.0, 1.0], [0.0, 0.0, 0.0], 0.0)
    s = math.sqrt(8 / 3)
    check(close_list(out, [-2 / s, 0.0, 2 / s], 1e-9), f"layer_norm([2, 4, 6]) with gain 1, shift 0, eps 0 should be about [-1.225, 0, 1.225]; got {show(out)}. Use the population variance (divide by the count).")
    out = call(layer_norm, [2.0, 4.0, 6.0], [2.0, 1.0, 0.5], [1.0, 0.0, -1.0], 0.0)
    check(close_list(out, [-4 / s + 1, 0.0, 1 / s - 1], 1e-9), "After normalizing, multiply by gamma and add beta, entry by entry.")
    check(close_list(call(layer_norm, [5.0, 5.0], [1.0, 1.0], [0.0, 0.0]), [0.0, 0.0], 1e-6), "A constant vector normalizes to zeros (eps keeps it from dividing by zero).")
    test("layer norm")

    out = call(rms_norm, [3.0, 4.0], [1.0, 1.0], 0.0)
    r = math.sqrt((9 + 16) / 2)
    check(close_list(out, [3 / r, 4 / r], 1e-9), f"rms_norm([3, 4]) should divide by sqrt((9 + 16) / 2) ≈ 3.536 without subtracting the mean; got {show(out)}.")
    check(close_list(call(rms_norm, [3.0, 4.0], [2.0, 0.5], 0.0), [6 / r, 2 / r], 1e-9), "rms_norm should multiply by gamma.")
    test("RMSNorm")

    trace = []
    norm1 = lambda v: (trace.append("norm1"), [x * 10 for x in v])[1]
    norm2 = lambda v: (trace.append("norm2"), [x * 100 for x in v])[1]
    attention = lambda v: (trace.append("attention"), [x + 1 for x in v])[1]
    feed_forward = lambda v: (trace.append("feed_forward"), [x * 2 for x in v])[1]
    result = call(block_forward, [1.0, 2.0], attention, feed_forward, norm1, norm2)
    # h = x + attention(norm1(x)) = [1 + 11, 2 + 21] = [12, 23]; out = h + feed_forward(norm2(h)) = [12 + 2400, 23 + 4600]
    check(trace == ["norm1", "attention", "norm2", "feed_forward"], f"block_forward should call norm1, attention, norm2, feed_forward in that order; it called {trace}.")
    check(close_list(result, [2412.0, 4623.0]), f"block_forward should compute h = x + attention(norm1(x)), then h + feed_forward(norm2(h)); got {show(result)}. The second step reads h, not x.")
    test("pre-norm residual composition")

    check(call(gpt_parameters, 50257, 1024, 768, 12, True) == 124_439_808, "GPT-2 small (tied) should have exactly 124,439,808 parameters.")
    check(call(gpt_parameters, 50257, 1024, 768, 12, False) == 124_439_808 + 50257 * 768, "Untied, add a separate vocab_size × d_model output projection.")
    check(call(gpt_parameters, 256, 128, 256, 4, True) == 3_257_856, "The tiny GPT from the lab (256, 128, 256, 4, tied) should have 3,257,856 parameters.")
    test("GPT-2's exact parameter count")


run(main)
