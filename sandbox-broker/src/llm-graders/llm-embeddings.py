# tests: 4
def main():
    ns = load()
    lookup, one_hot_times, nearest, grad = (need(ns, n) for n in ("lookup", "one_hot_times_table", "nearest", "embedding_gradient"))
    table = [[0.1, 0.2], [0.5, -0.3], [0.0, 0.9], [0.4, 0.4]]

    rows = call(lookup, table, [2, 0, 2])
    check(close_matrix(rows, [[0.0, 0.9], [0.1, 0.2], [0.0, 0.9]]), f"lookup(table, [2, 0, 2]) gave {show(rows)}.")
    rows[0][0] = 99.0
    check(table[2][0] == 0.0, "Changing a row returned by lookup changed the table. Return copies of the rows.")
    test("lookup copies rows")

    for token in range(4):
        check(close_list(call(one_hot_times, token, table), table[token]), f"one_hot_times_table({token}, table) should equal row {token}.")
    check(close_list(call(one_hot_times, 1, [[1, 2, 3], [4, 5, 6]]), [4, 5, 6]), "one_hot_times_table should work for any row width.")
    test("one-hot times the table equals lookup")

    emb = [[1.0, 0.0], [0.9, 0.1], [0.0, 1.0], [-1.0, 0.0], [0.7, 0.7], [2.0, 0.0]]
    got = call(nearest, emb, 0, 3)
    check(list(got) == [5, 1, 4], f"nearest(emb, 0, 3) should be [5, 1, 4]: token 5 points exactly the same way (cosine 1) even though it's longer; got {show(got)}.")
    check(0 not in call(nearest, emb, 0, 5), "A token should not be listed as its own neighbor.")
    tie = [[1.0, 0.0], [0.0, 1.0], [0.0, 2.0]]
    check(list(call(nearest, tie, 0, 2)) == [1, 2], "On a tie in similarity, the smaller token ID comes first.")
    test("nearest neighbors by cosine similarity")

    g = call(grad, 4, 2, [2, 0, 2], [[0.1, 0.0], [0.3, 0.3], [-0.2, 0.5]])
    check(close_matrix(g, [[0.3, 0.3], [0.0, 0.0], [-0.1, 0.5], [0.0, 0.0]]), f"embedding_gradient gave {show(g)}; row 2 should be the sum [0.1 - 0.2, 0.0 + 0.5], and unused rows zero.")
    test("gradients accumulate into used rows only")


run(main)
