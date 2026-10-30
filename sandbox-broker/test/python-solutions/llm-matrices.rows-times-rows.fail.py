# expect: matmul(a, b) for 2x3 times 3x2 should be [[7, 11], [16, 23]]
def shape(matrix):
    columns = len(matrix[0])
    if any(len(row) != columns for row in matrix):
        raise ValueError("ragged")
    return (len(matrix), columns)


def transpose(matrix):
    return [list(column) for column in zip(*matrix)]


def matvec(matrix, vector):
    if len(matrix[0]) != len(vector):
        raise ValueError("shape")
    return [sum(x * y for x, y in zip(row, vector)) for row in matrix]


def matmul(a, b):
    if len(a[0]) != len(b):
        raise ValueError("shape")
    # Bug: pairs rows of a with rows of b instead of columns of b.
    return [[sum(x * y for x, y in zip(row, other)) for other in b] for row in a]
