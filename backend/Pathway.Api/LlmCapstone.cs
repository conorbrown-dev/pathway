static partial class Curriculum
{
    // Module 9 of the AI & LLM course: the capstone. The learner picks a narrow job, builds and
    // validates a dataset, LoRA fine-tunes a small open-weight model, evaluates it against the base
    // and a frontier model, serves it locally behind an OpenAI-compatible endpoint, runs it as an
    // opencode subagent beside Claude or a Codex model, and presents the whole pipeline.
    private static LlmLessonSpec[] LlmCapstoneSpecs() =>
    [
        new(
            "llm-capstone-dataset",
            "Capstone: your model in opencode",
            "Capstone 1: choose a job and build the dataset",
            "Pick a narrow, checkable job a small model can learn, write the evaluation set before anything else, then build clean training data in the chat format with no leaks into the evaluation set.",
            "A fine-tuning project succeeds or fails on its data. Choose a job that is narrow, frequent and checkable, where a small model can be fast, private and cheap. Write a held-out evaluation set first, so it can't be shaped by what the model learns. Then collect or generate training examples in the same chat format the model will see at inference, deduplicate them, and split by a hash of the input so no example (or its near-copy) lands in both sets.",
            [
                ("llm-instruction-tuning", "your training data is a set of chat conversations rendered with a template."),
                ("llm-generalization", "deterministic splits and leak checks keep your evaluation honest."),
                ("llm-evaluation", "you'll design the evaluation before the training data."),
                ("llm-build-tiny-gpt", "you've trained a model from scratch once; now you'll adapt a real one."),
            ],
            """
            ## The plan for the capstone
            You'll take a small open-weight model, teach it one job well, and run it as a **specialist** inside opencode next to a frontier model (Claude or one of OpenAI's Codex models) that stays in charge. The frontier model handles the open-ended work; your model handles its narrow job quickly, cheaply, privately and offline. Then you'll present the whole pipeline. The seven capstone lessons are the steps.

            ## Choosing the job
            The right job is the most important decision in the project. Look for one that is:

            - **Narrow:** one kind of input, one kind of output. "Write a commit message for this diff" is narrow; "help with coding" isn't.
            - **Checkable:** you can tell automatically, or quickly by hand, whether an output is right. Exact labels, valid JSON with the right fields, a format with rules.
            - **Frequent:** it comes up often enough that a faster, cheaper, local model is worth having.
            - **Learnable by a small model:** pattern-following, formatting, classification, short transformations. Small models are poor at broad knowledge and long reasoning; don't fight that.
            - **Safe to get wrong sometimes:** a human reviews the output before it matters.

            Good examples:

            - Turn a staged diff into a commit message in your team's exact style.
            - Label support tickets or bug reports with a fixed set of categories and a severity, as JSON.
            - Name a test from a description of the behavior, following your naming convention.
            - Summarize a failing CI log into "what failed, where, probable cause" in three lines.
            - Convert a plain-English request into one of a fixed set of CLI commands for your project.

            Write a one-paragraph **job spec**: the input, the exact output format, three examples, and how you'll score an output.

            ## Write the evaluation set first
            Before collecting any training data, write **50 to 100 evaluation examples** by hand or from real data, with correct outputs. Doing this first matters: once you've seen training data and model outputs, you'll unconsciously write evaluation items that the model handles well. Include the hard and boring cases: very short inputs, very long ones, ambiguous ones, inputs that should produce "unknown."

            ## Collecting training data
            Aim for **300 to 2,000 examples** to start; small, clean datasets go a long way with LoRA. Sources:

            - **Your own history:** your git log (diffs and their commit messages), resolved tickets with their labels, past CI failures with their fixes. Real data teaches the real distribution.
            - **Synthetic data from a frontier model:** describe the job and have a strong model generate varied inputs and outputs, then **review** them; a fast human pass catches systematic errors. Check the provider's terms of service: some restrict using their model outputs to train other models.
            - **Augmenting:** vary inputs (rename identifiers, reorder, shorten) while keeping outputs correct.

            Keep personal data, secrets and anything confidential out of the dataset, or remove it before training (your redaction function from the security lesson helps).

            ## The format
            Each example is one line of **JSONL** (one JSON object per line) in the conversational format that Hugging Face TRL and MLX-LM both read:

            ```json
            {"messages": [{"role": "system", "content": "Write a commit message in the team style."}, {"role": "user", "content": "<the diff>"}, {"role": "assistant", "content": "fix(auth): refresh tokens before expiry"}]}
            ```

            Use the **same system prompt** in every example, and the same one at inference. The model learns the job in the context of that prompt.

            ## Cleaning and splitting
            1. **Validate** every line: valid JSON, a `messages` list, allowed roles, the user speaking before the assistant, an assistant message last, no empty content.
            2. **Deduplicate** on the normalized user content: identical inputs teach nothing new and can leak across splits.
            3. **Split by hash** of the normalized user content, as in the generalization lesson, so the same input always lands in the same split and re-running the script never shuffles examples between training and evaluation.
            4. **Check for leaks** between your training set and your hand-written evaluation set, and remove any training example whose input matches an evaluation input.

            ## What the words mean
            - **Job spec:** a short definition of the input, the output format and how outputs are scored.
            - **Evaluation set:** held-out examples with correct outputs, written before training and never trained on.
            - **JSONL:** a text file with one JSON object per line.
            - **Synthetic data:** examples generated by a model, then reviewed.

            ## Walk through one problem
            You collect 1,200 commit examples from your repository's history.

            1. Validation drops 40 malformed or empty lines: 1,160 left.
            2. Deduplication on normalized diffs removes 85 repeats (merge commits, reverts, cherry-picks): 1,075 left.
            3. Hash-splitting with a 10% validation fraction gives about 970 for training and 105 for validation (used to watch the loss during training).
            4. The leak check against your 80 hand-written evaluation examples finds 6 whose diffs also appear in training; remove those 6 training examples. Your evaluation now measures the job, not memory.

            ## Explain it to your team
            "We picked a narrow, checkable job a small model can learn, and wrote the evaluation set before collecting any training data, so it couldn't be biased by what we saw. Training data came from our own history plus reviewed synthetic examples, all in the same chat format and system prompt we'll use in production. We validated, deduplicated, split by a hash of the input, and removed anything that overlapped the evaluation set."
            """,
            """
            job spec → eval set (50–100, written first) → training data (300–2,000) → validate → dedup → hash split → leak check
            {"messages": [{"role": "system", "content": "…"}, {"role": "user", "content": "…"}, {"role": "assistant", "content": "…"}]}
            """,
            LlmCode(
                "Clean a fine-tuning dataset",
                "Write the dataset pipeline for your capstone: parse and validate JSONL chat examples, deduplicate, split deterministically and check for leaks. Standard library only.",
                [
                    "`parse_examples(jsonl_text)` returns `(examples, errors)`: one example per valid non-blank line, and one error string per invalid line that starts with `line N:` (counting from 1); a valid line is a JSON object with a `messages` list of role/content dicts (roles system/user/assistant), at most one system message first, a user message before the first assistant message, an assistant message last, and no empty or whitespace-only content",
                    "`user_key(example)` returns the normalized text (lowercased, whitespace collapsed) of all user messages joined by a single newline, and `dedupe(examples)` keeps the first example for each key, in order",
                    "`split(examples, validation_fraction)` returns `(train, validation)`, sending an example to validation when `zlib.crc32(user_key(example).encode()) / 2**32 < validation_fraction`, preserving order",
                    "`remove_leaks(train, evaluation)` returns `(clean_train, removed_count)`, dropping every training example whose `user_key` matches an evaluation example's",
                ],
                """
                import json
                import zlib


                def parse_examples(jsonl_text):
                    ...


                def user_key(example):
                    ...


                def dedupe(examples):
                    ...


                def split(examples, validation_fraction):
                    ...


                def remove_leaks(train, evaluation):
                    ...
                """,
                "Loop with `for number, line in enumerate(jsonl_text.splitlines(), start=1)`, skip blank lines, and wrap `json.loads` in try/except `json.JSONDecodeError`. Write the message checks as a helper that returns an error message or None.",
                ["parsing and validating lines", "normalized keys and deduplication", "deterministic hash split", "removing evaluation leaks"])
        ),
        new(
            "llm-lora-finetuning",
            "Capstone: your model in opencode",
            "Capstone 2: LoRA, the math of cheap fine-tuning",
            "Instead of updating billions of weights, freeze the model and learn a small low-rank correction for selected layers. Understand why that works, how many parameters it trains, and how the adapter is merged back in.",
            "Full fine-tuning updates every weight and needs the full training memory budget. LoRA freezes the original weight matrix W and learns a low-rank update made of two thin matrices, B (d_out × r) and A (r × d_in), added as (α/r)·B·A·x to the layer's output. With rank r of 8 to 64 this trains well under 1% of the parameters, so a small model can be fine-tuned on one consumer GPU, especially with the frozen weights quantized to 4 bits (QLoRA). After training, the update can be merged into W, so serving costs nothing extra.",
            [
                ("llm-capstone-dataset", "you'll train on the dataset you built."),
                ("llm-matrices", "LoRA is a statement about matrix shapes and products."),
                ("llm-inference-memory-quantization", "QLoRA keeps the frozen weights quantized."),
                ("llm-training-networks", "the adapter is trained with AdamW, warmup and clipping."),
            ],
            """
            ## The problem with full fine-tuning
            Fine-tuning a 3-billion-parameter model the ordinary way means updating all 3 billion weights, with gradients and Adam state for each: about 16 bytes per parameter, or roughly 48 GB, beyond any consumer GPU. You'd also end up with a full 6 GB copy of the model for every job you fine-tune.

            ## The LoRA idea
            **LoRA** (Low-Rank Adaptation, 2021) starts from an observation: the *change* a fine-tune makes to a weight matrix is usually simple. It can be described well by far fewer numbers than the matrix holds. So instead of changing W, **freeze it**, and learn a small correction alongside it:

            > output = W·x + (α / r) · B · (A · x)

            - **A** is an r × d_in matrix: it squeezes the input down to just r numbers.
            - **B** is a d_out × r matrix: it expands those r numbers back up to the output size.
            - **r**, the **rank**, is small: 8, 16, 32. The product B·A is a full d_out × d_in matrix, but one with limited "richness," since every column is a combination of only r directions. That's what "low-rank" means.
            - **α / r** is a fixed scaling factor. α ("alpha") is a hyperparameter, often set to r or 2r, so changing r doesn't change how big the update is.

            B starts at **zero**, so at the beginning of training the correction is exactly zero and the model behaves exactly like the original. A starts small and random. Only A and B are trained.

            ## How many parameters?
            For one d_out × d_in matrix, LoRA trains r × d_in (for A) + d_out × r (for B) = **r × (d_in + d_out)** numbers, instead of d_in × d_out.

            For a 2,048 × 2,048 attention projection at rank 16: 16 × 4,096 = 65,536 trainable numbers instead of 4,194,304, about 1.6%. Applied to the attention and MLP projections of a whole small model, LoRA typically trains **0.1% to 1%** of the parameters. Gradients and Adam state are only needed for those, which is where the memory saving comes from.

            **QLoRA** goes further: store the frozen W in 4 bits (the quantization lesson), compute in 16 bits, and train 16-bit adapters on top. A 3-billion-parameter model's frozen weights then take about 1.5–2 GB, and fine-tuning fits on an 8–12 GB GPU or a free cloud notebook.

            ## Merging
            After training, compute W′ = W + (α/r)·B·A once and save that. The merged layer gives exactly the same outputs as the frozen-W-plus-adapter version, but it's an ordinary matrix again: no extra cost at inference, and the model can be converted and quantized like any other. Alternatively, keep the small adapter file (a few megabytes) separate and load it on top of the base model when needed, which is handy for having several specialists share one base.

            ## Choosing the settings
            - **Target modules:** the attention projections (often named q_proj, k_proj, v_proj, o_proj) and the MLP projections. Targeting all of them usually works best.
            - **Rank r:** 8–32 for narrow jobs. Higher isn't always better; it can overfit small datasets.
            - **α:** equal to r or 2r.
            - **Learning rate:** around 1e-4 to 2e-4, higher than full fine-tuning because only new parameters are learning, with warmup and decay.
            - **Epochs:** 1 to 3 over a small dataset. Watch the validation loss; when it rises, you're memorizing.
            - **Loss on assistant tokens only**, as in the instruction-tuning lesson.

            A known risk is **forgetting**: training hard on a narrow task can degrade general behavior. LoRA's small update limits it, and your evaluation will catch it if your job relies on general skills.

            ## What the symbols stand for
            - **W**: the frozen weight matrix, d_out × d_in.
            - **A**: r × d_in, **B**: d_out × r, the trainable adapter matrices.
            - **r**: the rank, a **hyperparameter** such as 16.
            - **α**: the scaling hyperparameter; the update is multiplied by α / r.
            - **x**: the layer's input vector, length d_in.
            - **W′** = W + (α/r)·B·A: the merged weight.
            - Trainable numbers per matrix: **r(d_in + d_out)**.

            ## Walk through one problem
            W is 2 × 2, all zeros except W = [[1, 0], [0, 1]] (the identity). Rank r = 1, α = 2, A = [[1, 1]] (1 × 2), B = [[0.5], [0]] (2 × 1), and x = [2, 4].

            1. Original output: W·x = [2, 4].
            2. A·x = [1·2 + 1·4] = [6]: the input squeezed to one number.
            3. B·[6] = [3, 0], then times α/r = 2: [6, 0].
            4. Output: [2, 4] + [6, 0] = [8, 4].
            5. Merged: B·A = [[0.5, 0.5], [0, 0]], times 2 = [[1, 1], [0, 0]], so W′ = [[2, 1], [0, 1]], and W′·x = [2·2 + 1·4, 4] = [8, 4]. The same answer, as merging promises.

            ## Explain it to your team
            "Full fine-tuning updates every weight, which needs far more memory than we have. LoRA freezes the model and learns a small low-rank correction for selected layers, two thin matrices whose product is added to each layer's output. It trains well under 1% of the parameters, and with the frozen weights in 4 bits it fits on a single consumer GPU. After training we merge the correction back into the weights, so serving the fine-tuned model costs nothing extra."
            """,
            """
            output = W·x + (α / r) · B · (A · x)        A: r × d_in   B: d_out × r   (B starts at 0)
            trainable per matrix = r · (d_in + d_out)    2,048 × 2,048 at r = 16 → 65,536 vs 4,194,304
            merge: W′ = W + (α / r) · B · A               same outputs, no extra cost at inference
            """,
            LlmCode(
                "LoRA by hand",
                "Implement the LoRA forward pass, merging, and parameter counting with matrices as lists of rows. Plain Python only. The grader checks that the merged layer gives the same outputs as the adapter.",
                [
                    "`lora_parameters(d_in, d_out, rank)` returns rank × (d_in + d_out), and `adapter_parameters(shapes, rank)` sums it over a list of `(d_in, d_out)` shapes",
                    "`lora_forward(W, A, B, x, alpha, rank)` returns W·x + (alpha / rank)·B·(A·x), without ever forming the full B·A product",
                    "`merge_lora(W, A, B, alpha, rank)` returns a new matrix W + (alpha / rank)·B·A, leaving W unchanged",
                    "`trainable_fraction(total_params, shapes, rank)` returns adapter_parameters(shapes, rank) / total_params",
                ],
                """
                def lora_parameters(d_in, d_out, rank):
                    ...


                def adapter_parameters(shapes, rank):
                    ...


                def lora_forward(W, A, B, x, alpha, rank):
                    ...


                def merge_lora(W, A, B, alpha, rank):
                    ...


                def trainable_fraction(total_params, shapes, rank):
                    ...
                """,
                "Write a `matvec` helper (each row dotted with the vector). In `lora_forward`, compute `h = matvec(A, x)` (length rank), then `matvec(B, h)`, scale it, and add it to `matvec(W, x)`. For merging, entry (i, j) of B·A is `sum(B[i][k] * A[k][j] for k in range(rank))`.",
                ["parameter counting", "forward pass through the adapter", "merging leaves W alone and matches the adapter", "trainable fraction"])
        ),
        new(
            "llm-capstone-train",
            "Capstone: your model in opencode",
            "Capstone 3: fine-tune your model",
            "Run the real fine-tune: pick a base model and check its license, choose your hardware path (NVIDIA GPU, Apple Silicon, or a free cloud notebook), train a LoRA adapter on your dataset, watch the losses, and merge the result.",
            "A capstone fine-tune takes a small open-weight instruct model (roughly 0.5 to 4 billion parameters, with a license that allows your use and a chat template with tool support), trains a LoRA adapter on the assistant tokens of your dataset with TRL on an NVIDIA GPU or cloud notebook, or MLX-LM on Apple Silicon, and watches training and validation loss to stop before it memorizes. The merged model, saved in Hugging Face format, is what you evaluate and serve next.",
            [
                ("llm-lora-finetuning", "you'll train exactly the adapter you just implemented."),
                ("llm-pytorch-lab", "the training stack runs on PyTorch."),
                ("llm-safety-security", "check the base model's license before you build on it."),
            ],
            """
            ## Choose a base model
            Pick a small **instruct** model (already post-trained to follow instructions), not a base model: you're adapting an assistant, not teaching one from scratch. Criteria:

            - **Size:** 0.5–4 billion parameters fine-tunes comfortably on consumer hardware and serves fast. Start small; move up only if your evaluation says you must.
            - **License:** read it. Apache 2.0 and MIT allow commercial use; some models restrict it or require attribution.
            - **Chat template with tool support,** if your specialist will run as an opencode subagent, because opencode's agents call tools. Many recent small instruct models ship one; check the model card.
            - **Supported by llama.cpp,** so you can convert it to GGUF (most popular architectures are).

            As of late 2026, small Apache-2.0 instruct models from the Qwen 3 and Qwen 3.5 families (for example 1.7B, 2B or 4B) are common choices for exactly this; check current releases and benchmarks on the Hugging Face Hub when you start, because this changes month to month.

            ## Choose a hardware path
            **Path A, NVIDIA GPU (8 GB+) or a free cloud notebook (Colab or Kaggle GPU):** Hugging Face TRL with PEFT.

            ```python
            # pip install -U torch transformers datasets trl peft
            from datasets import load_dataset
            from peft import LoraConfig
            from trl import SFTConfig, SFTTrainer

            data = load_dataset("json", data_files={"train": "train.jsonl", "validation": "validation.jsonl"})
            config = SFTConfig(
                output_dir="adapter",
                num_train_epochs=2,
                learning_rate=1e-4,
                lr_scheduler_type="cosine",
                warmup_steps=20,
                per_device_train_batch_size=4,
                gradient_accumulation_steps=4,      # effective batch of 16
                assistant_only_loss=True,           # loss on assistant tokens only
                eval_strategy="steps",
                eval_steps=50,
                logging_steps=10,
                max_grad_norm=1.0,
            )
            trainer = SFTTrainer(
                model="Qwen/Qwen3-1.7B",            # your chosen base model
                args=config,
                train_dataset=data["train"],
                eval_dataset=data["validation"],
                peft_config=LoraConfig(r=16, lora_alpha=32, target_modules="all-linear", task_type="CAUSAL_LM"),
            )
            trainer.train()
            trainer.save_model("adapter")
            ```

            `assistant_only_loss=True` needs the model's chat template to mark the assistant's turns; TRL handles this for common model families and tells you if your template doesn't support it. **Gradient accumulation** adds up gradients over several small batches before each optimizer step, so you get the effect of a bigger batch in less memory. For tighter memory, load the base in 4 bits (QLoRA) with bitsandbytes, or use Unsloth, a library that speeds up LoRA training and cuts its memory use.

            To merge: load the base model and the adapter with PEFT, call `merge_and_unload()`, and `save_pretrained("merged")` along with the tokenizer.

            **Path B, Apple Silicon (16 GB+ unified memory):** MLX-LM, Apple's framework for its own chips. Put `train.jsonl` and `valid.jsonl` (the same `messages` format) in a `data` folder, then:

            ```sh
            pip install -U mlx-lm
            python -m mlx_lm.lora --model Qwen/Qwen3-1.7B --train --data ./data --iters 600 --batch-size 4 --learning-rate 1e-4 --adapter-path ./adapter
            python -m mlx_lm.fuse --model Qwen/Qwen3-1.7B --adapter-path ./adapter --save-path ./merged
            ```

            Start from the full-precision model (as above) if you'll convert to GGUF next; fusing an already-quantized MLX model needs `--de-quantize`. Run `python -m mlx_lm.lora --help` for your installed version's options.

            ## Watch the run
            Log training loss and validation loss. Healthy: both fall, then validation flattens. Stop (or use the checkpoint from) when validation loss is lowest; if it rises while training loss keeps falling, you're memorizing. Typical runs on a few thousand short examples take minutes to an hour.

            Then try it: load the merged model, give it three evaluation inputs it has never seen, using the exact system prompt from training, and read the outputs. Don't score yet; that's the next lesson.

            ## What the words mean
            - **Instruct model:** a model already post-trained to follow instructions.
            - **Gradient accumulation:** summing gradients over several batches before one optimizer step.
            - **QLoRA:** LoRA with the frozen base weights stored in 4 bits.
            - **Adapter:** the trained LoRA matrices, saved separately (a few megabytes).
            - **Merged model:** base weights with the adapter folded in, saved as an ordinary model.

            ## Explain it to your team
            "We started from a small open-weight instruct model with a license that fits our use, and trained a LoRA adapter on our dataset, computing the loss only on the assistant's replies. We watched validation loss and kept the checkpoint where it was lowest. Then we merged the adapter into the weights, so we have an ordinary model to evaluate and serve."
            """,
            """
            base: small instruct model (0.5–4B), license checked, tool-capable chat template, llama.cpp-supported
            NVIDIA / cloud notebook: TRL SFTTrainer + LoraConfig(r=16, lora_alpha=32, target_modules="all-linear")
            Apple Silicon:           python -m mlx_lm.lora … --train   then   python -m mlx_lm.fuse … --save-path ./merged
            watch: train loss ↓, validation loss ↓ then flat → keep the lowest-validation checkpoint
            """,
            LlmLab(
                "Fine-tune your model",
                "Run the fine-tune on your dataset and keep the evidence below in your capstone repository. This is milestone 2 of your capstone.",
                [
                    "Your base model's name, size, license (with a link) and why it fits your job",
                    "Your hardware path and the exact training command or script, with its hyperparameters",
                    "The training and validation loss curve, and which checkpoint you kept and why",
                    "The trainable-parameter count reported by the trainer, compared with your adapter_parameters estimate",
                    "Three unseen inputs with the merged model's outputs, using the training system prompt",
                ],
                "If you run out of memory: lower the batch size and raise gradient accumulation, load the base in 4 bits, shorten the maximum sequence length, or pick a smaller base model."),
            LlmLabStamp("TRL · PEFT · MLX-LM", "https://huggingface.co/docs/trl/sft_trainer")),
        new(
            "llm-capstone-evaluate",
            "Capstone: your model in opencode",
            "Capstone 4: evaluate against the base and a frontier model",
            "Score the base model, your fine-tuned model and a frontier model on the evaluation set you wrote first, compare them item by item, and decide with evidence whether your specialist earns its place.",
            "Run every model on the same held-out evaluation set with the same prompt and settings, score each output with the check from your job spec, and compare models on the same items: count where one wins, loses or ties, and use a sign test to see whether the difference could be chance. Report quality alongside speed and cost, and look at the failures, not just the averages.",
            [
                ("llm-capstone-train", "you'll evaluate the model you trained."),
                ("llm-evaluation", "paired comparisons and error bars come from that lesson."),
                ("llm-context-engineering", "outputs that should be JSON are parsed and validated."),
            ],
            """
            ## The comparison that matters
            You now have three candidates for your job:

            1. the **base** model you started from, prompted with your system prompt and a few examples;
            2. your **fine-tuned** model;
            3. a **frontier** model (Claude or a Codex model), prompted the same way.

            The question isn't "is my model good?" but "**which model should do this job, at what cost?**" A fine-tune that matches the frontier model on your job, runs locally in a fraction of a second, costs nothing per call and keeps data private is a clear win. One that's barely better than its own base model isn't worth maintaining.

            ## Running the evaluation fairly
            - Same evaluation items, same system prompt, same decoding settings (temperature 0, or a fixed seed and several runs) for every model.
            - Score with the check from your job spec, written as code wherever possible: exact match for labels; for JSON, parse it and compare the fields that matter; for format rules, a validator. Where judgment is unavoidable, use a rubric, and if you use a model as judge, swap orders and check it against a sample you scored yourself.
            - Record speed (time to first token, total time per item) and cost per 1,000 items.
            - Save every output. You'll want to read the failures.

            ## Comparing two models properly
            Both models answered the **same** items, so compare them **item by item** (a **paired** comparison):

            - **Wins:** items where model A scored higher than model B.
            - **Losses:** items where B scored higher.
            - **Ties:** items where they scored the same, including both right and both wrong. Ties say nothing about which is better, so set them aside.

            If the two models were really equally good, each non-tied item would be a coin flip: A wins half the time. The **sign test** asks how surprising your result would be under that coin-flip assumption. Count the non-tied items n and A's wins w. The **p-value** is the probability that a fair coin, flipped n times, gives a result at least as lopsided as w, counting both directions. Computing it means adding up binomial probabilities: the chance of exactly j heads in n flips is C(n, j) / 2ⁿ, where C(n, j) counts the ways to choose j of the n flips.

            A small p-value (below 0.05 is the usual line) means the difference is unlikely to be chance. A large one means your evaluation set can't tell the models apart. That's not proof they're equal; you may need more items.

            ## Read the failures
            Averages hide patterns. Sort the fine-tuned model's failures into groups: wrong format, wrong label on a particular kind of input, too long, invented details. Each group suggests a fix: more training examples of that kind, a clearer system prompt, a stricter output validator, or a decision that this case goes to the frontier model instead.

            ## What the words mean
            - **Paired comparison:** comparing two models on the same items, one item at a time.
            - **Win / loss / tie:** whether A scored higher than, lower than, or equal to B on an item.
            - **Sign test:** a test of whether wins and losses differ more than coin flips would.
            - **p-value:** the probability of a result at least this lopsided if the models were truly equal.

            ## Walk through one problem
            On 80 evaluation items, your fine-tune is right on 66 and the base model on 51. Item by item: 18 items where only the fine-tune is right, 3 where only the base is right, and 59 ties.

            1. Non-tied items: n = 18 + 3 = 21, with w = 18 wins.
            2. Under coin flips, a result at least this lopsided means 18 or more heads, or 3 or fewer. Each tail is (C(21,0) + C(21,1) + C(21,2) + C(21,3)) / 2²¹ = (1 + 21 + 210 + 1,330) / 2,097,152 ≈ 0.00074.
            3. Two-sided p ≈ 2 × 0.00074 ≈ 0.0015. Very unlikely to be chance: the fine-tune is genuinely better than its base on this job.
            4. Repeat against the frontier model. If that comparison is 9 wins to 11 losses, p is large: on this job they're roughly level, and yours is local, fast and free.

            ## Explain it to your team
            "We ran the base model, our fine-tune and a frontier model on the same held-out evaluation set with the same prompt and scored them automatically. Compared item by item, the fine-tune beats its base by a margin a sign test says isn't chance, and it's roughly level with the frontier model on this narrow job, while running locally in a fraction of the time and cost. We read the failures, too, and they tell us what to fix next."
            """,
            """
            score every model on the same eval items, same prompt, temperature 0
            paired:  wins = A right, B wrong · losses = B right, A wrong · ties set aside
            sign test: n = wins + losses; p = 2 × P(Binomial(n, ½) ≤ min(wins, losses)), capped at 1
            example: 18 wins, 3 losses → p ≈ 0.0015
            """,
            LlmCode(
                "Score and compare models",
                "Write the scoring and comparison code for your capstone evaluation. Standard library only.",
                [
                    "`score_output(expected, output, mode)` returns 1.0 or 0.0: mode `'exact'` compares after stripping surrounding whitespace; mode `'json'` parses the output as JSON (0.0 if it doesn't parse) and requires every key in the `expected` dict to have an equal value",
                    "`paired_counts(scores_a, scores_b)` returns `(wins, losses, ties)` for model A against model B, item by item",
                    "`sign_test_p(wins, losses)` returns the two-sided exact sign-test p-value: 2 × P(Binomial(n, ½) ≤ min(wins, losses)) with n = wins + losses, capped at 1.0, and 1.0 when n is 0",
                    "`summarize(scores)` returns `(accuracy, low, high)` with the 95% interval from the evaluation lesson, clipped to [0, 1]",
                ],
                """
                import json
                import math


                def score_output(expected, output, mode):
                    ...


                def paired_counts(scores_a, scores_b):
                    ...


                def sign_test_p(wins, losses):
                    ...


                def summarize(scores):
                    ...
                """,
                "`math.comb(n, j)` gives C(n, j). The lower tail is `sum(math.comb(n, j) for j in range(k + 1)) / 2 ** n` with `k = min(wins, losses)`. For JSON mode, catch `json.JSONDecodeError` and also return 0.0 when the parsed value isn't a dict.",
                ["exact and JSON scoring", "paired wins, losses and ties", "exact sign test", "accuracy with an interval"])
        ),
        new(
            "llm-serve-local-model",
            "Capstone: your model in opencode",
            "Capstone 5: serve your model behind an OpenAI-compatible API",
            "Convert the merged model to GGUF, quantize it, serve it with llama.cpp's llama-server (or Ollama), and talk to it over the same chat-completions API that most tools speak, including streamed replies and tool calls.",
            "Most tools, opencode included, talk to models through an OpenAI-compatible chat-completions API: POST a JSON body with the model name, messages and optional tools to /v1/chat/completions, and read back a message or a stream of server-sent events whose deltas you join into text and tool calls. To serve your model that way, convert the merged Hugging Face model to GGUF with llama.cpp's converter, quantize it, and run llama-server with its chat template enabled for tool calling, or import the GGUF into Ollama.",
            [
                ("llm-capstone-evaluate", "you'll serve the model your evaluation chose."),
                ("llm-inference-memory-quantization", "you'll pick a quantization level and check the memory math."),
                ("llm-tool-calling-agents", "the API carries tool definitions and tool calls."),
            ],
            """
            ## From training format to serving format
            Your merged model is in **Hugging Face format**: safetensors weight files, a config and a tokenizer. llama.cpp and Ollama, the most popular local runtimes, use **GGUF**: a single file holding the weights (usually quantized), the tokenizer and the chat template.

            ```sh
            git clone https://github.com/ggml-org/llama.cpp
            pip install -r llama.cpp/requirements.txt
            python llama.cpp/convert_hf_to_gguf.py ./merged --outfile my-model-f16.gguf --outtype f16
            # build llama.cpp (see its README), or install a release, to get llama-quantize and llama-server
            llama-quantize my-model-f16.gguf my-model-Q4_K_M.gguf Q4_K_M
            ```

            Re-run a few evaluation items on the quantized model. If quality dropped noticeably, try `Q5_K_M` or `Q8_0`.

            ## Run the server
            ```sh
            llama-server -m my-model-Q4_K_M.gguf --jinja -c 16384 --port 8080
            ```

            - `--jinja` uses the model's own chat template, which llama.cpp needs for **tool calling**.
            - `-c` sets the context size; the KV cache for it must fit in memory alongside the weights.
            - The server now speaks the OpenAI-compatible API at `http://127.0.0.1:8080/v1`.

            **Ollama alternative:** write a file called `Modelfile` containing `FROM ./my-model-Q4_K_M.gguf`, run `ollama create my-model -f Modelfile`, then `ollama run my-model`. Ollama serves the same API at `http://localhost:11434/v1`. For tool-heavy use, give it a large enough context (its `num_ctx` parameter), because a small default context can break tool calls.

            ## The chat-completions API
            A request is an HTTP POST to `/v1/chat/completions` with a JSON body:

            ```json
            {
              "model": "my-model",
              "messages": [{"role": "system", "content": "…"}, {"role": "user", "content": "…"}],
              "temperature": 0,
              "stream": true,
              "tools": [{"type": "function", "function": {"name": "read_file", "description": "…", "parameters": {"type": "object", "properties": {"path": {"type": "string"}}, "required": ["path"]}}}]
            }
            ```

            Without streaming, the reply's `choices[0].message` holds `content` (text) or `tool_calls`. Each tool call has an `id`, and a `function` with a `name` and `arguments`, where the arguments are a **JSON string**, not an object, so parse them.

            With `"stream": true`, the server sends **server-sent events**: lines starting with `data: `, each holding a small JSON chunk, and a final `data: [DONE]`. Each chunk's `choices[0].delta` carries a fragment: a piece of `content`, or a piece of a tool call. Tool calls arrive in fragments too, identified by an `index`: the first fragment for an index carries the `id` and the function `name`; later fragments append more characters to `arguments`. The client's job is to join the fragments back together. That's exactly what opencode does with your model's output, and it's this lesson's exercise.

            Check your server with curl before plugging it into anything:

            ```sh
            curl http://127.0.0.1:8080/v1/chat/completions -H "Content-Type: application/json" \
              -d '{"model": "my-model", "messages": [{"role": "user", "content": "Say hello"}]}'
            ```

            ## What the words mean
            - **GGUF:** llama.cpp's single-file model format, with weights, tokenizer and chat template.
            - **OpenAI-compatible API:** the de facto standard HTTP interface for chat models: `/v1/chat/completions`, `messages`, `tools`, streamed deltas.
            - **Server-sent events (SSE):** a streaming format of `data: …` lines over one HTTP response.
            - **Delta:** one streamed fragment of the reply.
            - **finish_reason:** why generation stopped: `stop` (finished), `length` (hit the token limit) or `tool_calls`.

            ## Walk through one problem
            A stream delivers these chunks (shown as just their deltas):

            1. `{"role": "assistant", "content": ""}` — the start.
            2. `{"tool_calls": [{"index": 0, "id": "call_1", "function": {"name": "read_file", "arguments": ""}}]}` — the tool call begins.
            3. `{"tool_calls": [{"index": 0, "function": {"arguments": "{\"path\": "}}]}` — part of the arguments.
            4. `{"tool_calls": [{"index": 0, "function": {"arguments": "\"a.txt\"}"}}]}` — the rest.
            5. A chunk with `finish_reason: "tool_calls"`, then `data: [DONE]`.

            Joined: one tool call, id `call_1`, name `read_file`, arguments `{"path": "a.txt"}`, which parses to the object `{"path": "a.txt"}`.

            ## Explain it to your team
            "We converted the merged model to GGUF, quantized it to about 4.5 bits per weight after checking that quality held, and serve it locally with llama.cpp behind the standard OpenAI-compatible API. Any tool that speaks that API, including opencode, can use it, with streaming and tool calls."
            """,
            """
            merged (HF) → convert_hf_to_gguf.py → my-model-f16.gguf → llama-quantize … Q4_K_M → llama-server --jinja -c 16384 --port 8080
            API: POST http://127.0.0.1:8080/v1/chat/completions   {model, messages, tools?, temperature, stream}
            stream: data: {"choices": [{"delta": {"content": "Hel"}}]} … data: [DONE]
            tool calls stream as fragments keyed by index; join the argument strings, then json.loads
            """,
            LlmCode(
                "Talk to an OpenAI-compatible server",
                "Build chat-completions requests and assemble a streamed reply, the way opencode does with your model. Standard library only. No network: the grader feeds you recorded streams.",
                [
                    "`build_request(model, messages, tools=None, temperature=0.0, stream=True)` returns the request dict with `model`, `messages`, `temperature` and `stream`, and adds `tools` (each wrapped as `{'type': 'function', 'function': {...}}`) only when tools are given",
                    "`parse_sse(text)` returns the list of parsed JSON chunks from every `data: ` line, stopping at `data: [DONE]` and ignoring blank lines, comment lines (starting with `:`) and other fields",
                    "`assemble(chunks)` returns a dict with `content` (all content deltas joined), `tool_calls` (a list, in index order, of dicts with `id`, `name` and `arguments` parsed from the joined argument string) and `finish_reason` (the last non-null one)",
                    "Invalid tool-call argument JSON should raise `ValueError` naming the tool call's id",
                ],
                """
                import json


                def build_request(model, messages, tools=None, temperature=0.0, stream=True):
                    ...


                def parse_sse(text):
                    ...


                def assemble(chunks):
                    ...
                """,
                "Keep tool calls in a dict keyed by `index`, each holding `id`, `name` and a list of argument fragments; set `id` and `name` the first time they appear and append every `arguments` fragment. Read each delta with `chunk['choices'][0].get('delta', {})`.",
                ["building requests", "parsing the event stream", "joining content and tool-call fragments", "reporting broken tool arguments"])
        ),
        new(
            "llm-opencode-integration",
            "Capstone: your model in opencode",
            "Capstone 6: run your model in opencode beside Claude or Codex",
            "Register your local server as a custom provider in opencode, keep Claude or a Codex model as the primary agent, and add your model as a specialist subagent with only the permissions its job needs.",
            "opencode reads providers and agents from its config file. Your local server is a custom OpenAI-compatible provider: a base URL ending in /v1, plus each model's context and output limits so opencode can manage the window. The primary agent stays on a frontier model, connected with an API key (Claude) or a ChatGPT sign-in (Codex models). Your model becomes a subagent with a description that tells the primary agent when to delegate, and permission rules that deny everything its job doesn't need. opencode 1.x and 2.x name these keys differently, so check which version you run.",
            [
                ("llm-serve-local-model", "opencode talks to the server you just started."),
                ("llm-tool-calling-agents", "subagents, permissions and harnesses were introduced there."),
                ("llm-safety-security", "least privilege decides your subagent's permissions."),
            ],
            """
            ## The architecture
            Frontier models are best at open-ended work: understanding a codebase, planning, writing and fixing code. Your model is good at one narrow job, fast, local and free. So opencode runs both:

            - The **primary agent** (opencode's default `build` agent) uses Claude or a Codex model and does the main work.
            - Your model runs as a **subagent**: when the primary agent reaches your job (say, "write the commit message"), it delegates to your subagent, which runs in its own fresh context and returns the result.

            ## Install and connect the frontier model
            Install opencode following opencode.ai's instructions, then check the version with `opencode --version`. In September 2026, **opencode 2.0** shipped as `@opencode/cli`, and it renamed several config keys: `provider` became `providers`, `agent` became `agents`, and permissions became an ordered list of rules. The older `opencode-ai` package installs 1.x. Both are below; use the one that matches your version, and check opencode.ai's docs for anything that's changed since.

            Connect the frontier model with `/connect` inside opencode:

            - **Anthropic (Claude):** choose Anthropic and paste an API key from the Anthropic Console. opencode's docs note that Anthropic prohibits using Claude Pro/Max subscriptions through third-party plugins, so use an API key.
            - **OpenAI (Codex models):** choose OpenAI and sign in with ChatGPT, or enter an API key.

            Run `/models` to see the exact model IDs available to you, such as `anthropic/claude-sonnet-5-5`, and the current Codex model IDs.

            ## Register your server as a provider
            In your project's `opencode.jsonc`, opencode **2.x**:

            ```json
            {
              "$schema": "https://opencode.ai/config.json",
              "model": "anthropic/claude-sonnet-5-5",
              "providers": {
                "local": {
                  "name": "My fine-tuned models",
                  "package": "@opencode/ai/providers/openai-compatible",
                  "settings": { "baseURL": "http://127.0.0.1:8080/v1" },
                  "models": {
                    "commit-writer": { "name": "Commit writer (LoRA)", "limit": { "context": 16384, "output": 1024 } }
                  }
                }
              },
              "agents": {
                "commit-writer": {
                  "description": "Writes a commit message in the team's style from the staged diff. Use it whenever a commit message is needed.",
                  "mode": "subagent",
                  "model": "local/commit-writer",
                  "permissions": [
                    { "action": "*", "resource": "*", "effect": "deny" },
                    { "action": "read", "resource": "*", "effect": "allow" }
                  ]
                }
              }
            }
            ```

            The same in opencode **1.x** (`opencode.json`): use `provider` instead of `providers`, `"npm": "@ai-sdk/openai-compatible"` instead of `package`, `options` instead of `settings`, `agent` instead of `agents`, and a permission object such as `"permission": { "edit": "deny", "bash": "deny", "webfetch": "deny" }`.

            What each part does:

            - **`model`** (top level): the primary agent's model, your frontier model.
            - **Provider `baseURL`:** your llama-server (`http://127.0.0.1:8080/v1`) or Ollama (`http://localhost:11434/v1`). It must end in `/v1`.
            - **`limit`:** the context window you started the server with, and a maximum output. opencode uses these to manage context; standard providers fill them in automatically, local ones can't.
            - **Agent `description`:** this is what the primary agent reads when deciding whether to delegate, so say exactly what the subagent does and when to use it.
            - **`mode: subagent`** and **`model: "local/commit-writer"`**: provider ID, a slash, then the model key.
            - **Permissions:** deny by default, then allow only what the job needs. The commit writer needs to read the diff; it doesn't need to edit files, run shell commands or reach the web. The last matching rule wins, so the broad deny goes first.

            ## Use it
            Ask the primary agent to "use the commit-writer subagent to draft the commit message for my staged changes," or, in 1.x, mention it directly with `@commit-writer`. Watch what happens: the primary agent delegates, your model runs on your machine, and the result comes back. Try the same task with the primary agent alone and compare speed, cost and quality, using your evaluation from Capstone 4 as the real verdict.

            If your subagent's tool calls fail, check that llama-server was started with `--jinja`, that the context is large enough for opencode's system prompt and tool definitions (16k or more), and that your model's chat template supports tools. Small models are weaker at tool calling than frontier ones, which is another reason to keep their permissions and job narrow.

            ## What the words mean
            - **Provider:** a source of models in opencode, with a base URL and credentials.
            - **Primary agent:** the agent you talk to directly, here on a frontier model.
            - **Subagent:** an agent the primary agent can delegate to, with its own model, context and permissions.
            - **Permission rule:** an action (such as `read`, `edit`, `shell`), a resource pattern, and an effect (`allow`, `ask` or `deny`).

            ## Walk through one problem
            Your commit-writer subagent's permissions are `[deny everything, allow read]`. During a session it asks to run `git commit -m "…"`.

            1. The action is `shell`. Rules are checked in order and the last match wins.
            2. Rule 1 (`*`, deny) matches. Rule 2 (`read`, allow) doesn't match `shell`.
            3. Last match: deny. The subagent can't commit on its own; it returns the message and the primary agent, with the user's approval, makes the commit. That's least privilege doing its job.

            ## Explain it to your team
            "opencode runs both models. Claude or a Codex model is the primary agent and does the open-ended work. Our fine-tuned model is served locally and registered as a custom OpenAI-compatible provider, and it runs as a subagent with a description that says when to delegate to it and permissions that deny everything its job doesn't need. The primary agent hands it the narrow task, it answers locally in a fraction of a second, and the frontier model carries on."
            """,
            """
            primary agent (build): anthropic/claude-… or an OpenAI Codex model   ← /connect, /models
            provider "local": OpenAI-compatible, baseURL http://127.0.0.1:8080/v1, models.commit-writer.limit {context, output}
            subagent "commit-writer": mode subagent · model local/commit-writer · description says when to delegate
            permissions: deny * first, then allow only what the job needs (last matching rule wins)
            2.x keys: providers / package / settings / agents / permissions[]    1.x keys: provider / npm / options / agent / permission{}
            """,
            LlmCode(
                "Write your opencode config",
                "Write the opencode config (JSON, comments allowed) that registers your local server as a provider and adds your model as a least-privilege subagent, with a frontier model as the primary. Use either the 2.x shape or the 1.x shape from the lesson. The grader parses the JSON and checks its structure.",
                [
                    "A top-level `model` set to an `anthropic/…` or `openai/…` model, so a frontier model is the primary agent",
                    "A custom provider (`providers` with `package` and `settings`, or `provider` with `npm` and `options`) using the OpenAI-compatible package, whose `baseURL` is a local address (127.0.0.1 or localhost) ending in `/v1`, with at least one model that has numeric `limit.context` and `limit.output`",
                    "An agent (under `agents` or `agent`) with `mode` `subagent`, a `description` of at least 40 characters, and `model` set to `<your provider>/<your model key>`",
                    "Least privilege: in 2.x, the subagent's `permissions` list starts with a deny rule for action `*` and never allows `edit`, `shell` or `webfetch`; in 1.x, its `permission` object sets `edit`, `bash` and `webfetch` to `deny`",
                ],
                """
                {
                  "$schema": "https://opencode.ai/config.json",
                  // Primary agent: your frontier model
                  "model": "",
                  "providers": {
                    // Your llama-server or Ollama endpoint
                  },
                  "agents": {
                    // Your specialist subagent
                  }
                }
                """,
                "Start from the lesson's 2.x example and change the names to your own job. `//` comment lines are fine; the grader strips them before parsing. The subagent's `model` must be the provider key, a slash, and the key under that provider's `models`.",
                ["the frontier model is primary", "a local OpenAI-compatible provider with limits", "a well-described subagent on your model", "least-privilege permissions"])
        ),
        new(
            "llm-present-ai-end-to-end",
            "Capstone: your model in opencode",
            "Capstone 7: present AI end to end",
            "Turn the course and your project into a talk your team or class can follow: how AI works from data to deployment, what you built, the numbers, the limits, and what you'd do next.",
            "A strong end-to-end AI talk follows the pipeline: what a model is and how it learns, how text becomes tokens and vectors, what a transformer does, how pretraining and post-training make an assistant, how it's evaluated and served, and how harnesses and tools turn it into an agent. Then it grounds every claim in the speaker's own project: the job, the data, the fine-tune, the evaluation numbers with their uncertainty, a live demo, the costs, and the limits and risks. The \"Explain it to your team\" sections from every lesson are the script's raw material.",
            [
                ("llm-opencode-integration", "your demo is the subagent running in opencode."),
                ("llm-capstone-evaluate", "your numbers come from the evaluation."),
                ("llm-build-tiny-gpt", "your from-scratch GPT shows the mechanism at small scale."),
            ],
            """
            ## Who you're talking to
            Before writing a slide, decide who's in the room and what they should be able to do afterward. A team of engineers wants to know whether to use your specialist and how it fails. A class wants the mental model. A manager wants cost, risk and payoff. The core story is the same; the depth and the ending change.

            ## The story arc
            Aim for about 20–30 minutes plus questions, roughly one slide per idea:

            1. **The hook (1 slide):** your demo's result, up front. "This model writes our commit messages locally in 0.4 seconds, as well as Claude does on our evaluation."
            2. **What a model is (1–2):** a function with billions of knobs, trained by gradient descent to reduce a loss. Use the line-fitting picture.
            3. **Text to numbers (1):** tokens and embeddings. Show your BPE tokenizer splitting a sentence.
            4. **The transformer (2):** attention as "every token looks back at the others and blends what's relevant," then the block. Show attention weights from your tiny GPT if you can.
            5. **How an assistant is made (2):** pretraining (data, compute, scale), then post-training (SFT, preferences, RL). Show your tiny GPT's samples next to an assistant's answer: same architecture, very different scale and training.
            6. **Measuring it (1):** evaluation, contamination, error bars.
            7. **Running it (1):** memory, quantization, the API, tools and harnesses.
            8. **Your project (3–4):** the job and why it suits a small model; the dataset (and that you wrote the evaluation first); the fine-tune; the evaluation table for base, fine-tune and frontier model, with paired results and the sign test; speed and cost.
            9. **Live demo (1):** opencode delegating to your subagent. Have a recording ready in case the demo fails.
            10. **Limits and risks (1):** where it fails (from your failure analysis), hallucination, prompt injection, licensing, what you deliberately didn't let it do.
            11. **What's next (1):** the next improvement your failures point to, and when you'd use a frontier model instead.

            ## Use your own "Explain it to your team" lines
            Every lesson in this course ended with a short spoken explanation. Collect them in order: they're the narration for sections 2–7. Rewrite each in your own words; if you can't, that's the lesson to revisit before the talk.

            ## Make numbers honest and visual
            - Show the evaluation as a small table: model, accuracy with its 95% interval, median latency, cost per 1,000 calls.
            - Say what the evaluation set is and how it was built (written first, held out, no leaks).
            - Never present a difference your sign test can't support as a win; "roughly level" is a fine, honest result.
            - One clear chart beats three busy ones: the training and validation loss curve tells the fine-tuning story in a glance.

            ## Prepare for the questions you'll get
            Write a short answer for each before you present:

            - "Does it learn from our conversations?" (No: weights are frozen at inference; only context changes.)
            - "Why not just use Claude for everything?" (Cost, latency, privacy, offline use, measured quality on this job.)
            - "How do you know it's not just memorizing?" (Held-out evaluation written first, hash split, leak checks.)
            - "What happens when it's wrong?" (Human review, narrow permissions, failures route to the frontier model.)
            - "Is it safe to give agents tools?" (Least privilege, sandboxing, injection, the lethal trifecta.)
            - "How big a GPU do we need?" (The memory arithmetic for your model and context.)
            - "Does it actually understand?" (What's measurable, and what's an open research question.)

            ## Rehearse
            Run the talk aloud twice, once timed. Run the demo on the machine and network you'll present from. Keep the recording one click away.

            ## What the words mean
            - **Narrative arc:** the order of ideas that takes listeners from what they know to what you want them to understand.
            - **Hook:** the opening that tells listeners why the talk is worth their attention.
            - **Backup recording:** a pre-recorded demo to show if the live one fails.

            ## Explain it to your team
            "Here's how AI works, from data to deployment, and here's a model I built and run next to Claude: what it does, how I trained it, how I measured it against the base model and a frontier model, what it costs, where it fails, and how we keep it safe."
            """,
            """
            hook → what a model is → tokens & embeddings → the transformer → pretraining & post-training
            → evaluation → serving & harnesses → your project (job, data, fine-tune, eval table) → live demo
            → limits & risks → what's next → questions
            """,
            LlmLab(
                "Give the talk",
                "Prepare and deliver your end-to-end AI presentation to a team, class, study group or recorded audience. This is the final milestone of your capstone.",
                [
                    "Your slides (10–16 slides following the story arc), with the evaluation table and loss curve",
                    "Your speaker notes, built from your own 'Explain it to your team' lines",
                    "A demo recording of opencode delegating to your subagent",
                    "Written answers to the seven likely questions in the lesson",
                    "A recording or a short written reflection after presenting: what landed, what confused people, and what you'd change",
                ],
                "If you don't have an audience yet, record the talk and share it with one person for feedback. Presenting to even one listener surfaces the gaps."),
            LlmLabStamp("Your capstone", "https://opencode.ai/docs/agents/")),
    ];
}
