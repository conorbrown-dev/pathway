# expect: should be (-13, -8)
def mse_gradients(w, b, xs, ys):
    errors = [w * x + b - y for x, y in zip(xs, ys)]
    n = len(xs)
    return sum(e * x for e, x in zip(errors, xs)) / n, sum(errors) / n


def step(w, b, dw, db, learning_rate):
    return w - learning_rate * dw, b - learning_rate * db


def fit_line(xs, ys, learning_rate, steps):
    w, b, losses = 0.0, 0.0, []
    for _ in range(steps):
        losses.append(sum((w * x + b - y) ** 2 for x, y in zip(xs, ys)) / len(xs))
        w, b = step(w, b, *mse_gradients(w, b, xs, ys), learning_rate)
    return w, b, losses
