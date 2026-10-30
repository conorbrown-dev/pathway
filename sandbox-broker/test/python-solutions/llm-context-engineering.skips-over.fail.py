# expect: Stop at the first history message (from newest) that doesn't fit
import json


def fit_history(system, history, new_message, window, reserve_output, count_tokens):
    budget = window - reserve_output - count_tokens(system["content"]) - count_tokens(new_message["content"])
    if budget < 0:
        raise ValueError("the system prompt and the new message don't fit in the window")
    kept = []
    for message in reversed(history):
        cost = count_tokens(message["content"])
        if cost > budget:
            continue  # bug: skips a big message and keeps older ones
        kept.append(message)
        budget -= cost
    return [system] + list(reversed(kept)) + [new_message]


def few_shot_messages(instruction, examples, query):
    messages = [{"role": "system", "content": instruction}]
    for example_input, example_output in examples:
        messages.append({"role": "user", "content": example_input})
        messages.append({"role": "assistant", "content": example_output})
    messages.append({"role": "user", "content": query})
    return messages


def wrap_document(name, text):
    safe = text.replace("</document>", "&lt;/document&gt;")
    return f'<document name="{name}">\n{safe}\n</document>'


def extract_json(reply):
    decoder = json.JSONDecoder()
    for index, character in enumerate(reply):
        if character != "{":
            continue
        try:
            value, _ = decoder.raw_decode(reply, index)
        except json.JSONDecodeError:
            continue
        if isinstance(value, dict):
            return value
    raise ValueError("no JSON object found")
