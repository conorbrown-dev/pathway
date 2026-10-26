# expect: round, don't truncate
import math


def weight_memory_gb(params, bits):
    return params * bits / 8 / 1e9


def kv_cache_gb(layers, kv_heads, head_dim, tokens, bytes_per_value=2):
    return 2 * layers * kv_heads * head_dim * bytes_per_value * tokens / 1e9


def decode_tokens_per_second(params, bits, bandwidth_gb_s):
    return bandwidth_gb_s / weight_memory_gb(params, bits)


def quantize_int8(values):
    largest = max((abs(v) for v in values), default=0.0)
    if largest == 0:
        return [0 for _ in values], 1.0
    scale = largest / 127
    return [max(-127, min(127, int(v / scale))) for v in values], scale


def dequantize(integers, scale):
    return [q * scale for q in integers]
