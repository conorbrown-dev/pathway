# tests: 4
def main():
    ns = load()
    relu, dense, mlp_forward, count_parameters, xor_network = (need(ns, n) for n in ("relu", "dense", "mlp_forward", "count_parameters", "xor_network"))

    check(call(relu, -2.5) == 0 and close(call(relu, 3.25), 3.25) and call(relu, 0.0) == 0, "relu should return 0 for negatives and the value itself otherwise.")
    out = call(dense, [[1, -1], [2, 0]], [0, -1], [3, 4], "relu")
    check(close_list(out, [0, 5]), f"dense with W=[[1, -1], [2, 0]], b=[0, -1], x=[3, 4], ReLU should give [0, 5]; got {show(out)}.")
    check(close_list(call(dense, [[1, -1], [2, 0]], [0, -1], [3, 4], "none"), [-1, 5]), "With activation 'none', negative outputs must be kept.")
    check(close_list(call(dense, [[0.5]], [0.0], [1.0], "tanh"), [math.tanh(0.5)]), "With activation 'tanh', apply math.tanh to each output.")
    check(raises(dense, ValueError, [[1, 2]], [0], [1, 2], "gelu"), "dense should raise ValueError for an activation it doesn't support.")
    check(raises(dense, ValueError, [[1, 2]], [0], [1, 2, 3], "relu"), "dense should raise ValueError when the input length differs from the number of columns.")
    test("relu and a dense layer")

    layers = [([[1, 1], [1, -1]], [0, 0], "relu"), ([[1, 1]], [0.5], "none")]
    check(close_list(call(mlp_forward, layers, [2, 1]), [4.5]), "mlp_forward should feed each layer's output into the next: [2, 1] → [3, 1] → [4.5].")
    check(close_list(call(mlp_forward, [], [7, 8]), [7, 8]), "With no layers, mlp_forward should return the input unchanged.")
    test("forward through several layers")

    check(call(count_parameters, [784, 128, 10]) == 101770, "count_parameters([784, 128, 10]) should be 101,770.")
    check(call(count_parameters, [2, 2, 1]) == 9, "count_parameters([2, 2, 1]) should be 9: (2*2 + 2) + (2*1 + 1).")
    check(call(count_parameters, [3]) == 0, "A single layer size has no connections, so count_parameters([3]) is 0.")
    test("parameter counting")

    network = call(xor_network)
    for (a, b), want in [((0, 0), 0), ((0, 1), 1), ((1, 0), 1), ((1, 1), 0)]:
        value = call(mlp_forward, network, [a, b])
        check(isinstance(value, list) and len(value) == 1, "xor_network's last layer should have exactly one output.")
        ok = value[0] >= 0.9 if want else value[0] <= 0.1
        check(ok, f"xor_network gives {value[0]:.3f} for ({a}, {b}); XOR should be {want}.")
    test("a network that computes XOR")


run(main)
