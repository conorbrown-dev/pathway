import math


def relu(v):
    return v if v > 0 else 0.0


ACTIVATIONS = {"relu": relu, "tanh": math.tanh, "none": lambda v: v}


def dense(weights, biases, inputs, activation):
    if activation not in ACTIVATIONS:
        raise ValueError(f"unsupported activation {activation!r}")
    if any(len(row) != len(inputs) for row in weights) or len(weights) != len(biases):
        raise ValueError("shape mismatch")
    f = ACTIVATIONS[activation]
    return [f(sum(w * x for w, x in zip(row, inputs)) + b) for row, b in zip(weights, biases)]


def mlp_forward(layers, x):
    for weights, biases, activation in layers:
        x = dense(weights, biases, x, activation)
    return x


def count_parameters(layer_sizes):
    return sum(n * m + m for n, m in zip(layer_sizes, layer_sizes[1:]))


def xor_network():
    return [([[1, 1], [1, 1]], [0, -1], "relu"), ([[1, -2]], [0], "none")]
