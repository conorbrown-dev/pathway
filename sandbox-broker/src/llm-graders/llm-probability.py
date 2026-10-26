# tests: 4
def main():
    ns = load()
    normalize, sample, next_words, expected_value = (need(ns, n) for n in ("normalize", "sample", "next_word_distribution", "expected_value"))

    p = call(normalize, {"common": 142, "rare": 48, "legendary": 10})
    check(isinstance(p, dict) and list(p) == ["common", "rare", "legendary"], "normalize should return a dict with the same keys in the same order.")
    check(close(p["common"], 0.71) and close(p["rare"], 0.24) and close(p["legendary"], 0.05), f"normalize gave {show(p)}; expected 0.71, 0.24, 0.05.")
    check(close(sum(call(normalize, {"a": 1, "b": 2, "c": 4}).values()), 1.0), "Normalized probabilities should add up to 1.")
    check(raises(normalize, ValueError, {"a": 0}), "normalize should raise ValueError when the total count is 0.")
    test("normalizing counts")

    loot = {"common": 0.70, "rare": 0.25, "legendary": 0.05}
    check(call(sample, loot, 0.0) == "common", "u = 0.0 should pick the first outcome.")
    check(call(sample, loot, 0.69) == "common", "u = 0.69 is inside common's stretch [0, 0.70).")
    check(call(sample, loot, 0.70) == "rare", "u = 0.70 is the start of rare's stretch [0.70, 0.95).")
    check(call(sample, loot, 0.83) == "rare", "u = 0.83 should pick rare.")
    check(call(sample, loot, 0.97) == "legendary", "u = 0.97 should pick legendary.")
    check(call(sample, {"a": 0.7, "b": 0.2, "c": 0.1}, 0.9999999999999999) == "c", "A u just below 1 must still return an outcome (the last one), even if the floats add to slightly less than 1.")
    rng = random.Random(3)
    tally = {"common": 0, "rare": 0, "legendary": 0}
    for _ in range(20000):
        tally[call(sample, loot, rng.random())] += 1
    check(abs(tally["rare"] / 20000 - 0.25) < 0.02, "Over many draws, rare should come up about 25% of the time.")
    test("sampling by cumulative stretches")

    table = call(next_words, "the cat sat on the mat the end".split())
    check(isinstance(table, dict) and list(table) == ["the", "cat", "sat", "on", "mat"], f"next_word_distribution keys should be the words that have a next word, in order of first appearance; got {show(list(table) if isinstance(table, dict) else table)}.")
    check(all(close(table["the"].get(w, 0), 1 / 3) for w in ("cat", "mat", "end")), f"After 'the', each of cat, mat, end should have probability 1/3; got {show(table['the'])}.")
    check(close(table["cat"].get("sat", 0), 1.0), "After 'cat', 'sat' should have probability 1.")
    table = call(next_words, "a b a c a b".split())
    check(close(table["a"]["b"], 2 / 3) and close(table["a"]["c"], 1 / 3), "In 'a b a c a b', P(b | a) = 2/3 and P(c | a) = 1/3.")
    check(list(table["a"]) == ["b", "c"], "Inner distributions should keep the order in which next words first appear.")
    test("conditional next-word distributions")

    check(close(call(expected_value, {v: 1 / 6 for v in range(1, 7)}), 3.5), "The expected value of a fair die is 3.5.")
    check(close(call(expected_value, {0: 0.9, 100: 0.1}), 10.0), "A 10% chance of 100 (else 0) has expected value 10.")
    test("expected value")


run(main)
