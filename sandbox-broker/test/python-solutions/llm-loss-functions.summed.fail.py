# expect: expected the mean of the three examples' losses
import math


def mean_squared_error(predictions, targets):
    if not predictions or len(predictions) != len(targets):
        raise ValueError("bad input")
    return sum((p - t) ** 2 for p, t in zip(predictions, targets)) / len(predictions)


def cross_entropy(probabilities, correct):
    p = probabilities[correct]
    return math.inf if p == 0 else -math.log(p)


def cross_entropy_from_logits(logits, correct):
    m = max(logits)
    return m + math.log(sum(math.exp(z - m) for z in logits)) - logits[correct]


def average_cross_entropy(batch_logits, targets):
    return sum(cross_entropy_from_logits(z, t) for z, t in zip(batch_logits, targets))
