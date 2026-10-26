# expect: Did you apply bias correction with t = 1?
import math
import random


def minibatches(n, batch_size, seed):
    order = list(range(n))
    random.Random(seed).shuffle(order)
    return [order[i:i + batch_size] for i in range(0, n, batch_size)]


def adam_step(params, grads, state, learning_rate, beta1=0.9, beta2=0.999, eps=1e-8):
    if not state:
        state["t"] = 0
        state["m"] = [0.0] * len(params)
        state["v"] = [0.0] * len(params)
    state["t"] += 1
    t = state["t"]
    state["m"] = [beta1 * m + (1 - beta1) * g for m, g in zip(state["m"], grads)]
    state["v"] = [beta2 * v + (1 - beta2) * g * g for v, g in zip(state["v"], grads)]
    new = []
    for p, m, v in zip(params, state["m"], state["v"]):
        m_hat = m
        v_hat = v
        new.append(p - learning_rate * m_hat / (math.sqrt(v_hat) + eps))
    return new


def clip_by_global_norm(grads, max_norm):
    total = math.sqrt(sum(g * g for g in grads))
    if total <= max_norm or total == 0:
        return list(grads)
    scale = max_norm / total
    return [g * scale for g in grads]


def warmup_cosine(step, total_steps, warmup_steps, peak_lr, min_lr):
    if step < warmup_steps:
        return peak_lr * step / warmup_steps
    progress = (step - warmup_steps) / (total_steps - warmup_steps)
    return min_lr + (peak_lr - min_lr) * (1 + math.cos(math.pi * progress)) / 2
