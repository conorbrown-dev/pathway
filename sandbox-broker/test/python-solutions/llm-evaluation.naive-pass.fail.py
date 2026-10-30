# expect: pass_at_k(10, 3, 5) should be 1 - 21/252
import math


def pass_at_k(n, c, k):
    if c == 0:
        return 0.0
    if n - c < k:
        return 1.0
    return 1 - (1 - c / n) ** k  # bug: assumes sampling with replacement


def accuracy_with_interval(correct):
    n = len(correct)
    p = sum(1 for flag in correct if flag) / n
    margin = 1.96 * math.sqrt(p * (1 - p) / n)
    return p, max(0.0, p - margin), min(1.0, p + margin)


def _ngrams(text, n):
    words = text.lower().split()
    return [tuple(words[i:i + n]) for i in range(len(words) - n + 1)]


def ngram_overlap(test_text, train_text, n):
    test = _ngrams(test_text, n)
    if not test:
        return 0.0
    train = set(_ngrams(train_text, n))
    return sum(gram in train for gram in test) / len(test)


def judge_consistency(first_order, swapped_order):
    undo = {"A": "B", "B": "A", "tie": "tie"}
    agree = sum(a == undo[b] for a, b in zip(first_order, swapped_order))
    return agree / len(first_order)
