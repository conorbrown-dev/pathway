# expect: Step against the gradient
def mse_gradients(w, b, xs, ys):
    errors = [w * x + b - y for x, y in zip(xs, ys)]
    n = len(xs)
    return sum(2 * e * x for e, x in zip(errors, xs)) / n, sum(2 * e for e in errors) / n


def step(w, b, dw, db, learning_rate):
    return w + learning_rate * dw, b + learning_rate * db


def fit_line(xs, ys, learning_rate, steps):
    return 0.0, 0.0, []
