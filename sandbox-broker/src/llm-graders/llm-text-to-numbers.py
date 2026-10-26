# tests: 4
def main():
    ns = load()
    enc_char, enc, vocab_fn, encode, decode = (need(ns, n) for n in ("utf8_encode_char", "utf8_encode", "char_vocabulary", "encode_chars", "decode_chars"))

    for ch in ["A", "z", "\n", "~"]:
        got = call(enc_char, ord(ch))
        check(list(got) == list(ch.encode("utf-8")), f"utf8_encode_char({ord(ch)}) should be {list(ch.encode())}; got {show(got)}. Code points below 128 are a single byte equal to the code point.")
    for ch in ["é", "ß", "Ж", "€", "猫", "😀", "߿", "ࠀ", "￿", "\U00010000"]:
        got = call(enc_char, ord(ch))
        check(list(got) == list(ch.encode("utf-8")), f"utf8_encode_char({ord(ch)}) for {ch!r} should be {list(ch.encode())}; got {show(got)}.")
    test("one-byte and multi-byte characters")

    for text in ["héllo", "price: 5€", "猫が好き", "ok 😀👍", ""]:
        got = call(enc, text)
        check(list(got) == list(text.encode("utf-8")), f"utf8_encode({text!r}) should be {list(text.encode())}; got {show(got)}.")
    test("whole strings, including emoji")

    vocab = call(vocab_fn, "banana!")
    check(vocab == {"!": 0, "a": 1, "b": 2, "n": 3}, f"char_vocabulary('banana!') should number characters in sorted order: {{'!': 0, 'a': 1, 'b': 2, 'n': 3}}; got {show(vocab)}.")
    test("sorted character vocabulary")

    ids = call(encode, "banana", vocab)
    check(list(ids) == [2, 1, 3, 1, 3, 1], f"encode_chars('banana', vocabulary) should be [2, 1, 3, 1, 3, 1]; got {show(ids)}.")
    text = "the café is open 😀"
    vocab = call(vocab_fn, text)
    check(call(decode, call(encode, text, vocab), vocab) == text, "decode_chars(encode_chars(text)) should give back the original text.")
    test("encode and decode round trip")


run(main)
