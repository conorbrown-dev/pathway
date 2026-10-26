import random


def mean(values):
    if not values:
        raise ValueError("mean of an empty list is undefined")
    return sum(values) / len(values)


def argmax(values):
    best = 0
    for index, value in enumerate(values):
        if value > values[best]:
            best = index
    return best


def word_counts(text):
    counts = {}
    for word in text.lower().split():
        counts[word] = counts.get(word, 0) + 1
    return counts


def shuffled(items, seed):
    copy = list(items)
    random.Random(seed).shuffle(copy)
    return copy
