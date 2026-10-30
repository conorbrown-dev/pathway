# expect: u = 0.70 is the start of rare's stretch
def normalize(counts):
    total = sum(counts.values())
    if total <= 0:
        raise ValueError("empty")
    return {k: v / total for k, v in counts.items()}


def sample(probabilities, u):
    running = 0.0
    last = None
    for outcome, probability in probabilities.items():
        running += probability
        last = outcome
        if u <= running:
            return outcome
    return last


def next_word_distribution(words):
    return {}


def expected_value(probabilities):
    return 0
