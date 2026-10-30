# tests: 4
def main():
    ns = load()
    flops, chinchilla, days, memory, dedup = (need(ns, n) for n in ("training_flops", "chinchilla_tokens", "training_days", "adam_memory_gb", "dedup_exact"))

    check(close(call(flops, 7e9, 2e12), 8.4e22, 1e-9), "training_flops(7e9, 2e12) should be 6 × 7e9 × 2e12 = 8.4e22.")
    check(close(call(chinchilla, 70e9), 1.4e12, 1e-9), "chinchilla_tokens(70e9) should be 20 tokens per parameter: 1.4e12.")
    test("FLOPs and Chinchilla tokens")

    check(close(call(days, 1.2e20, 512, 1e15, 0.4), 1.2e20 / (512 * 1e15 * 0.4) / 86400, 1e-9), "training_days should divide the FLOPs by (GPUs × peak × utilization) to get seconds, then by 86,400.")
    check(close(call(days, 8.4e22, 1000, 1e15, 0.4), 2.430555, 1e-5), "8.4e22 FLOPs on 1,000 GPUs at 1e15 FLOP/s and 40% utilization is about 2.43 days.")
    test("turning FLOPs into days")

    check(close(call(memory, 7e9), 112.0, 1e-9), "adam_memory_gb(7e9) should be 7e9 × 16 bytes = 112 GB.")
    check(close(call(memory, 1e9, 2), 2.0, 1e-9), "adam_memory_gb(1e9, 2) should be 2 GB.")
    test("training memory")

    docs = ["The cat sat.", "the  cat   SAT.", "A dog ran.", "The cat sat. ", "a dog ran", "New text"]
    kept = call(dedup, docs)
    check(kept == ["The cat sat.", "A dog ran.", "a dog ran", "New text"], f"dedup_exact gave {show(kept)}; keep the first original text of each normalized form, in order. 'a dog ran' differs from 'A dog ran.' by its period, so both stay.")
    check(call(dedup, []) == [], "dedup_exact([]) should be [].")
    test("exact deduplication")


run(main)
