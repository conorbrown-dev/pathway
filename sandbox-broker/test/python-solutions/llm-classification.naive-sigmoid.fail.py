# expect: sigmoid crashed for a large score
import math


def sigmoid(z):
    return 1 / (1 + math.exp(-z))


def train_logistic(rows, labels, learning_rate, steps):
    return [0.0] * len(rows[0]), 0.0


def confusion_counts(predicted, actual):
    return {"tp": 0, "fp": 0, "fn": 0, "tn": 0}


def precision_recall(counts):
    return 0.0, 0.0
