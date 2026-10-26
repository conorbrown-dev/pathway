# tests: 4
def main():
    ns = load()
    train, nll, ppl, generate = (need(ns, n) for n in ("train_bigram", "average_nll", "perplexity", "generate"))

    table = call(train, [0, 1, 0, 1, 0, 2], 3, 1)
    check(len(table) == 3 and all(len(row) == 3 for row in table), "train_bigram should return a vocab_size × vocab_size table.")
    check(close_list(table[0], [1 / 6, 3 / 6, 2 / 6]), f"Row 0 should be [1/6, 3/6, 2/6] with k = 1; got {show(table[0])}.")
    check(close_list(table[2], [1 / 3, 1 / 3, 1 / 3]), "A token never seen as a previous token should get a uniform row from smoothing.")
    check(all(close(sum(row), 1.0) for row in table), "Every row should add up to 1.")
    sharp = call(train, [0, 1, 0, 1], 2, 0.01)
    check(sharp[0][1] > 0.98 and sharp[0][0] > 0, "With small k, seen pairs dominate, but unseen pairs must stay above zero.")
    test("smoothed counts")

    check(close(call(nll, table, [0, 1]), math.log(2), 1e-9), "average_nll(table, [0, 1]) should be -log(0.5) = log 2.")
    value = call(nll, table, [0, 1, 0, 2])
    expected = (-math.log(3 / 6) - math.log(table[1][0]) - math.log(2 / 6)) / 3
    check(close(value, expected, 1e-9), "average_nll should average -log P(next | previous) over every adjacent pair.")
    test("average negative log-likelihood")

    check(close(call(ppl, table, [0, 1]), 2.0, 1e-9), "perplexity should be exp(average_nll): 2 for this text.")
    uniform = [[1 / 50] * 50 for _ in range(50)]
    check(close(call(ppl, uniform, [3, 7, 9, 1, 0]), 50.0, 1e-9), "A uniform model over 50 tokens has perplexity exactly 50.")
    test("perplexity")

    rng_table = call(train, [0, 1, 2, 0, 2, 1, 1, 0, 2, 2, 0, 1], 3, 0.5)
    out = call(generate, rng_table, 0, 12, 42)
    check(isinstance(out, list) and len(out) == 12 and out[0] == 0, "generate should return `length` tokens starting with `start`.")
    rng = random.Random(42)
    expected, token = [0], 0
    for _ in range(11):
        u, running = rng.random(), 0.0
        choice = len(rng_table) - 1
        for candidate, p in enumerate(rng_table[token]):
            running += p
            if u < running:
                choice = candidate
                break
        expected.append(choice)
        token = choice
    check(out == expected, f"generate(table, 0, 12, 42) gave {show(out)}; expected {show(expected)}. Make one random.Random(seed) before the loop and draw one u per new token.")
    test("reproducible generation")


run(main)
