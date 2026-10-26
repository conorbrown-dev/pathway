import json
import math


def score_output(expected, output, mode):
    if mode == "exact":
        return 1.0 if output.strip() == expected.strip() else 0.0
    try:
        parsed = json.loads(output)
    except json.JSONDecodeError:
        return 0.0
    if not isinstance(parsed, dict):
        return 0.0
    return 1.0 if all(key in parsed and parsed[key] == value and type(parsed[key]) is type(value) for key, value in expected.items()) else 0.0


def paired_counts(scores_a, scores_b):
    wins = sum(1 for a, b in zip(scores_a, scores_b) if a > b)
    losses = sum(1 for a, b in zip(scores_a, scores_b) if a < b)
    return wins, losses, len(scores_a) - wins - losses


def sign_test_p(wins, losses):
    n = wins + losses
    if n == 0:
        return 1.0
    k = min(wins, losses)
    tail = sum(math.comb(n, j) for j in range(k + 1)) / 2 ** n
    return min(1.0, 2 * tail)


def summarize(scores):
    n = len(scores)
    p = sum(scores) / n
    margin = 1.96 * math.sqrt(p * (1 - p) / n)
    return p, max(0.0, p - margin), min(1.0, p + margin)
