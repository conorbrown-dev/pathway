# expect: precision_recall
import math


def sigmoid(z):
    if z >= 0:
        return 1 / (1 + math.exp(-z))
    e = math.exp(z)
    return e / (1 + e)


def train_logistic(rows, labels, learning_rate, steps):
    weights, bias, n = [0.0] * len(rows[0]), 0.0, len(rows)
    for _ in range(steps):
        errors = [sigmoid(sum(w * x for w, x in zip(weights, row)) + bias) - y for row, y in zip(rows, labels)]
        weights = [w - learning_rate * sum(e * row[j] for e, row in zip(errors, rows)) / n for j, w in enumerate(weights)]
        bias -= learning_rate * sum(errors) / n
    return weights, bias


def confusion_counts(predicted, actual):
    counts = {"tp": 0, "fp": 0, "fn": 0, "tn": 0}
    for p, a in zip(predicted, actual):
        counts[("tp" if a else "fp") if p else ("fn" if a else "tn")] += 1
    return counts


def precision_recall(counts):
    return counts["tp"] / (counts["tp"] + counts["fp"]), counts["tp"] / (counts["tp"] + counts["fn"])
