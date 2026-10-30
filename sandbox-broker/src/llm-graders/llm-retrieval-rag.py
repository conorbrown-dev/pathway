# tests: 4
def main():
    ns = load()
    chunk, bow, top_k, recall = (need(ns, n) for n in ("chunk_words", "bag_of_words", "top_k", "recall_at_k"))

    words = " ".join(f"w{i}" for i in range(10))
    chunks = call(chunk, words, 4, 1)
    check(chunks == ["w0 w1 w2 w3", "w3 w4 w5 w6", "w6 w7 w8 w9"], f"chunk_words(10 words, 4, 1) should give chunks starting at 0, 3 and 6; got {show(chunks)}.")
    check(call(chunk, "a b c", 5, 2) == ["a b c"], "A text shorter than one chunk becomes a single chunk.")
    check(call(chunk, " ".join(f"w{i}" for i in range(9)), 3, 0) == ["w0 w1 w2", "w3 w4 w5", "w6 w7 w8"], "With overlap 0, chunks shouldn't share words or leave a duplicate at the end.")
    check(call(chunk, "", 4, 1) == [], "An empty text gives no chunks.")
    check(raises(chunk, ValueError, words, 4, 4) and raises(chunk, ValueError, words, 4, -1), "overlap must satisfy 0 <= overlap < size.")
    test("overlapping chunks")

    vocab = ["cat", "dog", "mat"]
    check(list(call(bow, "The cat sat on the mat today. Cat!", vocab)) == [1, 0, 1], "bag_of_words should count vocabulary words in the lowercased text split on whitespace ('Cat!' isn't 'cat'). Expected [1, 0, 1].")
    check(list(call(bow, "dog dog CAT", vocab)) == [1, 2, 0], "bag_of_words('dog dog CAT') should be [1, 2, 0].")
    test("bag-of-words vectors")

    chunk_vectors = [[1, 0, 0], [0, 1, 0], [1, 1, 0], [0, 0, 0], [2, 0, 0]]
    got = call(top_k, [1, 0, 0], chunk_vectors, 3)
    check(list(got) == [0, 4, 2], f"top_k([1, 0, 0], ..., 3) should be [0, 4, 2]: chunks 0 and 4 tie at similarity 1 (smaller index first), then chunk 2; got {show(got)}.")
    check(list(call(top_k, [0, 0, 1], chunk_vectors, 2)) == [0, 1], "When every similarity is 0 (including zero vectors), order by index.")
    test("top-k by cosine similarity")

    check(close(call(recall, [2, 0, 3], [1, 2], 3), 0.5), "recall_at_k([2, 0, 3], [1, 2], 3) should be 1/2.")
    check(close(call(recall, [2, 0, 1], [1, 2], 2), 0.5), "Only the first k results count: recall@2 here is 1/2.")
    check(close(call(recall, [1, 2], [1, 2], 2), 1.0), "Finding every relevant chunk gives 1.")
    test("recall@k")


run(main)
