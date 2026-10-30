# expect: number labels in order of first appearance
def one_hot(index, size):
    if not 0 <= index < size:
        raise ValueError("bad index")
    vector = [0] * size
    vector[index] = 1
    return vector


def build_vocabulary(labels):
    return {label: index for index, label in enumerate(sorted(set(labels)))}


def encode_labels(labels, vocabulary):
    return [one_hot(vocabulary[label], len(vocabulary)) for label in labels]


def standardize(values):
    return list(values)
