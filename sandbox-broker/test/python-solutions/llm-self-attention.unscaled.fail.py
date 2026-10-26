# expect: divided by √2
import math


def attention_scores(Q, K):
    d = len(Q[0])
    return [[sum(a * b for a, b in zip(q, k)) for k in K] for q in Q]


def causal_mask(scores):
    return [[value if j <= i else -math.inf for j, value in enumerate(row)] for i, row in enumerate(scores)]


def row_softmax(scores):
    out = []
    for row in scores:
        m = max(row)
        exps = [math.exp(v - m) for v in row]
        total = sum(exps)
        out.append([e / total for e in exps])
    return out


def attention(Q, K, V, causal):
    scores = attention_scores(Q, K)
    if causal:
        scores = causal_mask(scores)
    weights = row_softmax(scores)
    output = [[sum(w[j] * V[j][c] for j in range(len(V))) for c in range(len(V[0]))] for w in weights]
    return output, weights
