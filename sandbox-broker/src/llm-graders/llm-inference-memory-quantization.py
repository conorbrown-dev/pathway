# tests: 4
def main():
    ns = load()
    weights, kv, speed, quantize, dequantize = (need(ns, n) for n in ("weight_memory_gb", "kv_cache_gb", "decode_tokens_per_second", "quantize_int8", "dequantize"))

    check(close(call(weights, 7e9, 16), 14.0, 1e-9), "weight_memory_gb(7e9, 16) should be 14 GB.")
    check(close(call(weights, 7e9, 4), 3.5, 1e-9), "weight_memory_gb(7e9, 4) should be 3.5 GB.")
    check(close(call(kv, 32, 8, 128, 32000), 2 * 32 * 8 * 128 * 2 * 32000 / 1e9, 1e-9), "kv_cache_gb(32, 8, 128, 32000) should be about 4.19 GB with 2 bytes per value.")
    check(close(call(kv, 32, 8, 128, 1000, 1), 2 * 32 * 8 * 128 * 1000 / 1e9, 1e-9), "kv_cache_gb should use bytes_per_value.")
    test("weight and KV-cache memory")

    check(close(call(speed, 7e9, 16, 1000), 1000 / 14, 1e-9), "decode_tokens_per_second(7e9, 16, 1000) should be about 71.")
    check(close(call(speed, 7e9, 4, 1000), 1000 / 3.5, 1e-9), "Quantizing to 4 bits should raise the ceiling to about 286 tokens per second.")
    test("bandwidth-bound decoding speed")

    q, scale = call(quantize, [0.8, -0.3, 0.0, 0.1])
    check(close(scale, 0.8 / 127, 1e-12), f"The scale should be max |value| / 127 = 0.8 / 127; got {scale}.")
    check(list(q) == [127, -48, 0, 16], f"quantize_int8([0.8, -0.3, 0, 0.1]) should give [127, -48, 0, 16] (round, don't truncate: -0.3 / scale = -47.6); got {show(q)}.")
    check(all(isinstance(v, int) for v in q), "Quantized values should be Python ints.")
    q0, s0 = call(quantize, [0.0, 0.0])
    check(list(q0) == [0, 0] and s0 == 1.0, "An all-zero group should give scale 1.0 and zeros, not a division by zero.")
    test("int8 quantization")

    rng = random.Random(12)
    values = [rng.gauss(0, 0.05) for _ in range(256)] + [0.9]
    q, scale = call(quantize, values)
    check(all(-127 <= v <= 127 for v in q), "Quantized integers must stay within -127..127.")
    back = call(dequantize, q, scale)
    worst = max(abs(a - b) for a, b in zip(values, back))
    check(worst <= scale / 2 + 1e-12, f"Dequantized values should be within half a step ({scale / 2:.5f}) of the originals; the worst error was {worst:.5f}.")
    test("dequantization error stays within half a step")


run(main)
