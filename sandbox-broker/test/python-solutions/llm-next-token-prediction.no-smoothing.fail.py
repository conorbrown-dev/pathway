# expect: Row 0 should be [1/6, 3/6, 2/6] with k = 1
import math
import random


def train_bigram(tokens, vocab_size, k):
    counts = [[0] * vocab_size for _ in range(vocab_size)]
    for a, b in zip(tokens, tokens[1:]):
        counts[a][b] += 1
    return [[c / sum(row) if sum(row) else 1 / vocab_size for c in row] for row in counts]


def average_nll(table, tokens):
    pairs = list(zip(tokens, tokens[1:]))
    return sum(-math.log(table[a][b]) for a, b in pairs) / len(pairs)


def perplexity(table, tokens):
    return math.exp(average_nll(table, tokens))


def generate(table, start, length, seed):
    rng = random.Random(seed)
    out = [start]
    while len(out) < length:
        u, running = rng.random(), 0.0
        choice = len(table) - 1
        for candidate, p in enumerate(table[out[-1]]):
            running += p
            if u < running:
                choice = candidate
                break
        out.append(choice)
    return out
