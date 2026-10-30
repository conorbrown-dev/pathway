namespace Pathway.Content;

static partial class Curriculum
{
    // Module 7 of the AI & LLM course: how frontier models are made. Pretraining data and compute,
    // instruction tuning with chat templates and loss masks, preference tuning and RL, and honest
    // evaluation.
    private static LlmLessonSpec[] LlmFrontierSpecs() =>
    [
        new(
            "llm-pretraining-scale",
            "How frontier models are made",
            "Pretraining at scale: data, compute and scaling laws",
            "What it takes to pretrain a frontier model: trillions of filtered, deduplicated tokens, training compute you can estimate as 6 × parameters × tokens, memory for weights and optimizer state, and thousands of GPUs working in parallel.",
            "Pretraining is next-token prediction on trillions of tokens of filtered, deduplicated text and code. Its compute is about 6 floating-point operations per parameter per token, so it can be estimated before a run starts. Loss falls smoothly and predictably as parameters, data and compute grow (scaling laws); for a fixed compute budget the best loss comes from roughly 20 training tokens per parameter, though production models usually train on far more so they're cheaper to run. Training needs about 16 bytes of memory per parameter with Adam in mixed precision, which is why it's split across many GPUs.",
            [
                ("llm-transformer-block", "pretraining trains the architecture you counted there."),
                ("llm-training-networks", "it uses the same AdamW, schedules and clipping, at scale."),
                ("llm-generalization", "deduplication and contamination are leak problems at internet scale."),
            ],
            """
            ## The idea in plain words
            **Pretraining** is the expensive first phase that gives a model nearly all its knowledge and skill. The task is the one you've built: predict the next token, measured by cross-entropy. What changes is the scale: trillions of tokens of text and code, billions of parameters, and thousands of specialized chips running for weeks or months. This lesson is about the numbers, so you can reason about what any model costs to build.

            ## Data: where the tokens come from
            A pretraining dataset is assembled, not found. A typical pipeline:

            1. **Collect:** web crawls (Common Crawl has petabytes of pages), code repositories, books, papers, forums, licensed sources. Responsible teams honor opt-outs such as robots.txt and keep track of licenses; this is an active legal and ethical debate.
            2. **Extract:** strip HTML, menus, ads and boilerplate to get the actual text.
            3. **Filter:** remove spam, machine-generated junk, personal data, toxic content and pages in unwanted languages. Many pipelines train a small **quality classifier** to score pages, for example "how much does this look like a reference article?"
            4. **Deduplicate:** the web is enormously repetitive (mirrors, templates, copied articles). Exact duplicates are removed by hashing normalized text; near-duplicates are removed with techniques that compare overlapping chunks. Duplicates waste compute and make models memorize, as the generalization lesson warned.
            5. **Mix:** choose proportions of web text, code, math, books and other languages. The mix shapes what the model is good at, and it's one of a lab's most guarded decisions.

            Open datasets such as FineWeb document their pipelines in detail, and they're worth reading.

            ## Compute: the 6ND rule
            How much computation does training take? A useful rule: **about 6 floating-point operations (FLOPs) per parameter per training token.** Where does 6 come from? The forward pass does about one multiply and one add per parameter per token, so 2. The backward pass costs about twice the forward, so 4. That's 6 altogether.

            > training FLOPs ≈ 6 × N × D

            where N is the number of parameters and D is the number of training tokens. A 7-billion-parameter model trained on 2 trillion tokens needs about 6 × 7×10⁹ × 2×10¹² ≈ 8.4×10²² FLOPs.

            To turn FLOPs into time, divide by what the hardware actually delivers. A modern data-center GPU might peak around 10¹⁵ FLOPs per second (a **petaFLOP**) in the 16-bit formats training uses, but real training reaches only part of that, typically 35–55%, because of communication and memory traffic. That fraction is called **model FLOPs utilization (MFU)**. At 40% of 10¹⁵, one GPU does 4×10¹⁴ useful FLOPs per second, so 8.4×10²² FLOPs takes about 2.1×10⁸ GPU-seconds, roughly 2,400 GPU-days: about 2.5 days on 1,000 GPUs.

            ## Scaling laws
            Researchers found that a model's loss falls **smoothly and predictably** as you increase parameters, data and compute, following a **power law**: each tenfold increase in compute buys a roughly constant drop in loss. That predictability is what lets labs plan a huge run from small experiments.

            For a fixed compute budget, how should you split it between model size and data? DeepMind's 2022 **Chinchilla** study found the best loss comes from scaling both together, at roughly **20 training tokens per parameter**. A 70-billion-parameter model is "compute-optimal" at about 1.4 trillion tokens.

            But the training budget isn't the only cost. A model is trained once and then run billions of times, and a smaller model is cheaper to run. So production models are usually **overtrained**: far past 20 tokens per parameter, often 1,000 or more, accepting extra training cost for a smaller, cheaper, nearly-as-good model. Why loss follows power laws at all is not fully understood, and smooth loss curves don't always mean smooth gains in specific abilities.

            ## Memory: why one GPU isn't enough
            Training needs to hold, for every parameter, more than just the weight. With Adam in **mixed precision** (computing in 16-bit numbers but keeping a 32-bit master copy for accurate updates), the usual budget is about **16 bytes per parameter**: 2 for the 16-bit weight, 2 for its gradient, 4 for the 32-bit master copy, and 4 each for Adam's two running averages. A 7-billion-parameter model needs about 112 GB just for that, before counting **activations** (the intermediate values saved for the backward pass), which grow with batch size and context length.

            The largest single GPUs hold around 80–200 GB, so training is split across many:

            - **Data parallelism:** every GPU holds a copy of the model and processes different examples; gradients are averaged across GPUs each step.
            - **Sharding (ZeRO / FSDP):** split the optimizer state, gradients and even weights across GPUs, so no single GPU holds them all.
            - **Tensor parallelism:** split individual matrix multiplications across GPUs.
            - **Pipeline parallelism:** put different layers on different GPUs and stream batches through them like an assembly line.

            Big runs combine all four. They also have to survive hardware failures: with thousands of GPUs, something breaks every few hours, so runs save **checkpoints** regularly and restart from them.

            ## What the symbols stand for
            - **N**: number of parameters, a **constant** for a given model, like 7×10⁹.
            - **D**: number of training tokens, a **choice**, like 2×10¹².
            - **C ≈ 6ND**: total training FLOPs.
            - **MFU**: model FLOPs utilization, the fraction of peak hardware speed actually achieved, like 0.4.
            - **Bytes per parameter**: about 16 for Adam in mixed precision.

            ## Walk through one problem
            You have 512 GPUs, each with a peak of 10¹⁵ FLOP/s, at 40% MFU. How long to train a 1-billion-parameter model on its Chinchilla-optimal data?

            1. Tokens: 20 × 10⁹ = 2×10¹⁰.
            2. FLOPs: 6 × 10⁹ × 2×10¹⁰ = 1.2×10²⁰.
            3. Useful speed: 512 × 10¹⁵ × 0.4 ≈ 2.05×10¹⁷ FLOP/s.
            4. Time: 1.2×10²⁰ / 2.05×10¹⁷ ≈ 586 seconds, under 10 minutes. Small models are cheap; the cost grows with the product N × D, so frontier runs cost millions of times more.

            ## Explain it to your team
            "Pretraining is next-token prediction on trillions of cleaned, deduplicated tokens. Compute is about six operations per parameter per token, so we can estimate a run's cost before starting. Loss improves predictably with scale, and the compute-optimal recipe is about 20 tokens per parameter, though deployed models train on far more so they're cheaper to serve. Training needs about 16 bytes per parameter of memory, so it's split across thousands of GPUs that must survive failures."
            """,
            """
            training FLOPs ≈ 6 × N × D          (2 forward + 4 backward per parameter per token)
            7B parameters × 2T tokens  → 6 × 7e9 × 2e12 ≈ 8.4e22 FLOPs
            time = FLOPs / (GPUs × peak FLOP/s × MFU)
            compute-optimal tokens ≈ 20 × N      (1B → 20B tokens)
            Adam in mixed precision ≈ 16 bytes per parameter   (7B → 112 GB before activations)
            """,
            LlmCode(
                "Plan a training run",
                "Write the back-of-the-envelope calculator a team uses before a pretraining run, plus the exact deduplication step every data pipeline needs. Standard library only.",
                [
                    "`training_flops(params, tokens)` returns 6 × params × tokens, and `chinchilla_tokens(params)` returns 20 × params",
                    "`training_days(flops, gpus, peak_flops_per_gpu, utilization)` returns flops ÷ (gpus × peak × utilization), converted from seconds to days",
                    "`adam_memory_gb(params, bytes_per_param=16)` returns params × bytes_per_param ÷ 10⁹",
                    "`dedup_exact(documents)` returns the documents with exact duplicates removed after normalizing (lowercase, whitespace collapsed), keeping the first occurrence and the original order and text",
                ],
                """
                def training_flops(params, tokens):
                    ...


                def chinchilla_tokens(params):
                    ...


                def training_days(flops, gpus, peak_flops_per_gpu, utilization):
                    ...


                def adam_memory_gb(params, bytes_per_param=16):
                    ...


                def dedup_exact(documents):
                    ...
                """,
                "A day has 86,400 seconds. For deduplication, keep a `set` of normalized texts (`' '.join(text.lower().split())`) and add each document's original text to the output only the first time its normalized form appears.",
                ["FLOPs and Chinchilla tokens", "turning FLOPs into days", "training memory", "exact deduplication"])
        ),
        new(
            "llm-instruction-tuning",
            "How frontier models are made",
            "Instruction tuning: from text predictor to assistant",
            "A pretrained model continues text; an assistant answers. Supervised fine-tuning on example conversations, written in a chat template with special tokens and trained only on the assistant's words, makes the difference.",
            "A base model continues whatever text it's given, so a question may get more questions back. Supervised fine-tuning (SFT) trains it further on example conversations so it learns to respond as an assistant. Each conversation is rendered with a chat template that wraps every message in special role tokens, and the loss is masked so the model learns only from the assistant's tokens, not the user's. The same template must be used at inference, or the model sees a format it was never trained on.",
            [
                ("llm-pretraining-scale", "instruction tuning starts from a pretrained base model."),
                ("llm-tokenization-bpe", "chat templates rely on special tokens outside ordinary text."),
                ("llm-loss-functions", "the loss is the same cross-entropy, applied only to some tokens."),
            ],
            """
            ## Base models aren't assistants
            Give a pretrained **base model** the text "What is the capital of France?" and it may continue with "What is the capital of Germany? What is the capital of Spain?", because on the internet, a question is often followed by more questions, like in a quiz. The base model has the knowledge; it just doesn't know it's supposed to *answer*. It continues documents.

            **Post-training** turns it into an assistant. The first step is **supervised fine-tuning (SFT)**, also called **instruction tuning**: keep training with the same next-token loss, but on carefully written example conversations where an assistant responds helpfully. After a modest amount of this (thousands to a few million examples, tiny next to pretraining), the model learns the role.

            ## Chat templates and special tokens
            A conversation is a list of **messages**, each with a **role**:

            - **system:** instructions from whoever deploys the model (tone, rules, available tools).
            - **user:** the person's messages.
            - **assistant:** the model's replies.
            - **tool:** results returned by tools the model called (the tool-calling lesson).

            The model only reads one flat sequence of tokens, so the conversation has to be written out in an agreed format, the **chat template**. A common one, ChatML, wraps each message like this:

            ```
            <|im_start|>system
            You are a concise assistant.<|im_end|>
            <|im_start|>user
            What is the capital of France?<|im_end|>
            <|im_start|>assistant
            Paris.<|im_end|>
            ```

            `<|im_start|>` and `<|im_end|>` are **special tokens**: reserved IDs in the vocabulary that ordinary text can never produce. If a user types the characters "<|im_end|>" into a message, the tokenizer encodes them as ordinary characters, not as the special token, so users can't forge a role boundary. (When software gets this wrong, it's a security bug.)

            At inference, the harness renders the conversation so far and ends with `<|im_start|>assistant` and a newline: the **generation prompt**. The model continues from there, writes its reply, and emits `<|im_end|>` when it's done, which tells the harness to stop.

            Every model family has its own template. Using the wrong one at inference, for example missing the generation prompt or using another model's tokens, is a common cause of a fine-tuned model "suddenly getting dumb." Tokenizer files usually ship the template, and libraries like Hugging Face's `apply_chat_template` render it for you.

            ## Train only on what the assistant says
            In a training conversation, the system and user text is **given**. You don't want the model learning to write user messages. So the loss is computed only on the assistant's tokens, including the `<|im_end|>` that ends each assistant turn (so it learns when to stop). Every other position is **masked out**: it still flows through the model as context, but it contributes nothing to the loss. In code this is a **loss mask**, one flag per token: 1 means "train on this," 0 means "context only."

            ## What makes SFT data good
            - **Quality beats quantity.** A few thousand excellent, diverse examples can beat millions of mediocre ones. Every example teaches the model what a "good answer" looks like.
            - **Cover the behaviors you want:** following formats, admitting uncertainty, refusing harmful requests, asking clarifying questions, using tools.
            - **Consistency:** contradictory examples teach confusion.
            - **Sources:** human-written demonstrations, and increasingly, high-quality model-generated examples reviewed by people (**synthetic data**).

            SFT teaches format and behavior well, but it's limited by the demonstrations: a model can't learn to be better than its examples just by imitating them. The next lesson covers methods that go beyond imitation.

            ## What the words mean
            - **Base model:** a pretrained model before post-training.
            - **Supervised fine-tuning (SFT):** continued training on example conversations.
            - **Chat template:** the exact text format that turns a list of messages into one token sequence.
            - **Special token:** a reserved vocabulary entry that ordinary text can't produce.
            - **Generation prompt:** the opening of the assistant's turn, appended so the model writes the reply.
            - **Loss mask:** one flag per token saying whether it counts toward the loss.

            ## Walk through one problem
            Render a training example with a system message, one user message and one assistant reply, and decide what's trained.

            1. Pieces in order: `<|im_start|>system\n`, the system text, `<|im_end|>\n`, `<|im_start|>user\n`, the user text, `<|im_end|>\n`, `<|im_start|>assistant\n`, the reply, `<|im_end|>\n`.
            2. Trained (mask 1): the reply and the `<|im_end|>\n` after it.
            3. Not trained (mask 0): everything else, including the `<|im_start|>assistant\n` header, which the harness always supplies itself.
            4. In a multi-turn conversation, every assistant turn is trained the same way, and every user turn is masked.

            ## Explain it to your team
            "A pretrained model just continues text, so it's fine-tuned on example conversations to act as an assistant. Each conversation is written out in a chat template, with special tokens marking who's speaking, and the model is trained only on the assistant's words. At inference the harness uses the same template and asks the model to write the next assistant turn. A template mismatch is one of the most common reasons a fine-tuned model misbehaves."
            """,
            """
            <|im_start|>system\n  You are concise.        <|im_end|>\n     mask 0 0 0
            <|im_start|>user\n    Capital of France?     <|im_end|>\n     mask 0 0 0
            <|im_start|>assistant\n  Paris.                <|im_end|>\n     mask 0 1 1
            inference: render the conversation, then append "<|im_start|>assistant\n" (the generation prompt)
            """,
            LlmCode(
                "Render a chat and mask the loss",
                "Implement a ChatML-style chat template and the loss mask used for supervised fine-tuning. Standard library only. Use exactly the special-token strings `<|im_start|>` and `<|im_end|>`.",
                [
                    "`validate_conversation(messages)` raises `ValueError` unless every message is a dict with `role` in system/user/assistant/tool and string `content`, at most one system message comes first, the first non-system message is from the user, and the conversation ends with an assistant message",
                    "`render_chat(messages, add_generation_prompt)` returns the full text: each message as `<|im_start|>{role}\\n{content}<|im_end|>\\n`, followed by `<|im_start|>assistant\\n` when `add_generation_prompt` is true",
                    "`training_pieces(messages)` validates, then returns `(pieces, mask)`: three pieces per message (the header `<|im_start|>{role}\\n`, the content, and `<|im_end|>\\n`), with mask 1 only for an assistant message's content and its closing piece",
                    "Joining the pieces must give exactly `render_chat(messages, False)`",
                ],
                """
                ROLES = ("system", "user", "assistant", "tool")


                def validate_conversation(messages):
                    ...


                def render_chat(messages, add_generation_prompt):
                    ...


                def training_pieces(messages):
                    ...
                """,
                "Build each message's three pieces in one helper and reuse it in both `render_chat` and `training_pieces`, so they can't disagree. The header is never trained, even for assistant turns.",
                ["validation", "rendering with and without the generation prompt", "assistant-only loss mask", "pieces join back into the rendered chat"])
        ),
        new(
            "llm-preference-tuning",
            "How frontier models are made",
            "Preference tuning and reinforcement learning",
            "It's easier to say which of two answers is better than to write the best one. Reward models, RLHF, DPO and reinforcement learning with checkable rewards use that to push a model past its demonstrations.",
            "People compare two responses more reliably than they write ideal ones, so post-training collects preferences: for a prompt, which response was better. A reward model learns to score responses so that the preferred one scores higher, using the Bradley–Terry model (probability preferred = sigmoid of the score difference). RLHF then trains the assistant to earn high reward while a penalty keeps it close to its starting point. DPO skips the reward model and trains directly on the preference pairs. For tasks with checkable answers, such as math and code, reinforcement learning against verifiable rewards trains models to reason step by step.",
            [
                ("llm-instruction-tuning", "preference tuning starts from an SFT model."),
                ("llm-classification", "the sigmoid of a score difference turns scores into a probability of preference."),
                ("llm-exp-log-softmax", "DPO compares log-probabilities of whole responses."),
            ],
            """
            ## Why imitation isn't enough
            SFT teaches a model to imitate example answers. That has two limits. Writing excellent demonstrations for every situation is slow and expensive. And a model trained only to imitate can't really become better than its examples.

            But people are good at a different, easier task: **comparing**. Given two answers to the same question, most people can say which is more helpful, more accurate or safer, even if they couldn't have written either. **Preference tuning** builds on those comparisons.

            ## Preference data
            The raw material is triples: a **prompt**, a **chosen** response and a **rejected** response. Typically the model generates two or more answers; a person (or a model following written guidelines) picks the better one. Large labs collect hundreds of thousands to millions of these.

            ## Reward models and Bradley–Terry
            A **reward model** is a network (usually an LLM with its output layer replaced by a single number) that scores how good a response is. It's trained on the pairs with a simple idea from 1952, the **Bradley–Terry model**: the probability that A is preferred over B is the **sigmoid of the difference in their scores**.

            > P(A preferred over B) = sigmoid(score_A − score_B)

            Equal scores give 0.5. A score 2 higher gives about 0.88. Only the difference matters, so the scores have no absolute meaning. Training maximizes the probability of the human's choice, which means minimizing the loss **−log sigmoid(score_chosen − score_rejected)**: the same binary cross-entropy from the classification lesson, on a score difference. It's near 0 when the chosen response already scores much higher, and large when the reward model has them backwards.

            ## RLHF: optimizing against the reward model
            **Reinforcement learning from human feedback (RLHF)** then trains the assistant itself:

            1. The assistant writes responses to many prompts.
            2. The reward model scores them.
            3. The assistant is updated to make high-scoring responses more likely (classically with an algorithm called PPO).
            4. A penalty discourages drifting too far from the SFT model, measured by how different their probabilities are (a **KL penalty**).

            The penalty matters because of **reward hacking**: the reward model is an imperfect stand-in for what people want, and a model optimized hard against it finds its blind spots. Answers get longer, more flattering or more confident in ways that score well but aren't actually better. Keeping the model near its starting point limits the damage. Anthropic's **Constitutional AI** replaces many of the human comparisons with comparisons by a model guided by a written set of principles (RLAIF, "RL from AI feedback").

            ## DPO: preferences without a reward model
            **Direct Preference Optimization** (DPO, 2023) gets a similar result more simply. It skips the separate reward model and the reinforcement-learning loop. For each pair, compute how much **more** likely the current model makes the chosen response than a frozen reference copy does, and the same for the rejected response, both as log-probability differences. The loss pushes the first margin above the second:

            > DPO loss = −log sigmoid(β × [(log π(chosen) − log π_ref(chosen)) − (log π(rejected) − log π_ref(rejected))])

            In words: raise the chosen response's probability relative to the reference, lower the rejected one's, and β controls how hard to push (and so how far the model may drift). The log-probability of a whole response is the **sum** of its tokens' log-probabilities, as in the softmax lesson. DPO and its variants are widely used for open models because they're stable and cheap.

            ## RL with verifiable rewards and reasoning models
            Some tasks don't need human judgment because the answer can be **checked**: a math problem has a known answer, code either passes its tests or doesn't. **Reinforcement learning with verifiable rewards** samples many attempts per problem, rewards the correct ones, and makes them more likely. A popular method, **GRPO**, compares each attempt with the other attempts at the same problem: its **advantage** is how much better its reward is than the group's average, divided by the group's spread. Better-than-average attempts are reinforced; worse ones discouraged. No separate value model is needed.

            Trained this way at scale, models learn to write long chains of reasoning before answering: checking work, trying alternatives, backtracking. These are **reasoning models**, and they can trade more output tokens (**test-time compute**) for better answers on hard problems. How much of that written reasoning reflects the model's actual internal process is an open research question.

            ## What the symbols stand for
            - **r_A, r_B**: reward-model scores for two responses.
            - **σ(x)**: the sigmoid, 1 / (1 + exp(−x)).
            - **π(y | x)**: the probability the model being trained gives response y to prompt x. **π_ref**: the same for a frozen reference copy.
            - **β**: how strongly DPO pushes, a **hyperparameter** such as 0.1.
            - **advantage** = (reward − mean of the group's rewards) / (spread of the group's rewards), in GRPO.

            ## Walk through one problem
            DPO with β = 0.1. Log-probabilities: the policy gives the chosen response −40 and the rejected −42; the reference gives the chosen −41 and the rejected −41.

            1. Chosen margin: −40 − (−41) = +1. The policy already likes the chosen answer more than the reference does.
            2. Rejected margin: −42 − (−41) = −1. It likes the rejected answer less.
            3. Difference: 1 − (−1) = 2. Times β: 0.2.
            4. Loss: −log σ(0.2) = −log(0.550) ≈ 0.598. Below log 2 ≈ 0.693 (the loss at zero difference), so the model is moving the right way; training keeps pushing the difference up.

            ## Explain it to your team
            "After supervised fine-tuning, models are tuned on preferences: which of two answers was better. A reward model learns to score answers from those comparisons, and RLHF trains the assistant to earn high scores without drifting too far, since it will exploit the reward model's blind spots. DPO does it directly from the pairs. Where answers can be checked, like math and code, reinforcement learning rewards correct attempts, which is how reasoning models learn to think step by step."
            """,
            """
            Bradley–Terry:   P(A ≻ B) = σ(r_A − r_B)               σ(0) = 0.5   σ(2) ≈ 0.88
            reward model loss = −log σ(r_chosen − r_rejected)
            DPO loss = −log σ(β · [(log π(c) − log π_ref(c)) − (log π(r) − log π_ref(r))])
            GRPO advantage = (reward − mean(group rewards)) / spread(group rewards)
            """,
            LlmCode(
                "Preference losses",
                "Implement the losses behind reward models, DPO and GRPO. Plain Python and `math`. Compute −log σ(x) stably: it must not overflow for x = −1000 or x = 1000.",
                [
                    "`bradley_terry(score_a, score_b)` returns the probability that A is preferred: σ(score_a − score_b)",
                    "`reward_model_loss(chosen_score, rejected_score)` returns −log σ(chosen − rejected), computed stably",
                    "`dpo_loss(policy_chosen, policy_rejected, ref_chosen, ref_rejected, beta)` returns the DPO loss from the four sequence log-probabilities",
                    "`group_advantages(rewards)` returns (reward − mean) / population spread for each reward, or all zeros when every reward is equal",
                ],
                """
                import math


                def bradley_terry(score_a, score_b):
                    ...


                def reward_model_loss(chosen_score, rejected_score):
                    ...


                def dpo_loss(policy_chosen, policy_rejected, ref_chosen, ref_rejected, beta):
                    ...


                def group_advantages(rewards):
                    ...
                """,
                "−log σ(x) equals log(1 + exp(−x)). For large negative x that overflows, so use `-x + math.log1p(math.exp(x))` when x < 0, and `math.log1p(math.exp(-x))` otherwise.",
                ["Bradley–Terry probabilities", "stable reward-model loss", "DPO loss", "group-relative advantages"])
        ),
        new(
            "llm-evaluation",
            "How frontier models are made",
            "Evaluation: measuring what a model can really do",
            "Benchmarks, pass@k for code, contamination checks, the biases of LLM judges, and error bars: how to tell whether a model is actually better, and for your use.",
            "Evaluate models on held-out tasks that match how they'll be used, with automatic scoring where possible: exact match, multiple-choice accuracy, unit tests for code (pass@k, estimated without bias from n samples). Check that test items didn't leak into training data. LLM judges are useful but biased toward position, length and their own style, so swap orders and check consistency. Every score has sampling error, roughly √(p(1 − p)/n), so small differences on small test sets are often noise.",
            [
                ("llm-generalization", "evaluation is held-out testing, with the same leak problems."),
                ("llm-probability", "pass@k and error bars are probability calculations."),
                ("llm-pretraining-scale", "contamination happens because benchmarks end up in pretraining data."),
            ],
            """
            ## The idea in plain words
            "Model X is better than model Y" is one of the most repeated and least checked claims in AI. Evaluation is the discipline of making such claims honestly: what exactly was measured, on what data, how the answers were scored, and how sure we can be that a difference isn't luck.

            ## Kinds of evaluation
            - **Benchmarks:** fixed public test sets. Multiple-choice knowledge tests (score: accuracy), math word problems (score: exact match on the final answer), coding problems (score: do the unit tests pass?), and agent tasks such as resolving real GitHub issues in a repository. Useful for comparison, but they saturate (top models score near 100%) and leak into training data.
            - **Human evaluation:** people compare outputs side by side. Expensive and slow, but it measures what matters for open-ended tasks. Public arenas collect many such votes and rank models with Bradley–Terry scores, the same model as the previous lesson.
            - **LLM-as-judge:** a strong model grades or compares outputs using a rubric. Cheap and fast, with known biases (below).
            - **Your own evals:** a set of tasks drawn from your actual use case, with checks you trust. For any real product decision, including your capstone, this matters more than any leaderboard.

            ## pass@k for code
            Code models are often given k attempts: does **at least one** of k samples pass the tests? That's **pass@k**. The naive way to estimate it (generate exactly k samples, check if any passes, repeat) is noisy. The standard method, from OpenAI's Codex paper, generates **n ≥ k** samples per problem, counts the **c** that pass, and computes the exact probability that a random set of k from those n contains at least one passing sample:

            > pass@k = 1 − (number of ways to choose k samples all from the n − c failures) / (number of ways to choose k from all n)

            If c = 0, pass@k is 0. If fewer than k samples failed (n − c < k), every set of k must include a pass, so it's 1. Otherwise compute the ratio as a running product, (1 − k/(n − i)) for i = n − c + 1 … n, rather than with huge factorials. Average over problems.

            ## Contamination
            If test questions appeared in the training data, a high score may mean memorization, not ability: the leak problem from the generalization lesson, at internet scale. Benchmarks get posted, discussed and copied all over the web, so they end up in pretraining crawls. Checks include searching the training data for long overlapping word sequences (**n-gram overlap**), testing on newly written problems released after the model's training cutoff, and comparing performance on original versus lightly reworded questions. A big drop on the reworded version is a warning sign.

            ## LLM judges and their biases
            Studies of LLM judges have found consistent biases:

            - **Position bias:** preferring whichever answer is shown first (or second).
            - **Length bias:** preferring longer answers, even when they're not better.
            - **Self-preference:** preferring answers in their own style or from their own model family.

            Mitigations: judge each pair twice with the order swapped and count only consistent verdicts; use a written rubric with specific criteria; check the judge's agreement with human labels on a sample before trusting it.

            ## Error bars
            A test set is a sample of the possible questions, so every score has **sampling error**. For accuracy p measured on n independent items, the **standard error** is about √(p(1 − p) / n), and a rough **95% confidence interval** is p ± 1.96 standard errors.

            Example: 80% accuracy on 100 items has a standard error of √(0.8 × 0.2 / 100) = 0.04, so the interval is about 72% to 88%. A rival scoring 83% on the same 100 items is not clearly better. When comparing two models on the **same** items, a **paired** comparison (looking at the items where they disagree) is more sensitive than comparing the two intervals. Also, sampling with temperature above 0 adds run-to-run variation: run more than once, or report the setting.

            ## What the symbols stand for
            - **n**: samples generated per problem (pass@k), or test items (accuracy).
            - **c**: how many of the n samples passed.
            - **k**: the number of attempts allowed, a **setting** such as 1 or 10.
            - **p**: measured accuracy, a fraction from 0 to 1.
            - **SE** = √(p(1 − p)/n): the standard error. **95% interval** ≈ p ± 1.96·SE.

            ## Walk through one problem
            A model generates n = 10 solutions to a problem and c = 3 pass. What are pass@1 and pass@5?

            1. pass@1: the chance that one random sample passes, 3/10 = 0.3. (The formula gives the same thing.)
            2. pass@5: the chance that 5 random samples are all failures is (7 choose 5) / (10 choose 5) = 21 / 252 = 1/12 ≈ 0.083.
            3. So pass@5 = 1 − 0.083 ≈ 0.917. Five attempts almost always include a working solution, even though each one succeeds only 30% of the time.
            4. That's why an agent that can run tests and retry is far more capable than its pass@1 suggests.

            ## Explain it to your team
            "We judge models on held-out tasks that match real use, with automatic checks where we can: exact answers, or unit tests for code, scored as pass@k. We check that test questions didn't leak into training data, and we don't fully trust model judges, which favor long answers and whichever answer comes first. And every score has error bars: on a hundred questions, a three-point difference is usually noise."
            """,
            """
            pass@k = 1 − C(n − c, k) / C(n, k)         n = 10, c = 3: pass@1 = 0.3, pass@5 ≈ 0.917
            accuracy 80% on 100 items: SE = √(0.8 × 0.2 / 100) = 0.04 → 95% interval ≈ 72%–88%
            contamination: fraction of a test item's n-grams that appear in the training text
            judge consistency: agree on both orders (A,B) and (B,A), or don't count it
            """,
            LlmCode(
                "Honest scores",
                "Implement pass@k, accuracy with a confidence interval, a contamination check and a judge-consistency check. Standard library only.",
                [
                    "`pass_at_k(n, c, k)` returns the unbiased estimate 1 − C(n − c, k) / C(n, k): 0 when c is 0, 1 when n − c < k, otherwise computed as a running product without huge factorials",
                    "`accuracy_with_interval(correct)` takes a non-empty list of booleans and returns `(accuracy, low, high)` with the 95% interval accuracy ± 1.96 × √(p(1 − p)/n), clipped to [0, 1]",
                    "`ngram_overlap(test_text, train_text, n)` returns the fraction of the test text's word n-grams (lowercased, split on whitespace) that also occur in the training text, and 0.0 when the test text has no n-grams",
                    "`judge_consistency(first_order, swapped_order)` takes two lists of verdicts ('A', 'B' or 'tie'), the second judged with the answers swapped, and returns the fraction of items where the verdicts agree once the swap is undone",
                ],
                """
                import math


                def pass_at_k(n, c, k):
                    ...


                def accuracy_with_interval(correct):
                    ...


                def ngram_overlap(test_text, train_text, n):
                    ...


                def judge_consistency(first_order, swapped_order):
                    ...
                """,
                "For pass@k, start with `product = 1.0` and multiply by `1 - k / i` for every i from `n - c + 1` to `n`; the answer is `1 - product`. To undo the swap, map 'A' to 'B', 'B' to 'A', and leave 'tie' alone.",
                ["pass@k", "accuracy with a confidence interval", "n-gram contamination check", "judge consistency under swapping"])
        ),
    ];
}
