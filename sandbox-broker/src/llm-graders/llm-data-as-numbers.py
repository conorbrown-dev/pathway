# tests: 4
def main():
    ns = load()
    one_hot, build_vocabulary, encode_labels, standardize = (need(ns, n) for n in ("one_hot", "build_vocabulary", "encode_labels", "standardize"))

    check(call(one_hot, 1, 3) == [0, 1, 0], "one_hot(1, 3) should be [0, 1, 0].")
    check(call(one_hot, 0, 1) == [1], "one_hot(0, 1) should be [1].")
    check(call(one_hot, 4, 5) == [0, 0, 0, 0, 1], "one_hot(4, 5) should put the 1 in the last slot.")
    check(raises(one_hot, ValueError, 3, 3), "one_hot(3, 3) should raise ValueError: slots are numbered 0, 1, 2.")
    check(raises(one_hot, ValueError, -1, 3), "one_hot(-1, 3) should raise ValueError rather than count from the end.")
    test("one-hot vectors and bad indexes")

    vocabulary = call(build_vocabulary, ["uef", "aeon", "uef", "cybran", "aeon"])
    check(vocabulary == {"uef": 0, "aeon": 1, "cybran": 2}, f"build_vocabulary gave {show(vocabulary)}; number labels in order of first appearance.")
    check(call(build_vocabulary, []) == {}, "build_vocabulary([]) should be an empty dict.")
    test("vocabulary order")

    rows = call(encode_labels, ["aeon", "uef", "aeon"], vocabulary)
    check(rows == [[0, 1, 0], [1, 0, 0], [0, 1, 0]], f"encode_labels gave {show(rows)}.")
    check(raises(encode_labels, KeyError, ["seraphim"], vocabulary), "encode_labels should raise KeyError for a label that is not in the vocabulary.")
    test("label encoding")

    z = call(standardize, [2, 4, 4, 6])
    check(close_list(z, [-math.sqrt(2), 0.0, 0.0, math.sqrt(2)], 1e-6), f"standardize([2, 4, 4, 6]) gave {show(z)}; expected about [-1.414, 0, 0, 1.414]. Divide by the count (population standard deviation), not count - 1.")
    z = call(standardize, [10.0, 20.0, 30.0])
    check(close(sum(z), 0.0, 1e-9) and close(sum(v * v for v in z) / 3, 1.0, 1e-9), "Standardized values should have mean 0 and average squared value 1.")
    check(close_list(call(standardize, [5, 5, 5]), [0.0, 0.0, 0.0]), "standardize([5, 5, 5]) should be [0, 0, 0]: a feature that never changes carries no information.")
    test("z-scores, including a constant feature")


run(main)
