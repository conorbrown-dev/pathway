def shape(matrix):
    rows = len(matrix)
    columns = len(matrix[0]) if rows else 0
    if any(len(row) != columns for row in matrix):
        raise ValueError("rows have different lengths")
    return (rows, columns)


def transpose(matrix):
    return [list(column) for column in zip(*matrix)]


def _dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def matvec(matrix, vector):
    _, columns = shape(matrix)
    if columns != len(vector):
        raise ValueError(f"matrix has {columns} columns but the vector has {len(vector)} entries")
    return [_dot(row, vector) for row in matrix]


def matmul(a, b):
    _, inner = shape(a)
    rows_b, _ = shape(b)
    if inner != rows_b:
        raise ValueError(f"cannot multiply: inner dimensions {inner} and {rows_b} differ")
    columns = transpose(b)
    return [[_dot(row, column) for column in columns] for row in a]
