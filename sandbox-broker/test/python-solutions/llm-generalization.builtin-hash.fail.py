# expect: in the wrong split
def assign_split(example_id, validation_fraction, test_fraction):
    bucket = (hash(example_id) % 1000) / 1000
    if bucket < test_fraction:
        return "test"
    if bucket < test_fraction + validation_fraction:
        return "validation"
    return "train"


def find_leaks(train_texts, test_texts):
    return []


def fit_standardizer(train_values):
    return 0.0, 1.0


def apply_standardizer(values, mean, spread):
    return values


def majority_baseline_accuracy(train_labels, test_labels):
    return 0.0
