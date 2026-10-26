def get_pair_counts(ids):
    counts = {}
    for pair in zip(ids, ids[1:]):
        counts[pair] = counts.get(pair, 0) + 1
    return counts


def merge(ids, pair, new_id):
    out, i = [], 0
    while i < len(ids):
        if i + 1 < len(ids) and ids[i] == pair[0] and ids[i + 1] == pair[1]:
            out.append(new_id)
            i += 2
        else:
            out.append(ids[i])
            i += 1
    return out


def train_bpe(text, num_merges):
    ids = list(text.encode("utf-8"))
    merges = []
    for step in range(num_merges):
        counts = get_pair_counts(ids)
        if not counts:
            break
        pair = max(counts, key=counts.get)
        if counts[pair] < 2:
            break
        new_id = 256 + step
        ids = merge(ids, pair, new_id)
        merges.append((pair, new_id))
    return merges


def encode(text, merges):
    ids = list(text.encode("utf-8"))
    for pair, new_id in merges:
        ids = merge(ids, pair, new_id)
    return ids


def decode(ids, merges):
    vocab = {i: bytes([i]) for i in range(256)}
    for (a, b), new_id in merges:
        vocab[new_id] = vocab[a] + vocab[b]
    return b"".join(vocab[i] for i in ids).decode("utf-8")
