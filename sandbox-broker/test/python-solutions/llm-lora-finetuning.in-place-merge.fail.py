# expect: merge_lora must return a new matrix and leave W unchanged
def lora_parameters(d_in, d_out, rank):
    return rank * (d_in + d_out)


def adapter_parameters(shapes, rank):
    return sum(lora_parameters(d_in, d_out, rank) for d_in, d_out in shapes)


def _matvec(M, x):
    return [sum(a * b for a, b in zip(row, x)) for row in M]


def lora_forward(W, A, B, x, alpha, rank):
    base = _matvec(W, x)
    update = _matvec(B, _matvec(A, x))
    scale = alpha / rank
    return [b + scale * u for b, u in zip(base, update)]


def merge_lora(W, A, B, alpha, rank):
    scale = alpha / rank
    for i in range(len(W)):
        for j in range(len(W[0])):
            W[i][j] += scale * sum(B[i][k] * A[k][j] for k in range(rank))
    return W


def trainable_fraction(total_params, shapes, rank):
    return adapter_parameters(shapes, rank) / total_params
