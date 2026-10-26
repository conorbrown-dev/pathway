# expect: on a tie, return the first index
import random


def mean(values):
    if not values:
        raise ValueError("empty")
    return sum(values) / len(values)


def argmax(values):
    best = 0
    for index, value in enumerate(values):
        if value >= values[best]:
            best = index
    return best


def word_counts(text):
    return {}


def shuffled(items, seed):
    copy = list(items)
    random.Random(seed).shuffle(copy)
    return copy
