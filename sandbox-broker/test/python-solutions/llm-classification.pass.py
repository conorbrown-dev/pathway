import math


def sigmoid(z):
    if z >= 0:
        return 1 / (1 + math.exp(-z))
    e = math.exp(z)
    return e / (1 + e)


def train_logistic(rows, labels, learning_rate, steps):
    weights = [0.0] * len(rows[0])
    bias = 0.0
    n = len(rows)
    for _ in range(steps):
        errors = [sigmoid(sum(w * x for w, x in zip(weights, row)) + bias) - y for row, y in zip(rows, labels)]
        gradient = [sum(e * row[j] for e, row in zip(errors, rows)) / n for j in range(len(weights))]
        weights = [w - learning_rate * g for w, g in zip(weights, gradient)]
        bias -= learning_rate * sum(errors) / n
    return weights, bias


def confusion_counts(predicted, actual):
    counts = {"tp": 0, "fp": 0, "fn": 0, "tn": 0}
    for p, a in zip(predicted, actual):
        key = ("tp" if a else "fp") if p else ("fn" if a else "tn")
        counts[key] += 1
    return counts


def precision_recall(counts):
    flagged = counts["tp"] + counts["fp"]
    positive = counts["tp"] + counts["fn"]
    return (counts["tp"] / flagged if flagged else 0.0, counts["tp"] / positive if positive else 0.0)
