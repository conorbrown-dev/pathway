# expect: With temperature 0, sample_next should return the argmax
import math


def apply_temperature(logits, temperature):
    return [z / temperature for z in logits]


def _ranked(logits):
    return sorted(range(len(logits)), key=lambda i: (-logits[i], i))


def _softmax(logits):
    m = max(logits)
    exps = [math.exp(z - m) for z in logits]
    total = sum(exps)
    return [e / total for e in exps]


def top_k_filter(logits, k):
    keep = set(_ranked(logits)[:k])
    return [z if i in keep else -math.inf for i, z in enumerate(logits)]


def top_p_filter(logits, p):
    probs = _softmax(logits)
    keep, running = set(), 0.0
    for i in _ranked(logits):
        keep.add(i)
        running += probs[i]
        if running >= p:
            break
    return [z if i in keep else -math.inf for i, z in enumerate(logits)]


def sample_next(logits, temperature, top_k, top_p, u):
    if temperature == 0:
        temperature = 1.0  # bug: treats 0 as the model's own distribution
    z = apply_temperature(logits, temperature)
    if top_k is not None:
        z = top_k_filter(z, top_k)
    if top_p is not None:
        z = top_p_filter(z, top_p)
    running = 0.0
    probs = _softmax(z)
    for i, p in enumerate(probs):
        running += p
        if u < running:
            return i
    return len(probs) - 1
