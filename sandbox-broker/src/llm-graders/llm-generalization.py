# tests: 4
import zlib


def main():
    ns = load()
    assign, leaks, fit_std, apply_std, baseline = (need(ns, n) for n in ("assign_split", "find_leaks", "fit_standardizer", "apply_standardizer", "majority_baseline_accuracy"))

    def expected(example_id, val, test_fraction):
        bucket = zlib.crc32(example_id.encode()) / 2 ** 32
        return "test" if bucket < test_fraction else "validation" if bucket < test_fraction + val else "train"

    ids = [f"ticket-{i}" for i in range(400)]
    got = [call(assign, i, 0.1, 0.1) for i in ids]
    check(all(g in ("train", "validation", "test") for g in got), "assign_split should return 'train', 'validation' or 'test'.")
    wrong = [i for i, g in zip(ids, got) if g != expected(i, 0.1, 0.1)]
    check(not wrong, f"assign_split put {wrong[0] if wrong else ''!r} in the wrong split. Use zlib.crc32(example_id.encode()) / 2**32, test first, then validation.")
    check(call(assign, "ticket-7", 0.1, 0.1) == got[7], "assign_split must give the same answer every time for the same id.")
    shares = {s: got.count(s) / len(got) for s in ("train", "validation", "test")}
    check(0.7 < shares["train"] < 0.9, "About 80% of examples should land in train with 10% validation and 10% test.")
    test("deterministic hash split")

    train = ["Reset my password", "Billing  error on invoice", "App crashes on start"]
    test_texts = ["reset my PASSWORD", "New feature request", "  billing error on\tinvoice "]
    found = call(leaks, train, test_texts)
    check(found == ["billing error on invoice", "reset my password"], f"find_leaks gave {show(found)}; expected the normalized texts ['billing error on invoice', 'reset my password'], sorted.")
    check(call(leaks, ["a"], ["b"]) == [], "find_leaks with nothing in common should return [].")
    test("leak detection after normalizing")

    mean, spread = call(fit_std, [2, 4, 4, 6])
    check(close(mean, 4.0) and close(spread, math.sqrt(2)), f"fit_standardizer([2, 4, 4, 6]) should be (4, sqrt(2)); got {show((mean, spread))}.")
    z = call(apply_std, [100, 4], 4.0, 2.0)
    check(close_list(z, [48.0, 0.0]), "apply_standardizer must use the mean and spread it is given (from training), not recompute them from the new values.")
    test("standardizer fitted on training data only")

    check(close(call(baseline, ["billing"] * 7 + ["technical"] * 3, ["billing", "technical", "billing", "billing"]), 0.75), "Always predicting 'billing' gets 3 of these 4 test labels right: 0.75.")
    check(close(call(baseline, ["a", "b", "b", "a"], ["a", "b"]), 0.5), "On a tie, use the label seen first in training ('a'), giving 0.5 here.")
    check(close(call(baseline, ["b", "a", "a", "b"], ["b", "b", "a"]), 2 / 3), "On a tie, use the label seen first in training ('b' here).")
    test("majority baseline")


run(main)
