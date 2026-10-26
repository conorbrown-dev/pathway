# expect: the inner dimensions (3 and 2) don't match
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
    return [[sum(x * y for x, y in zip(row, column)) for column in zip(*b)] for row in a]
