def possible_contexts(vocab_size, n):
    return vocab_size ** (n - 1)


def contexts(tokens, n):
    return [tuple(tokens[i:i + n - 1]) for i in range(len(tokens)) if i + n - 1 < len(tokens)]


def unseen_fraction(train_tokens, test_tokens, n):
    seen = set(contexts(train_tokens, n))
    test = contexts(test_tokens, n)
    return sum(context not in seen for context in test) / len(test)


def sparsity_curve(train_tokens, test_tokens, max_n):
    return [unseen_fraction(train_tokens, test_tokens, n) for n in range(2, max_n + 1)]
