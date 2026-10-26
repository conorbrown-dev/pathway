def derivative(f, x, h=1e-5):
    return (f(x + h) - f(x - h)) / (2 * h)


def gradient(f, point, h=1e-5):
    partials = []
    for index in range(len(point)):
        up = list(point)
        down = list(point)
        up[index] += h
        down[index] -= h
        partials.append((f(up) - f(down)) / (2 * h))
    return partials


def chain_derivative(outer_rate, inner, inner_rate, x):
    return outer_rate(inner(x)) * inner_rate(x)


def tangent_line(f, x0):
    slope = derivative(f, x0)
    return slope, f(x0) - slope * x0
