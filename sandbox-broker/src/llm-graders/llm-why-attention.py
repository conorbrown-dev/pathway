# tests: 4
TRAIN = (
    "the cat sat on the mat and the dog sat on the rug . the cat saw the dog and the dog saw the cat . "
    "a bird sat on the fence and the cat watched the bird . the dog ran to the park and the cat ran home . "
    "the bird flew over the park and the dog barked at the bird . the cat slept on the mat all day . "
).split()
TEST = (
    "the dog sat on the mat and the cat sat on the fence . the bird saw the cat and the cat ran to the park . "
    "the dog slept on the rug all day and the bird watched the dog ."
).split()


def main():
    ns = load()
    possible, contexts, unseen, curve = (need(ns, n) for n in ("possible_contexts", "contexts", "unseen_fraction", "sparsity_curve"))

    check(call(possible, 50000, 2) == 50000, "possible_contexts(50000, 2) should be 50,000: a bigram conditions on one token.")
    check(call(possible, 50000, 3) == 2_500_000_000, "possible_contexts(50000, 3) should be 2.5 billion.")
    check(call(possible, 10, 1) == 1, "A unigram model (n = 1) has a single, empty context.")
    test("counting possible contexts")

    got = call(contexts, [1, 2, 3, 4], 3)
    check([tuple(c) for c in got] == [(1, 2), (2, 3)], f"contexts([1, 2, 3, 4], 3) should be [(1, 2), (2, 3)]: one per position that still has a next token; got {show(got)}.")
    check([tuple(c) for c in call(contexts, [1, 2, 3], 2)] == [(1,), (2,)], "contexts([1, 2, 3], 2) should be [(1,), (2,)].")
    check(call(contexts, [1, 2], 3) == [], "A sequence too short for any context gives [].")
    test("extracting contexts")

    check(close(call(unseen, [1, 2, 3, 1, 2], [1, 2, 9, 1], 2), 1 / 3), "unseen_fraction([1, 2, 3, 1, 2], [1, 2, 9, 1], 2): test contexts (1,), (2,), (9,); only (9,) is unseen, so 1/3.")
    check(close(call(unseen, [1, 2, 3, 1, 2], [9, 9, 9, 1, 1], 2), 3 / 4), "unseen_fraction([1, 2, 3, 1, 2], [9, 9, 9, 1, 1], 2): test contexts (9,), (9,), (9,), (1,); count repeats, so 3 of 4 are unseen.")
    check(close(call(unseen, [1, 2, 3], [1, 2, 3], 3), 0.0), "Identical text has no unseen contexts.")
    test("unseen fraction")

    values = call(curve, TRAIN, TEST, 5)
    check(isinstance(values, list) and len(values) == 4, "sparsity_curve(train, test, 5) should return 4 values, for n = 2, 3, 4, 5.")
    check(all(later >= earlier for earlier, later in zip(values, values[1:])), f"On real text the unseen fraction should never fall as n grows; got {show([round(v, 3) for v in values])}.")
    check(values[0] < 0.15 and values[-1] > 0.6, f"With one word of context almost everything has been seen, but with four words most contexts are new; got {show([round(v, 3) for v in values])}.")
    test("sparsity grows with n on real text")


run(main)
