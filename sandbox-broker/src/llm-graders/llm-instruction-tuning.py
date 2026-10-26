# tests: 4
CONVO = [
    {"role": "system", "content": "You are concise."},
    {"role": "user", "content": "Capital of France?"},
    {"role": "assistant", "content": "Paris."},
    {"role": "user", "content": "And Spain?"},
    {"role": "assistant", "content": "Madrid."},
]


def main():
    ns = load()
    validate, render, pieces_fn = (need(ns, n) for n in ("validate_conversation", "render_chat", "training_pieces"))

    try:
        call(validate, CONVO)
        call(validate, CONVO[1:])
    except GraderFailure:
        fail("validate_conversation rejected a valid conversation.")
    bad = [
        ("ends with the user", CONVO[:4]),
        ("starts with the assistant", [{"role": "assistant", "content": "Hi"}]),
        ("has a system message that isn't first", [CONVO[1], CONVO[0], CONVO[2]]),
        ("has an unknown role", [{"role": "narrator", "content": "x"}, CONVO[2]]),
        ("has non-string content", [{"role": "user", "content": 5}, CONVO[2]]),
        ("is empty", []),
    ]
    for label, convo in bad:
        check(raises(validate, ValueError, convo), f"validate_conversation should raise ValueError for a conversation that {label}.")
    test("validation")

    text = call(render, CONVO[:3], False)
    expected = "<|im_start|>system\nYou are concise.<|im_end|>\n<|im_start|>user\nCapital of France?<|im_end|>\n<|im_start|>assistant\nParis.<|im_end|>\n"
    check(text == expected, f"render_chat gave {show(text, 300)}; expected {show(expected, 300)}.")
    prompt = call(render, CONVO[:2], True)
    check(prompt.endswith("<|im_end|>\n<|im_start|>assistant\n"), "With add_generation_prompt, the text should end with the assistant header so the model writes the reply.")
    test("rendering with and without the generation prompt")

    pieces, mask = call(pieces_fn, CONVO)
    check(len(pieces) == 15 and len(mask) == 15, f"training_pieces should give three pieces per message (15 here); got {len(pieces)} pieces and {len(mask)} mask entries.")
    check(not mask[6] and pieces[6] == "<|im_start|>assistant\n", "The assistant header is supplied by the harness, so it must not be trained.")
    trained = [p for p, m in zip(pieces, mask) if m]
    check(trained == ["Paris.", "<|im_end|>\n", "Madrid.", "<|im_end|>\n"], f"Only assistant content and its closing <|im_end|> should be trained; you trained {show(trained)}.")
    check(raises(pieces_fn, ValueError, CONVO[:4]), "training_pieces should validate first and reject a conversation that ends with the user.")
    test("assistant-only loss mask")

    check("".join(pieces) == call(render, CONVO, False), "Joining the training pieces should give exactly render_chat(messages, False).")
    test("pieces join back into the rendered chat")


run(main)
