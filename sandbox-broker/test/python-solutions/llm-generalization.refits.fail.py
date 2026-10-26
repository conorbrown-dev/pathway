# expect: apply_standardizer must use the mean and spread it is given
import math
import zlib


def assign_split(example_id, validation_fraction, test_fraction):
    bucket = zlib.crc32(example_id.encode()) / 2 ** 32
    if bucket < test_fraction:
        return "test"
    if bucket < test_fraction + validation_fraction:
        return "validation"
    return "train"


def find_leaks(train_texts, test_texts):
    norm = lambda t: " ".join(t.lower().split())
    return sorted({norm(t) for t in test_texts} & {norm(t) for t in train_texts})


def fit_standardizer(train_values):
    mean = sum(train_values) / len(train_values)
    return mean, math.sqrt(sum((v - mean) ** 2 for v in train_values) / len(train_values))


def apply_standardizer(values, mean, spread):
    mean, spread = fit_standardizer(values)
    return [(v - mean) / spread for v in values]


def majority_baseline_accuracy(train_labels, test_labels):
    return 0.0
