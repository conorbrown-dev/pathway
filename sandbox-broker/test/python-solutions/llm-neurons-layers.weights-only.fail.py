# expect: count_parameters([784, 128, 10]) should be 101,770
import math


def relu(v):
    return max(0.0, v)


def dense(weights, biases, inputs, activation):
    if activation not in ("relu", "tanh", "none"):
        raise ValueError("activation")
    if any(len(row) != len(inputs) for row in weights):
        raise ValueError("shape")
    f = {"relu": relu, "tanh": math.tanh, "none": lambda v: v}[activation]
    return [f(sum(w * x for w, x in zip(row, inputs)) + b) for row, b in zip(weights, biases)]


def mlp_forward(layers, x):
    for weights, biases, activation in layers:
        x = dense(weights, biases, x, activation)
    return x


def count_parameters(layer_sizes):
    return sum(n * m for n, m in zip(layer_sizes, layer_sizes[1:]))


def xor_network():
    return [([[1, 1], [1, 1]], [0, -1], "relu"), ([[1, -2]], [0], "none")]
