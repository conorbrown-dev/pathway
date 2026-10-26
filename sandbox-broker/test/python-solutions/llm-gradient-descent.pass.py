def mse_gradients(w, b, xs, ys):
    errors = [w * x + b - y for x, y in zip(xs, ys)]
    n = len(xs)
    dw = sum(2 * e * x for e, x in zip(errors, xs)) / n
    db = sum(2 * e for e in errors) / n
    return dw, db


def step(w, b, dw, db, learning_rate):
    return w - learning_rate * dw, b - learning_rate * db


def fit_line(xs, ys, learning_rate, steps):
    w, b = 0.0, 0.0
    losses = []
    for _ in range(steps):
        losses.append(sum((w * x + b - y) ** 2 for x, y in zip(xs, ys)) / len(xs))
        dw, db = mse_gradients(w, b, xs, ys)
        w, b = step(w, b, dw, db, learning_rate)
    return w, b, losses
