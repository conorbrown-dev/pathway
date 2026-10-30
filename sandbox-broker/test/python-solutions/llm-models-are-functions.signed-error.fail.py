# expect: should be (6 + 2 + 1) / 3 = 3
def predict(weights, bias, features):
    if len(weights) != len(features):
        raise ValueError("length")
    return sum(w * x for w, x in zip(weights, features)) + bias


def predict_all(weights, bias, rows):
    return [predict(weights, bias, row) for row in rows]


def mean_absolute_error(predictions, targets):
    if not predictions or len(predictions) != len(targets):
        raise ValueError("length")
    return abs(sum(p - t for p, t in zip(predictions, targets))) / len(predictions)


def best_bias(weights, rows, targets, candidates):
    return min(candidates, key=lambda b: mean_absolute_error(predict_all(weights, b, rows), targets))
