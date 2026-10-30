# tests: 4
def main():
    ns = load()
    mean, argmax, word_counts, shuffled = (need(ns, n) for n in ("mean", "argmax", "word_counts", "shuffled"))

    check(close(call(mean, [2, 4, 9]), 5.0), "mean([2, 4, 9]) should be 5.0.")
    check(close(call(mean, [0.5]), 0.5), "mean([0.5]) should be 0.5.")
    check(raises(mean, ValueError, []), "mean([]) should raise ValueError: there is no average of nothing.")
    test("mean and its empty-list error")

    check(call(argmax, [0.1, 0.7, 0.2]) == 1, "argmax([0.1, 0.7, 0.2]) should be 1.")
    check(call(argmax, [-5, -2, -9]) == 1, "argmax([-5, -2, -9]) should be 1: the largest of negative numbers is the one closest to zero.")
    check(call(argmax, [3, 8, 8, 1]) == 1, "argmax([3, 8, 8, 1]) should be 1: on a tie, return the first index.")
    check(call(argmax, [4]) == 0, "argmax([4]) should be 0.")
    test("argmax with ties")

    counts = call(word_counts, "The cat saw the  other cat\nTHE end")
    check(isinstance(counts, dict), "word_counts should return a dict.")
    check(counts == {"the": 3, "cat": 2, "saw": 1, "other": 1, "end": 1}, f"word_counts gave {show(counts)}; lowercase first, then split on any whitespace.")
    check(call(word_counts, "") == {}, "word_counts('') should be an empty dict.")
    test("word counting")

    items = list(range(20))
    original = list(items)
    result = call(shuffled, items, 7)
    expected = list(range(20))
    random.Random(7).shuffle(expected)
    check(items == original, "shuffled changed the list it was given. Copy it first with list(items).")
    check(isinstance(result, list) and result == expected, "shuffled(items, 7) should match random.Random(7).shuffle applied to a copy.")
    check(call(shuffled, items, 7) == result, "The same seed should give the same order every time.")
    check(call(shuffled, items, 8) != result, "Different seeds should (almost always) give different orders.")
    test("seeded shuffle that leaves the input alone")


run(main)
