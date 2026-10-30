# tests: 4
def matvec(M, x):
    return [sum(a * b for a, b in zip(row, x)) for row in M]


def main():
    ns = load()
    lora_parameters, adapter_parameters, lora_forward, merge_lora, trainable_fraction = (need(ns, n) for n in ("lora_parameters", "adapter_parameters", "lora_forward", "merge_lora", "trainable_fraction"))

    check(call(lora_parameters, 2048, 2048, 16) == 65536, "lora_parameters(2048, 2048, 16) should be 16 × (2048 + 2048) = 65,536.")
    check(call(lora_parameters, 2048, 8192, 8) == 8 * (2048 + 8192), "lora_parameters should use rank × (d_in + d_out).")
    check(call(adapter_parameters, [(2048, 2048), (2048, 8192), (8192, 2048)], 16) == 16 * (4096 + 10240 + 10240), "adapter_parameters should sum over every shape.")
    test("parameter counting")

    W = [[1.0, 0.0], [0.0, 1.0]]
    A = [[1.0, 1.0]]
    B = [[0.5], [0.0]]
    out = call(lora_forward, W, A, B, [2.0, 4.0], 2.0, 1)
    check(close_list(out, [8.0, 4.0]), f"The lesson's example should give [8, 4]; got {show(out)}.")
    zeroB = [[0.0], [0.0]]
    check(close_list(call(lora_forward, W, A, zeroB, [2.0, 4.0], 16.0, 1), [2.0, 4.0]), "With B all zeros (as at the start of training), the output must equal W·x.")
    test("forward pass through the adapter")

    rng = random.Random(14)
    d_in, d_out, rank = 5, 4, 2
    W = [[rng.uniform(-1, 1) for _ in range(d_in)] for _ in range(d_out)]
    A = [[rng.uniform(-1, 1) for _ in range(d_in)] for _ in range(rank)]
    B = [[rng.uniform(-1, 1) for _ in range(rank)] for _ in range(d_out)]
    original = [list(row) for row in W]
    merged = call(merge_lora, W, A, B, 8.0, rank)
    check(W == original, "merge_lora must return a new matrix and leave W unchanged.")
    expected = [[W[i][j] + 4.0 * sum(B[i][k] * A[k][j] for k in range(rank)) for j in range(d_in)] for i in range(d_out)]
    check(close_matrix(merged, expected, 1e-9), "merge_lora should return W + (alpha / rank)·B·A.")
    for _ in range(3):
        x = [rng.uniform(-2, 2) for _ in range(d_in)]
        check(close_list(matvec(merged, x), call(lora_forward, W, A, B, x, 8.0, rank), 1e-9), "The merged matrix must give exactly the same outputs as the adapter's forward pass.")
    test("merging leaves W alone and matches the adapter")

    check(close(call(trainable_fraction, 1_700_000_000, [(2048, 2048)] * 4 * 28, 16), 16 * 4096 * 112 / 1.7e9, 1e-9), "trainable_fraction should divide adapter_parameters by the model's total parameter count.")
    test("trainable fraction")


run(main)
