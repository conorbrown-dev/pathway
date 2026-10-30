# tests: 4
def numeric(build, xs, index, h=1e-6):
    Value = NS["Value"]
    up = [Value(x + (h if i == index else 0)) for i, x in enumerate(xs)]
    down = [Value(x - (h if i == index else 0)) for i, x in enumerate(xs)]
    return (build(*up).data - build(*down).data) / (2 * h)


def gradients(build, xs):
    Value = NS["Value"]
    values = [Value(x) for x in xs]
    out = build(*values)
    out.backward()
    return out, [v.grad for v in values]


NS = {}


def main():
    NS.update(load())
    Value = need(NS, "Value")

    a, b = Value(2.0), Value(-3.0)
    try:
        checks = [
            (a * b + 1 - a).data, (2 * a).data, (a + 1).data, (1 - a).data, (-a).data, (a ** 2).data, (a ** 0.5).data, (a - b).data,
            Value(0.5).tanh().data, Value(-1.0).relu().data, Value(1.5).relu().data, Value(1.0).exp().data,
        ]
    except Exception as error:  # noqa: BLE001
        fail(f"Building expressions with Value raised {type(error).__name__}: {error}. Support +, -, *, ** and unary minus with numbers on either side.")
    expected = [-7.0, 4.0, 3.0, -1.0, -2.0, 4.0, math.sqrt(2.0), 5.0, math.tanh(0.5), 0.0, 1.5, math.e]
    check(close_list(checks, expected, 1e-9), f"Forward values were {show([round(c, 4) for c in checks])}; expected {show([round(e, 4) for e in expected])}.")
    test("arithmetic builds the right values")

    _, (ga,) = gradients(lambda a: a * a, [3.0])
    check(close(ga, 6.0), f"For a * a at a = 3, a.grad should be 6 (3 + 3, one per path); got {ga}. Use += so both paths add up.")
    _, (ga,) = gradients(lambda a: a + a, [5.0])
    check(close(ga, 2.0), f"For a + a, a.grad should be 2; got {ga}.")

    def diamond(a):
        b = a * 2
        c = a * 3
        return b * c
    _, (ga,) = gradients(diamond, [1.5])
    check(close(ga, 18.0), f"For b = 2a, c = 3a, d = b * c at a = 1.5, a.grad should be 12a = 18; got {ga}.")

    def shared(a):
        b = a * 2
        c = b * 3
        return b + c
    _, (ga,) = gradients(shared, [1.0])
    check(close(ga, 8.0), f"For b = 2a, c = 3b, d = b + c, a.grad should be 8; got {ga}. A node must run its backward step only after every node that used it has finished (reverse topological order), and only once.")
    test("values used twice accumulate")

    cases = [
        ("a * b + a", lambda a, b: a * b + a, [2.0, 3.0]),
        ("(a * b).tanh()", lambda a, b: (a * b).tanh(), [0.3, -0.7]),
        ("(a + b) ** 3 - 2 * a", lambda a, b: (a + b) ** 3 - 2 * a, [1.2, 0.4]),
        ("(a * 3 - b).relu() * b", lambda a, b: (a * 3 - b).relu() * b, [1.0, 0.5]),
        ("(a * b).exp() * (1 - a)", lambda a, b: (a * b).exp() * (1 - a), [0.2, 0.9]),
        ("a ** 0.5 + b ** -1", lambda a, b: a ** 0.5 + b ** -1, [4.0, 2.0]),
    ]
    for label, build, xs in cases:
        try:
            _, grads = gradients(build, xs)
        except Exception as error:  # noqa: BLE001
            fail(f"backward() on {label} raised {type(error).__name__}: {error}.")
        for i in range(len(xs)):
            want = numeric(build, xs, i)
            check(close(grads[i], want, 1e-4), f"For {label} at {xs}, the gradient of input {i} is {grads[i]:.5f} but a numerical check gives {want:.5f}.")
    test("gradients match numerical estimates")

    rng = random.Random(2)
    data = [([rng.uniform(-1, 1) for _ in range(3)], rng.choice([-1.0, 1.0])) for _ in range(8)]
    weights = [Value(rng.uniform(-0.5, 0.5)) for _ in range(3)]
    bias = Value(0.0)

    def total_loss():
        loss = Value(0.0)
        for x, y in data:
            out = (sum((w * xi for w, xi in zip(weights, x)), bias)).tanh()
            loss = loss + (out - y) ** 2
        return loss

    first = None
    for _ in range(60):
        loss = total_loss()
        first = loss.data if first is None else first
        for p in weights + [bias]:
            p.grad = 0.0
        loss.backward()
        for p in weights + [bias]:
            p.data -= 0.05 * p.grad
    last = total_loss().data
    check(last < first * 0.8, f"Training a tanh neuron with your engine should cut the loss noticeably (from {first:.3f}); it ended at {last:.3f}.")
    test("a small neuron trains with your engine")


run(main)
