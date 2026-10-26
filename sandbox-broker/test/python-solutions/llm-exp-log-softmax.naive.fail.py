# expect: softmax([1000, 1001]) crashed. Subtract the largest score
import math


def softmax(scores):
    exps = [math.exp(s) for s in scores]
    total = sum(exps)
    return [e / total for e in exps]


def log_sum_exp(scores):
    return math.log(sum(math.exp(s) for s in scores))


def log_softmax(scores):
    return [math.log(p) for p in softmax(scores)]


def sequence_log_prob(probabilities):
    product = 1.0
    for p in probabilities:
        product *= p
    return math.log(product)
