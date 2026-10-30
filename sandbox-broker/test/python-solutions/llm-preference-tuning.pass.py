import math


def _sigmoid(x):
    if x >= 0:
        return 1 / (1 + math.exp(-x))
    e = math.exp(x)
    return e / (1 + e)


def _neg_log_sigmoid(x):
    if x < 0:
        return -x + math.log1p(math.exp(x))
    return math.log1p(math.exp(-x))


def bradley_terry(score_a, score_b):
    return _sigmoid(score_a - score_b)


def reward_model_loss(chosen_score, rejected_score):
    return _neg_log_sigmoid(chosen_score - rejected_score)


def dpo_loss(policy_chosen, policy_rejected, ref_chosen, ref_rejected, beta):
    margin = (policy_chosen - ref_chosen) - (policy_rejected - ref_rejected)
    return _neg_log_sigmoid(beta * margin)


def group_advantages(rewards):
    mean = sum(rewards) / len(rewards)
    spread = math.sqrt(sum((r - mean) ** 2 for r in rewards) / len(rewards))
    if spread == 0:
        return [0.0 for _ in rewards]
    return [(r - mean) / spread for r in rewards]
