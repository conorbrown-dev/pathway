# expect: dot should raise ValueError for different lengths
import math


def add(a, b):
    if len(a) != len(b):
        raise ValueError("length")
    return [x + y for x, y in zip(a, b)]


def scale(k, a):
    return [k * x for x in a]


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def norm(a):
    return math.sqrt(dot(a, a))


def cosine_similarity(a, b):
    return dot(a, b) / (norm(a) * norm(b))
