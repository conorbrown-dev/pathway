# expect: blank lines are skipped, not errors
import json
import zlib

ROLES = ("system", "user", "assistant")


def _problem(record):
    if not isinstance(record, dict) or not isinstance(record.get("messages"), list) or not record["messages"]:
        return "expected an object with a non-empty messages list"
    messages = record["messages"]
    for index, message in enumerate(messages):
        if not isinstance(message, dict) or message.get("role") not in ROLES:
            return f"message {index} has a missing or unknown role"
        if not isinstance(message.get("content"), str) or not message["content"].strip():
            return f"message {index} has empty content"
        if message["role"] == "system" and index != 0:
            return "a system message may only come first"
    roles = [m["role"] for m in messages if m["role"] != "system"]
    if not roles or roles[0] != "user":
        return "the user must speak before the assistant"
    if messages[-1]["role"] != "assistant":
        return "the last message must be from the assistant"
    return None


def parse_examples(jsonl_text):
    examples, errors = [], []
    for number, line in enumerate(jsonl_text.splitlines(), start=1):
        try:
            record = json.loads(line)
        except json.JSONDecodeError as error:
            errors.append(f"line {number}: invalid JSON ({error.msg})")
            continue
        problem = _problem(record)
        if problem:
            errors.append(f"line {number}: {problem}")
        else:
            examples.append(record)
    return examples, errors


def user_key(example):
    users = [" ".join(m["content"].lower().split()) for m in example["messages"] if m["role"] == "user"]
    return "\n".join(users)


def dedupe(examples):
    seen, kept = set(), []
    for example in examples:
        key = user_key(example)
        if key not in seen:
            seen.add(key)
            kept.append(example)
    return kept


def split(examples, validation_fraction):
    train, validation = [], []
    for example in examples:
        bucket = zlib.crc32(user_key(example).encode()) / 2 ** 32
        (validation if bucket < validation_fraction else train).append(example)
    return train, validation


def remove_leaks(train, evaluation):
    blocked = {user_key(example) for example in evaluation}
    clean = [example for example in train if user_key(example) not in blocked]
    return clean, len(train) - len(clean)
