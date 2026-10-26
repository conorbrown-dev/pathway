# tests: 4
def main():
    ns = load()
    predict, predict_all, mae, best_bias = (need(ns, n) for n in ("predict", "predict_all", "mean_absolute_error", "best_bias"))

    check(close(call(predict, [12, 30], 10, [5, 2]), 130), "predict([12, 30], 10, [5, 2]) should be 12*5 + 30*2 + 10 = 130.")
    check(close(call(predict, [0.5, -2.0, 1.0], 0.0, [4, 1, 3]), 3.0), "predict([0.5, -2, 1], 0, [4, 1, 3]) should be 3.0.")
    check(close(call(predict, [], 7, []), 7), "With no features, the prediction is just the bias.")
    check(raises(predict, ValueError, [1, 2], 0, [1]), "predict should raise ValueError when weights and features differ in length.")
    test("weighted sum plus bias")

    rows = [[5, 2], [1, 1], [3, 3]]
    check(close_list(call(predict_all, [12, 30], 10, rows), [130, 52, 136]), "predict_all should return one prediction per row: [130, 52, 136].")
    check(call(predict_all, [12, 30], 10, []) == [], "predict_all with no rows should return [].")
    test("predictions for many rows")

    check(close(call(mae, [130, 52, 136], [136, 50, 135]), 3.0), "mean_absolute_error([130, 52, 136], [136, 50, 135]) should be (6 + 2 + 1) / 3 = 3.")
    check(close(call(mae, [1, 2], [3, 0]), 2.0), "Absolute error ignores the sign: [1, 2] vs [3, 0] is 2.")
    check(raises(mae, ValueError, [], []), "mean_absolute_error([], []) should raise ValueError.")
    check(raises(mae, ValueError, [1], [1, 2]), "mean_absolute_error should raise ValueError for lists of different lengths.")
    test("mean absolute error")

    targets = [136, 50, 135]
    check(call(best_bias, [12, 30], rows, targets, [0, 5, 10, 15, 20]) == 10, "best_bias should pick 10 from [0, 5, 10, 15, 20].")
    check(call(best_bias, [1], [[0], [0]], [4, 6], [9, 5, 1]) == 5, "best_bias should pick the lowest error, not the first candidate.")
    check(call(best_bias, [1], [[0], [0]], [4, 6], [4, 6]) == 4, "On a tie, best_bias should keep the earliest candidate.")
    test("search picks the best bias")


run(main)
