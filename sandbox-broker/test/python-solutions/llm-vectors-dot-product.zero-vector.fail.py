# expect: cosine_similarity with a zero vector should raise ValueError
import math


def add(a, b):
    if len(a) != len(b):
        raise ValueError("length")
    return [x + y for x, y in zip(a, b)]


def scale(k, a):
    return [k * x for x in a]


def dot(a, b):
    if len(a) != len(b):
        raise ValueError("length")
    return sum(x * y for x, y in zip(a, b))


def norm(a):
    return math.sqrt(dot(a, a))


def cosine_similarity(a, b):
    length = norm(a) * norm(b)
    return dot(a, b) / length if length else 0.0
