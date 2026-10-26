# expect: A u just below 1 must still return an outcome
def normalize(counts):
    total = sum(counts.values())
    if total <= 0:
        raise ValueError("empty")
    return {k: v / total for k, v in counts.items()}


def sample(probabilities, u):
    running = 0.0
    for outcome, probability in probabilities.items():
        running += probability
        if u < running:
            return outcome
    return None


def next_word_distribution(words):
    return {}


def expected_value(probabilities):
    return 0
