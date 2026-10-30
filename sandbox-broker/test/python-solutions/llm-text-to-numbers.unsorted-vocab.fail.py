# expect: char_vocabulary('banana!') should number characters in sorted order
def utf8_encode_char(code_point):
    if code_point < 128:
        return [code_point]
    if code_point < 2048:
        count, lead = 1, 192
    elif code_point < 65536:
        count, lead = 2, 224
    else:
        count, lead = 3, 240
    continuation = []
    for _ in range(count):
        continuation.insert(0, 128 + code_point % 64)
        code_point //= 64
    return [lead + code_point] + continuation


def utf8_encode(text):
    out = []
    for ch in text:
        out.extend(utf8_encode_char(ord(ch)))
    return out


def char_vocabulary(text):
    vocab = {}
    for ch in text:
        vocab.setdefault(ch, len(vocab))
    return vocab


def encode_chars(text, vocabulary):
    return [vocabulary[ch] for ch in text]


def decode_chars(ids, vocabulary):
    inverse = {i: ch for ch, i in vocabulary.items()}
    return "".join(inverse[i] for i in ids)
