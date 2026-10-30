def normalize(counts):
    total = sum(counts.values())
    if total <= 0:
        raise ValueError("counts must add up to more than zero")
    return {outcome: count / total for outcome, count in counts.items()}


def sample(probabilities, u):
    running = 0.0
    last = None
    for outcome, probability in probabilities.items():
        running += probability
        last = outcome
        if u < running:
            return outcome
    return last


def next_word_distribution(words):
    counts = {}
    for current, following in zip(words, words[1:]):
        inner = counts.setdefault(current, {})
        inner[following] = inner.get(following, 0) + 1
    return {word: normalize(inner) for word, inner in counts.items()}


def expected_value(probabilities):
    return sum(value * probability for value, probability in probabilities.items())
