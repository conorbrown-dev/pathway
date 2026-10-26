def predict(weights, bias, features):
    if len(weights) != len(features):
        raise ValueError("weights and features must have the same length")
    return sum(w * x for w, x in zip(weights, features)) + bias


def predict_all(weights, bias, rows):
    return [predict(weights, bias, row) for row in rows]


def mean_absolute_error(predictions, targets):
    if not predictions or len(predictions) != len(targets):
        raise ValueError("need two non-empty lists of the same length")
    return sum(abs(p - t) for p, t in zip(predictions, targets)) / len(predictions)


def best_bias(weights, rows, targets, candidates):
    best, best_error = None, None
    for bias in candidates:
        error = mean_absolute_error(predict_all(weights, bias, rows), targets)
        if best_error is None or error < best_error:
            best, best_error = bias, error
    return best
