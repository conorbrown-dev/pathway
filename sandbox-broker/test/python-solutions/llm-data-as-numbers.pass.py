import math


def one_hot(index, size):
    if not 0 <= index < size:
        raise ValueError(f"index {index} is outside 0..{size - 1}")
    vector = [0] * size
    vector[index] = 1
    return vector


def build_vocabulary(labels):
    vocabulary = {}
    for label in labels:
        if label not in vocabulary:
            vocabulary[label] = len(vocabulary)
    return vocabulary


def encode_labels(labels, vocabulary):
    return [one_hot(vocabulary[label], len(vocabulary)) for label in labels]


def standardize(values):
    mean = sum(values) / len(values)
    variance = sum((value - mean) ** 2 for value in values) / len(values)
    spread = math.sqrt(variance)
    if spread == 0:
        return [0.0 for _ in values]
    return [(value - mean) / spread for value in values]
