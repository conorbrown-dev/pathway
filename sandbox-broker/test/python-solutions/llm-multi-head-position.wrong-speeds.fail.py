# expect: Pair i is (vector[2i], vector[2i + 1]) and turns by
import math


def split_heads(vector, n_heads):
    if len(vector) % n_heads:
        raise ValueError("vector length must divide evenly among the heads")
    size = len(vector) // n_heads
    return [vector[h * size:(h + 1) * size] for h in range(n_heads)]


def merge_heads(heads):
    return [x for head in heads for x in head]


def sinusoidal_positions(seq_len, d):
    rows = []
    for p in range(seq_len):
        row = []
        for i in range(d // 2):
            angle = p / 10000 ** (2 * i / d)
            row += [math.sin(angle), math.cos(angle)]
        rows.append(row)
    return rows


def rope(vector, position, base=10000.0):
    d = len(vector)
    out = list(vector)
    for i in range(d // 2):
        angle = position * base ** (-i / d)  # bug: half the right exponent
        a, b = vector[2 * i], vector[2 * i + 1]
        out[2 * i] = a * math.cos(angle) - b * math.sin(angle)
        out[2 * i + 1] = a * math.sin(angle) + b * math.cos(angle)
    return out
