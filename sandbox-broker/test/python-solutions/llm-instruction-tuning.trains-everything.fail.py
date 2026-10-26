# expect: Only assistant content and its closing <|im_end|> should be trained
ROLES = ("system", "user", "assistant", "tool")


def validate_conversation(messages):
    if not messages:
        raise ValueError("a conversation needs messages")
    for index, message in enumerate(messages):
        if not isinstance(message, dict) or message.get("role") not in ROLES or not isinstance(message.get("content"), str):
            raise ValueError(f"message {index} is malformed")
        if message["role"] == "system" and index != 0:
            raise ValueError("a system message may only come first")
    rest = messages[1:] if messages[0]["role"] == "system" else messages
    if not rest or rest[0]["role"] != "user":
        raise ValueError("the first non-system message must be from the user")
    if messages[-1]["role"] != "assistant":
        raise ValueError("a training conversation must end with the assistant")


def _pieces(message):
    return [f"<|im_start|>{message['role']}\n", message["content"], "<|im_end|>\n"]


def render_chat(messages, add_generation_prompt):
    text = "".join(piece for message in messages for piece in _pieces(message))
    return text + ("<|im_start|>assistant\n" if add_generation_prompt else "")


def training_pieces(messages):
    validate_conversation(messages)
    pieces, mask = [], []
    for message in messages:
        trained = message["role"] == "assistant"
        header, content, end = _pieces(message)
        pieces += [header, content, end]
        mask += [0, 1, 1]
    return pieces, mask
