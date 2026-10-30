# expect: Divide by the count (population standard deviation)
import math


def one_hot(index, size):
    if not 0 <= index < size:
        raise ValueError("bad index")
    vector = [0] * size
    vector[index] = 1
    return vector


def build_vocabulary(labels):
    vocabulary = {}
    for label in labels:
        vocabulary.setdefault(label, len(vocabulary))
    return vocabulary


def encode_labels(labels, vocabulary):
    return [one_hot(vocabulary[label], len(vocabulary)) for label in labels]


def standardize(values):
    mean = sum(values) / len(values)
    spread = math.sqrt(sum((value - mean) ** 2 for value in values) / (len(values) - 1))
    if spread == 0:
        return [0.0 for _ in values]
    return [(value - mean) / spread for value in values]
