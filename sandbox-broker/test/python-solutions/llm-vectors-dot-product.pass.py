import math


def _same_length(a, b):
    if len(a) != len(b):
        raise ValueError("vectors must have the same length")


def add(a, b):
    _same_length(a, b)
    return [x + y for x, y in zip(a, b)]


def scale(k, a):
    return [k * x for x in a]


def dot(a, b):
    _same_length(a, b)
    return sum(x * y for x, y in zip(a, b))


def norm(a):
    return math.sqrt(dot(a, a))


def cosine_similarity(a, b):
    _same_length(a, b)
    length = norm(a) * norm(b)
    if length == 0:
        raise ValueError("a zero vector has no direction")
    return dot(a, b) / length
