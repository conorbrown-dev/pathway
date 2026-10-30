import math
import zlib


def assign_split(example_id, validation_fraction, test_fraction):
    bucket = zlib.crc32(example_id.encode()) / 2 ** 32
    if bucket < test_fraction:
        return "test"
    if bucket < test_fraction + validation_fraction:
        return "validation"
    return "train"


def _normalize(text):
    return " ".join(text.lower().split())


def find_leaks(train_texts, test_texts):
    seen = {_normalize(text) for text in train_texts}
    return sorted({_normalize(text) for text in test_texts} & seen)


def fit_standardizer(train_values):
    mean = sum(train_values) / len(train_values)
    spread = math.sqrt(sum((v - mean) ** 2 for v in train_values) / len(train_values))
    return mean, spread


def apply_standardizer(values, mean, spread):
    if spread == 0:
        return [0.0 for _ in values]
    return [(v - mean) / spread for v in values]


def majority_baseline_accuracy(train_labels, test_labels):
    counts = {}
    for label in train_labels:
        counts[label] = counts.get(label, 0) + 1
    majority = max(counts, key=counts.get)
    return sum(label == majority for label in test_labels) / len(test_labels)
