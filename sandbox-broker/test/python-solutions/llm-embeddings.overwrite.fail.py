# expect: row 2 should be the sum
import math


def lookup(table, ids):
    return [list(table[i]) for i in ids]


def one_hot_times_table(token_id, table):
    one_hot = [1.0 if r == token_id else 0.0 for r in range(len(table))]
    width = len(table[0])
    return [sum(one_hot[r] * table[r][c] for r in range(len(table))) for c in range(width)]


def _cosine(a, b):
    return sum(x * y for x, y in zip(a, b)) / (math.sqrt(sum(x * x for x in a)) * math.sqrt(sum(y * y for y in b)))


def nearest(table, token_id, k):
    scored = [(_cosine(table[token_id], row), other) for other, row in enumerate(table) if other != token_id]
    scored.sort(key=lambda item: (-item[0], item[1]))
    return [other for _, other in scored[:k]]


def embedding_gradient(vocab_size, dim, ids, upstream):
    grad = [[0.0] * dim for _ in range(vocab_size)]
    for token, g in zip(ids, upstream):
        for c in range(dim):
            grad[token][c] = g[c]
    return grad
