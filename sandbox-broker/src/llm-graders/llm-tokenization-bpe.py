# tests: 4
def reference_train(text, num_merges):
    ids = list(text.encode("utf-8"))
    merges = []
    for step in range(num_merges):
        counts = {}
        for pair in zip(ids, ids[1:]):
            counts[pair] = counts.get(pair, 0) + 1
        if not counts:
            break
        pair = max(counts, key=counts.get)
        if counts[pair] < 2:
            break
        new_id = 256 + step
        merged, i = [], 0
        while i < len(ids):
            if i + 1 < len(ids) and (ids[i], ids[i + 1]) == pair:
                merged.append(new_id)
                i += 2
            else:
                merged.append(ids[i])
                i += 1
        ids = merged
        merges.append((pair, new_id))
    return merges


def main():
    ns = load()
    counts_fn, merge_fn, train_fn, encode_fn, decode_fn = (need(ns, n) for n in ("get_pair_counts", "merge", "train_bpe", "encode", "decode"))

    a, b, c, d = 97, 98, 99, 100
    counts = call(counts_fn, [a, a, a, b, d, a, a, a, b, a, c])
    check(counts == {(a, a): 4, (a, b): 2, (b, d): 1, (d, a): 1, (b, a): 1, (a, c): 1}, f"get_pair_counts gave {show(counts)}; expected (97, 97): 4, (97, 98): 2 and the rest 1.")
    check(list(counts)[:3] == [(a, a), (a, b), (b, d)], "get_pair_counts should list pairs in order of first appearance.")
    check(call(counts_fn, [5]) == {}, "A single token has no pairs.")
    test("pair counting")

    check(call(merge_fn, [a, a, a, b], (a, a), 256) == [256, a, b], "merge([a, a, a, b], (a, a), 256) should be [256, a, b]: merges don't overlap, and scan left to right.")
    check(call(merge_fn, [a, a, a, a], (a, a), 256) == [256, 256], "merge([a, a, a, a], (a, a), 256) should be [256, 256].")
    check(call(merge_fn, [1, 2, 3], (3, 4), 256) == [1, 2, 3], "If the pair never occurs, merge should return the same tokens.")
    test("non-overlapping merges")

    merges = call(train_fn, "aaabdaaabac", 2)
    check([tuple(m[0]) for m in merges] == [(a, a), (256, a)] and [m[1] for m in merges] == [256, 257], f"train_bpe('aaabdaaabac', 2) should merge (97, 97) → 256, then (256, 97) → 257 (it wins the tie because it appears first); got {show(merges)}.")
    text = "the cat sat on the mat. the cat ate the rat. that cat is the fattest cat." * 3
    expected = reference_train(text, 30)
    got = call(train_fn, text, 30)
    check([(tuple(p), n) for p, n in got] == expected, "train_bpe on a longer text gave different merges from the reference. Count pairs fresh after every merge and break ties by first appearance.")
    check(len(call(train_fn, "abcdef", 10)) == 0, "With no pair occurring at least twice, training should stop with no merges.")
    test("training with the tie rule")

    merges = call(train_fn, text, 30)
    ids = call(encode_fn, "the fat cat sat", merges)
    check(len(ids) < len("the fat cat sat"), "Encoding text similar to the training text should use fewer tokens than bytes.")
    for sample in ["the fat cat sat", "unseen words: zebra ünïcödé 😀", ""]:
        out = call(decode_fn, call(encode_fn, sample, merges), merges)
        check(out == sample, f"decode(encode({sample!r})) gave {out!r}; it should give back the original text.")
    test("encode and decode round trip")


run(main)
