# tests: 4
def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def main():
    ns = load()
    split, merge, positions, rope = (need(ns, n) for n in ("split_heads", "merge_heads", "sinusoidal_positions", "rope"))

    v = [1, 2, 3, 4, 5, 6, 7, 8]
    heads = call(split, v, 2)
    check([list(h) for h in heads] == [[1, 2, 3, 4], [5, 6, 7, 8]], f"split_heads(1..8, 2) should be [[1, 2, 3, 4], [5, 6, 7, 8]]; got {show(heads)}.")
    check([list(h) for h in call(split, v, 4)] == [[1, 2], [3, 4], [5, 6], [7, 8]], "split_heads(1..8, 4) should give four slices of two.")
    check(list(call(merge, heads)) == v, "merge_heads should concatenate the heads back into the original vector.")
    check(raises(split, ValueError, v, 3), "split_heads should raise ValueError when the length doesn't divide evenly.")
    test("splitting and merging heads")

    table = call(positions, 4, 6)
    check(len(table) == 4 and all(len(row) == 6 for row in table), "sinusoidal_positions(4, 6) should have 4 rows of 6 numbers.")
    for p in range(4):
        for i in range(3):
            angle = p / 10000 ** (2 * i / 6)
            check(close(table[p][2 * i], math.sin(angle), 1e-9) and close(table[p][2 * i + 1], math.cos(angle), 1e-9), f"Row {p}, pair {i} should be sin and cos of {p} / 10000 ** ({2 * i} / 6).")
    test("sinusoidal encodings")

    q = [1.0, 0.0]
    check(close_list(call(rope, q, 3, 1.0), [math.cos(3), math.sin(3)], 1e-9), "rope([1, 0], 3, base=1) should rotate by 3 radians: [cos 3, sin 3].")
    rng = random.Random(6)
    for _ in range(5):
        vec = [rng.uniform(-2, 2) for _ in range(8)]
        pos = rng.randint(0, 500)
        rotated = call(rope, vec, pos)
        check(close(math.sqrt(dot(rotated, rotated)), math.sqrt(dot(vec, vec)), 1e-9), "RoPE must not change a vector's length; it only rotates each pair.")
    check(close_list(call(rope, [0.3, -0.2, 1.1, 0.4], 0), [0.3, -0.2, 1.1, 0.4], 1e-12), "At position 0 RoPE should change nothing.")
    vec = [0.5, -1.0, 2.0, 0.25, -0.75, 1.5]
    expected = []
    for i in range(3):
        angle = 5 * 10000.0 ** (-2 * i / 6)
        a, b = vec[2 * i], vec[2 * i + 1]
        expected += [a * math.cos(angle) - b * math.sin(angle), a * math.sin(angle) + b * math.cos(angle)]
    check(close_list(call(rope, vec, 5), expected, 1e-9), "rope(vector, 5) disagrees with the reference. Pair i is (vector[2i], vector[2i + 1]) and turns by position * base ** (-2 * i / d).")
    test("RoPE keeps vector lengths")

    q = [rng.uniform(-1, 1) for _ in range(8)]
    k = [rng.uniform(-1, 1) for _ in range(8)]
    base_score = dot(call(rope, q, 7), call(rope, k, 4))
    for shift in (1, 25, 300):
        shifted = dot(call(rope, q, 7 + shift), call(rope, k, 4 + shift))
        check(close(shifted, base_score, 1e-9), f"rope(q, {7 + shift}) · rope(k, {4 + shift}) should equal rope(q, 7) · rope(k, 4): only the gap of 3 positions should matter.")
    check(not close(dot(call(rope, q, 9), call(rope, k, 4)), base_score, 1e-6), "Changing the gap between positions should change the score.")
    test("RoPE scores depend only on relative position")


run(main)
