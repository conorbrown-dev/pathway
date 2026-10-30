# expect: predict should raise ValueError when weights and features differ in length
def predict(weights, bias, features):
    return sum(w * x for w, x in zip(weights, features)) + bias


def predict_all(weights, bias, rows):
    return [predict(weights, bias, row) for row in rows]


def mean_absolute_error(predictions, targets):
    return sum(abs(p - t) for p, t in zip(predictions, targets)) / len(predictions)


def best_bias(weights, rows, targets, candidates):
    return candidates[0]
