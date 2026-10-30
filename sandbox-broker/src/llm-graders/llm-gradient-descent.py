# tests: 4
def reference_mse(w, b, xs, ys):
    return sum((w * x + b - y) ** 2 for x, y in zip(xs, ys)) / len(xs)


def main():
    ns = load()
    grads, step, fit_line = (need(ns, n) for n in ("mse_gradients", "step", "fit_line"))

    dw, db = call(grads, 0.0, 0.0, [1, 2], [3, 5])
    check(close(dw, -13.0) and close(db, -8.0), f"mse_gradients(0, 0, [1, 2], [3, 5]) should be (-13, -8); got {show((dw, db))}.")
    rng = random.Random(4)
    xs = [rng.uniform(-3, 3) for _ in range(12)]
    ys = [rng.uniform(-5, 5) for _ in range(12)]
    for w, b in [(0.5, -1.0), (-2.0, 3.0), (1.7, 0.2)]:
        dw, db = call(grads, w, b, xs, ys)
        h = 1e-6
        num_dw = (reference_mse(w + h, b, xs, ys) - reference_mse(w - h, b, xs, ys)) / (2 * h)
        num_db = (reference_mse(w, b + h, xs, ys) - reference_mse(w, b - h, xs, ys)) / (2 * h)
        check(close(dw, num_dw, 1e-4) and close(db, num_db, 1e-4), f"At w={w}, b={b} your gradient {show((dw, db))} disagrees with the numerical check {show((round(num_dw, 4), round(num_db, 4)))}. Remember the factor 2 and dividing by the number of examples.")
    test("gradients match a numerical check")

    w, b = call(step, 0.0, 0.0, -13.0, -8.0, 0.1)
    check(close(w, 1.3) and close(b, 0.8), f"step(0, 0, -13, -8, 0.1) should give (1.3, 0.8); got {show((w, b))}. Step against the gradient: subtract learning_rate * gradient.")
    test("a single step moves downhill")

    rng = random.Random(9)
    xs = [rng.uniform(-2, 2) for _ in range(40)]
    ys = [2.5 * x - 1.0 + rng.gauss(0, 0.05) for x in xs]
    result = call(fit_line, xs, ys, 0.1, 300)
    check(isinstance(result, tuple) and len(result) == 3, "fit_line should return (w, b, losses).")
    w, b, losses = result
    check(close(w, 2.5, 0.02) and close(b, -1.0, 0.03), f"fit_line should recover about w = 2.5, b = -1.0 from this data; got w={w:.3f}, b={b:.3f}.")
    check(isinstance(losses, list) and len(losses) == 300, "losses should hold one MSE value per step (the loss before that step).")
    check(close(losses[0], reference_mse(0.0, 0.0, xs, ys), 1e-9), "losses[0] should be the MSE at the starting point w = 0, b = 0.")
    check(all(later <= earlier + 1e-12 for earlier, later in zip(losses, losses[1:])), "With a sensible learning rate the loss should never go up.")
    test("the fitted line recovers the true slope and intercept")

    _, _, losses = call(fit_line, xs, ys, 1.5, 30)
    check(losses[-1] > losses[0], "With learning rate 1.5 on this data, steps overshoot and the loss should grow. If it shrinks, check that each step uses the gradient at the current parameters.")
    test("an oversized learning rate diverges")


run(main)
