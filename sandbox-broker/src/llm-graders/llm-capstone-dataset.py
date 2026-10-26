# tests: 4
import json
import zlib


def ex(user, assistant="ok", system="Write a commit message."):
    return {"messages": [{"role": "system", "content": system}, {"role": "user", "content": user}, {"role": "assistant", "content": assistant}]}


def main():
    ns = load()
    parse, user_key, dedupe, split, remove_leaks = (need(ns, n) for n in ("parse_examples", "user_key", "dedupe", "split", "remove_leaks"))

    lines = [
        json.dumps(ex("diff one", "fix: one")),
        "",
        "{not json",
        json.dumps({"messages": [{"role": "user", "content": "q"}]}),
        json.dumps({"messages": [{"role": "assistant", "content": "a"}, {"role": "user", "content": "q"}, {"role": "assistant", "content": "a"}]}),
        json.dumps({"messages": [{"role": "user", "content": "   "}, {"role": "assistant", "content": "a"}]}),
        json.dumps({"messages": [{"role": "user", "content": "q"}, {"role": "system", "content": "late"}, {"role": "assistant", "content": "a"}]}),
        json.dumps({"text": "no messages"}),
        json.dumps({"messages": [{"role": "user", "content": "diff two"}, {"role": "assistant", "content": "feat: two"}]}),
        json.dumps({"messages": [{"role": "robot", "content": "q"}, {"role": "assistant", "content": "a"}]}),
    ]
    examples, errors = call(parse, "\n".join(lines))
    check(isinstance(examples, list) and len(examples) == 2, f"parse_examples should accept exactly the 2 valid lines here; it accepted {len(examples) if isinstance(examples, list) else show(examples)}.")
    check(isinstance(errors, list) and len(errors) == 7, f"There should be 7 errors (blank lines are skipped, not errors); got {len(errors) if isinstance(errors, list) else show(errors)}: {show(errors, 400)}.")
    starts = sorted(int(e.split(":")[0].split()[1]) for e in errors)
    check(starts == [3, 4, 5, 6, 7, 8, 10], f"Each error should start with 'line N:' using the line's number (counting from 1); got lines {starts}.")
    test("parsing and validating lines")

    check(call(user_key, ex("  Fix   the\tBUG  ")) == "fix the bug", "user_key should lowercase and collapse whitespace.")
    two_users = {"messages": [{"role": "user", "content": "A"}, {"role": "assistant", "content": "x"}, {"role": "user", "content": "B  c"}, {"role": "assistant", "content": "y"}]}
    check(call(user_key, two_users) == "a\nb c", "user_key should join all user messages with a newline.")
    data = [ex("Diff A", "1"), ex("diff   a", "2"), ex("diff b", "3")]
    kept = call(dedupe, data)
    check([e["messages"][2]["content"] for e in kept] == ["1", "3"], "dedupe should keep the first example for each normalized user text, in order.")
    test("normalized keys and deduplication")

    many = [ex(f"change number {i}") for i in range(300)]
    train, validation = call(split, many, 0.1)
    expected_val = [e for e in many if zlib.crc32(" ".join(e["messages"][1]["content"].lower().split()).encode()) / 2 ** 32 < 0.1]
    check(validation == expected_val, "split should send exactly the examples whose crc32 bucket is below validation_fraction to validation, in their original order.")
    check(len(train) + len(validation) == 300 and all(e not in validation for e in train), "Every example should land in exactly one split.")
    test("deterministic hash split")

    evaluation = [ex("change number 5"), ex("CHANGE  number 7"), ex("something else")]
    clean, removed = call(remove_leaks, many, evaluation)
    check(removed == 2 and len(clean) == 298, f"remove_leaks should drop the 2 training examples whose normalized input matches an evaluation input; removed {removed}.")
    check(all(call(user_key, e) not in ("change number 5", "change number 7") for e in clean), "No leaked example should remain.")
    test("removing evaluation leaks")


run(main)
