# expect: gradient changed the point it was given
def derivative(f, x, h=1e-5):
    return (f(x + h) - f(x - h)) / (2 * h)


def gradient(f, point, h=1e-5):
    partials = []
    for index in range(len(point)):
        original = point[index]
        point[index] = original + h
        up = f(point)
        point[index] = original - h
        down = f(point)
        partials.append((up - down) / (2 * h))
        if index < len(point) - 1:
            point[index] = original  # bug: the last entry is never restored
    return partials


def chain_derivative(outer_rate, inner, inner_rate, x):
    return outer_rate(inner(x)) * inner_rate(x)


def tangent_line(f, x0):
    slope = derivative(f, x0)
    return slope, f(x0) - slope * x0
