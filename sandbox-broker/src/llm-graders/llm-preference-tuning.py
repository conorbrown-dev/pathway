# tests: 4
def main():
    ns = load()
    bt, rm_loss, dpo, adv = (need(ns, n) for n in ("bradley_terry", "reward_model_loss", "dpo_loss", "group_advantages"))

    check(close(call(bt, 1.0, 1.0), 0.5), "Equal scores give a 0.5 preference probability.")
    check(close(call(bt, 3.0, 1.0), 1 / (1 + math.exp(-2)), 1e-9), "A score 2 higher gives about 0.88.")
    check(close(call(bt, 1.0, 3.0) + call(bt, 3.0, 1.0), 1.0, 1e-12), "P(A over B) + P(B over A) should be 1.")
    test("Bradley–Terry probabilities")

    check(close(call(rm_loss, 1.0, 1.0), math.log(2), 1e-9), "reward_model_loss with equal scores should be log 2.")
    check(close(call(rm_loss, 2.0, 0.0), math.log1p(math.exp(-2)), 1e-9), "reward_model_loss(2, 0) should be -log σ(2) ≈ 0.127.")
    try:
        big_wrong = call(rm_loss, -500.0, 500.0)
        big_right = call(rm_loss, 500.0, -500.0)
    except GraderFailure:
        fail("reward_model_loss crashed for a large score difference. Use log1p and branch on the sign.")
    check(close(big_wrong, 1000.0, 1e-9), "reward_model_loss(-500, 500) should be about 1000 (very wrong), computed without overflow.")
    check(0.0 <= big_right < 1e-300 or close(big_right, 0.0, 1e-12), "reward_model_loss(500, -500) should be about 0.")
    test("stable reward-model loss")

    value = call(dpo, -40.0, -42.0, -41.0, -41.0, 0.1)
    check(close(value, math.log1p(math.exp(-0.2)), 1e-9), f"The lesson's DPO example should give -log σ(0.2) ≈ 0.598; got {show(value)}.")
    check(close(call(dpo, -10.0, -10.0, -10.0, -10.0, 0.5), math.log(2), 1e-9), "When the policy equals the reference, the DPO loss is log 2.")
    check(call(dpo, -40.0, -45.0, -41.0, -41.0, 0.1) < call(dpo, -40.0, -42.0, -41.0, -41.0, 0.1), "Lowering the rejected response's probability further should lower the DPO loss.")
    test("DPO loss")

    a = call(adv, [1.0, 0.0, 0.0, 1.0])
    check(close_list(a, [1.0, -1.0, -1.0, 1.0], 1e-9), f"group_advantages([1, 0, 0, 1]) should be [1, -1, -1, 1]; got {show(a)}.")
    a = call(adv, [2.0, 4.0, 9.0])
    m = 5.0
    s = math.sqrt(((2 - m) ** 2 + (4 - m) ** 2 + (9 - m) ** 2) / 3)
    check(close_list(a, [(2 - m) / s, (4 - m) / s, (9 - m) / s], 1e-9), "Advantages are (reward - mean) / population spread.")
    check(close_list(call(adv, [0.5, 0.5, 0.5]), [0.0, 0.0, 0.0]), "When every attempt scores the same, there's nothing to learn: all advantages are 0.")
    test("group-relative advantages")


run(main)
