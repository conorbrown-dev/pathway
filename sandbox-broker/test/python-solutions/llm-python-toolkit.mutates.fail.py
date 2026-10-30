# expect: shuffled changed the list it was given
import random


def mean(values):
    if not values:
        raise ValueError("empty")
    return sum(values) / len(values)


def argmax(values):
    return values.index(max(values))


def word_counts(text):
    counts = {}
    for word in text.lower().split():
        counts[word] = counts.get(word, 0) + 1
    return counts


def shuffled(items, seed):
    random.Random(seed).shuffle(items)
    return items
