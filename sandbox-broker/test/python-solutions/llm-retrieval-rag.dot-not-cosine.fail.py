# expect: chunks 0 and 4 tie at similarity 1
import math


def chunk_words(text, size, overlap):
    if not 0 <= overlap < size:
        raise ValueError("overlap must satisfy 0 <= overlap < size")
    words = text.split()
    chunks = []
    start = 0
    while start < len(words):
        chunks.append(" ".join(words[start:start + size]))
        if start + size >= len(words):
            break
        start += size - overlap
    return chunks


def bag_of_words(text, vocabulary):
    words = text.lower().split()
    return [words.count(term) for term in vocabulary]


def _cosine(a, b):
    na = math.sqrt(sum(x * x for x in a))
    nb = math.sqrt(sum(y * y for y in b))
    if na == 0 or nb == 0:
        return 0.0
    return sum(x * y for x, y in zip(a, b)) / (na * nb)


def top_k(query_vector, chunk_vectors, k):
    scored = sorted(range(len(chunk_vectors)), key=lambda i: (-sum(x * y for x, y in zip(query_vector, chunk_vectors[i])), i))
    return scored[:k]


def recall_at_k(results, relevant, k):
    found = set(results[:k]) & set(relevant)
    return len(found) / len(relevant)
