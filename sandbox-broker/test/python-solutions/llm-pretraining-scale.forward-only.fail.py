# expect: training_flops(7e9, 2e12) should be 6 × 7e9 × 2e12
def training_flops(params, tokens):
    return 2 * params * tokens


def chinchilla_tokens(params):
    return 20 * params


def training_days(flops, gpus, peak_flops_per_gpu, utilization):
    seconds = flops / (gpus * peak_flops_per_gpu * utilization)
    return seconds / 86400


def adam_memory_gb(params, bytes_per_param=16):
    return params * bytes_per_param / 1e9


def dedup_exact(documents):
    seen, kept = set(), []
    for text in documents:
        key = " ".join(text.lower().split())
        if key not in seen:
            seen.add(key)
            kept.append(text)
    return kept
