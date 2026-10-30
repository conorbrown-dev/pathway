# tests: 4
def main():
    ns = load()
    add, scale, dot, norm, cosine = (need(ns, n) for n in ("add", "scale", "dot", "norm", "cosine_similarity"))

    check(close_list(call(add, [1, 2], [3, 5]), [4, 7]), "add([1, 2], [3, 5]) should be [4, 7].")
    check(raises(add, ValueError, [1, 2], [1]), "add should raise ValueError for vectors of different lengths.")
    a = [1, 2]
    check(close_list(call(scale, 3, a), [3, 6]) and a == [1, 2], "scale(3, [1, 2]) should return [3, 6] without changing the input.")
    check(close_list(call(scale, -1, [2, -5]), [-2, 5]), "scale(-1, v) should flip the vector.")
    test("adding and scaling")

    check(close(call(dot, [1, 2, 3], [4, 5, 6]), 32), "dot([1, 2, 3], [4, 5, 6]) should be 32.")
    check(close(call(dot, [2, 0], [0, 3]), 0), "Vectors at right angles have a dot product of 0.")
    check(close(call(dot, [2, 0], [-3, 0]), -6), "Opposite vectors have a negative dot product.")
    check(raises(dot, ValueError, [1, 2, 3], [1, 2]), "dot should raise ValueError for different lengths instead of silently ignoring extra entries.")
    test("dot product")

    check(close(call(norm, [3, 4]), 5.0), "norm([3, 4]) should be 5.")
    check(close(call(norm, [1, 2, 2]), 3.0), "norm([1, 2, 2]) should be 3.")
    check(close(call(norm, [0, 0]), 0.0), "norm([0, 0]) should be 0.")
    test("length")

    check(close(call(cosine, [4, 1, 0], [3, 0, 1]), 12 / (math.sqrt(17) * math.sqrt(10)), 1e-9), "cosine_similarity([4, 1, 0], [3, 0, 1]) should be about 0.920.")
    check(close(call(cosine, [1, 1], [5, 5]), 1.0, 1e-9), "Vectors pointing the same way have cosine similarity 1, whatever their lengths.")
    check(close(call(cosine, [1, 0], [-2, 0]), -1.0, 1e-9), "Opposite vectors have cosine similarity -1.")
    rng = random.Random(11)
    for _ in range(50):
        u = [rng.uniform(-5, 5) for _ in range(6)]
        v = [rng.uniform(-5, 5) for _ in range(6)]
        value = call(cosine, u, v)
        check(-1 - 1e-9 <= value <= 1 + 1e-9, f"cosine_similarity must lie between -1 and 1, got {value}.")
    check(raises(cosine, ValueError, [0, 0], [1, 2]), "cosine_similarity with a zero vector should raise ValueError: it has no direction.")
    check(raises(cosine, ValueError, [1, 2], [1, 2, 3]), "cosine_similarity should raise ValueError for different lengths.")
    test("cosine similarity and its zero-vector error")


run(main)
