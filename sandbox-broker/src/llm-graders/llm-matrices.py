# tests: 4
def main():
    ns = load()
    shape, transpose, matvec, matmul = (need(ns, n) for n in ("shape", "transpose", "matvec", "matmul"))
    a = [[1, 2, 3], [4, 5, 6]]

    check(tuple(call(shape, a)) == (2, 3), "shape([[1, 2, 3], [4, 5, 6]]) should be (2, 3): rows, then columns.")
    check(tuple(call(shape, [[7]])) == (1, 1), "shape([[7]]) should be (1, 1).")
    check(raises(shape, ValueError, [[1, 2], [3]]), "shape should raise ValueError when rows have different lengths.")
    test("shape, including ragged rows")

    t = call(transpose, a)
    check(close_matrix(t, [[1, 4], [2, 5], [3, 6]]), f"transpose gave {show(t)}; expected [[1, 4], [2, 5], [3, 6]].")
    check(a == [[1, 2, 3], [4, 5, 6]], "transpose should not change its input.")
    check(close_matrix(call(transpose, t), a), "Transposing twice should give back the original.")
    test("transpose")

    check(close_list(call(matvec, a, [1, 0, 2]), [7, 16]), "matvec(a, [1, 0, 2]) should be [7, 16].")
    check(close_list(call(matvec, [[3, 2, 4], [2, 3, 5]], [2, 1, 1]), [12, 12]), "The store prices example should give [12, 12].")
    check(raises(matvec, ValueError, a, [1, 2]), "matvec should raise ValueError when the vector length differs from the column count.")
    test("matrix times vector")

    b = [[1, 0], [0, 1], [2, 3]]
    check(close_matrix(call(matmul, a, b), [[7, 11], [16, 23]]), "matmul(a, b) for 2x3 times 3x2 should be [[7, 11], [16, 23]].")
    check(close_matrix(call(matmul, b, a), [[1, 2, 3], [4, 5, 6], [14, 19, 24]]), "matmul(b, a) is a different 3x3 matrix: order matters.")
    identity = [[1, 0, 0], [0, 1, 0], [0, 0, 1]]
    check(close_matrix(call(matmul, a, identity), a), "Multiplying by the identity matrix should change nothing.")
    check(raises(matmul, ValueError, a, a), "matmul of two 2x3 matrices should raise ValueError: the inner dimensions (3 and 2) don't match.")
    rng = random.Random(5)
    x = [[rng.randint(-3, 3) for _ in range(4)] for _ in range(3)]
    y = [[rng.randint(-3, 3) for _ in range(2)] for _ in range(4)]
    expected = [[sum(x[i][k] * y[k][j] for k in range(4)) for j in range(2)] for i in range(3)]
    check(close_matrix(call(matmul, x, y), expected), "matmul gave the wrong answer for a random 3x4 times 4x2 product.")
    test("matrix times matrix and the shape rule")


run(main)
