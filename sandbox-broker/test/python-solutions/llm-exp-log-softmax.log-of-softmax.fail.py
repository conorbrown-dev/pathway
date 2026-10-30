# expect: log_softmax([0, -800])[1] should be about -800
import math


def softmax(scores):
    m = max(scores)
    exps = [math.exp(s - m) for s in scores]
    total = sum(exps)
    return [e / total for e in exps]


def log_sum_exp(scores):
    m = max(scores)
    return m + math.log(sum(math.exp(s - m) for s in scores))


def log_softmax(scores):
    return [math.log(p) if p > 0 else float("-inf") for p in softmax(scores)]


def sequence_log_prob(probabilities):
    return sum(math.log(p) for p in probabilities)
