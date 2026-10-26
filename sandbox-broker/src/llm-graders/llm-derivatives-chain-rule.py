# tests: 4
def main():
    ns = load()
    derivative, gradient, chain, tangent = (need(ns, n) for n in ("derivative", "gradient", "chain_derivative", "tangent_line"))

    check(close(call(derivative, lambda x: x * x, 3.0), 6.0, 1e-4), "derivative of x*x at 3 should be about 6.")
    check(close(call(derivative, math.exp, 1.0), math.e, 1e-4), "derivative of exp at 1 should be about e: exp is its own derivative.")
    check(close(call(derivative, math.log, 2.0), 0.5, 1e-4), "derivative of log at 2 should be about 1/2.")
    cube = lambda x: x ** 3
    check(close(call(derivative, cube, 2.0, 1e-3), 12.000001, 1e-9), "Use the central difference (f(x+h) - f(x-h)) / (2h); a one-sided difference is less accurate. For x**3 at 2 with h=0.001 it gives exactly 12.000001.")
    test("central difference")

    f = lambda p: p[0] ** 2 + 3 * p[0] * p[1] + p[1] ** 3
    point = [1.0, 2.0]
    g = call(gradient, f, point)
    check(point == [1.0, 2.0], "gradient changed the point it was given. Nudge a copy instead.")
    check(close_list(g, [8.0, 15.0], 1e-5), f"gradient of a^2 + 3ab + b^3 at (1, 2) should be about [8, 15]; got {show(g)}.")
    check(close_list(call(gradient, lambda p: sum(p), [0.0, 0.0, 0.0]), [1.0, 1.0, 1.0], 1e-6), "gradient of a + b + c is [1, 1, 1].")
    test("gradient with copies")

    value = call(chain, lambda u: 2 * u, lambda x: 3 * x + 1, lambda x: 3.0, 1.0)
    check(close(value, 24.0), "For y = (3x + 1)^2 at x = 1, the chain rule gives 2u * 3 = 8 * 3 = 24.")
    value = call(chain, lambda u: math.exp(u), lambda x: x * x, lambda x: 2 * x, 1.5)
    check(close(value, math.exp(2.25) * 3.0, 1e-9), "For y = exp(x^2), dy/dx = exp(x^2) * 2x.")
    test("chain rule")

    slope, intercept = call(tangent, lambda x: x * x, 3.0)
    check(close(slope, 6.0, 1e-5) and close(intercept, -9.0, 1e-4), f"tangent_line(x*x, 3) should be slope 6, intercept -9 (the line y = 6x - 9); got {show((slope, intercept))}.")
    test("tangent line")


run(main)
