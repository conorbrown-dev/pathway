# expect: GPT-2 small (tied) should have exactly 124,439,808 parameters
import math


def layer_norm(x, gamma, beta, eps=1e-5):
    mean = sum(x) / len(x)
    variance = sum((v - mean) ** 2 for v in x) / len(x)
    scale = math.sqrt(variance + eps)
    return [g * (v - mean) / scale + b for v, g, b in zip(x, gamma, beta)]


def rms_norm(x, gamma, eps=1e-5):
    rms = math.sqrt(sum(v * v for v in x) / len(x) + eps)
    return [g * v / rms for v, g in zip(x, gamma)]


def block_forward(x, attention, feed_forward, norm1, norm2):
    h = [a + b for a, b in zip(x, attention(norm1(x)))]
    return [a + b for a, b in zip(h, feed_forward(norm2(h)))]


def gpt_parameters(vocab_size, context, d_model, n_layers, tied):
    d = d_model
    embeddings = vocab_size * d + context * d
    per_block = (
        2 * 2 * d                 # two layer norms, gain and shift
        + 3 * d * d
        + d * d
        + d * 4 * d
        + 4 * d * d
    )
    final_norm = 2 * d
    head = 0 if tied else vocab_size * d
    return embeddings + n_layers * per_block + final_norm + head
