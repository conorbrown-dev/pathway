# How AI works: LLMs end to end

Course id `ai-llm-engineering` · languageId `llm` · lessons in `backend/Pathway.Api/Llm*.cs` · graders in `sandbox-broker/src/evaluator.mjs` (`llm-*` cases)
Last reviewed 2026-10-26

## Who it's for

Working developers and students who use AI tools and want to understand them all the way down, then build and ship their own model. The course assumes:

- comfortable use of a computer and a terminal (Pathway's Linux CLI course covers this);
- arithmetic, and the idea of a variable. Everything else, including the Python and the math, is taught inside the course before it is used. The Precalculus course is a helpful companion but not a requirement.

By the end, a learner can explain every stage of how a modern LLM is built and used, has written the core pieces (tokenizer, autograd, attention, sampling, an agent loop) from scratch, has trained a small GPT, has fine-tuned and evaluated an open-weight model, serves it locally, and runs it inside opencode next to Claude and/or OpenAI's Codex models. The last lesson turns that into a presentation for a team or class.

## Authoring rules

1. **Nothing before its prerequisites.** Every lesson declares the earlier lessons it builds on in `LlmPrerequisites`. The lesson page shows them as "What you need first", generated from that table, and `CurriculumInvariantTests` fails if a lesson depends on a later one. A concept that isn't covered by an earlier lesson is taught in full where it first appears.
2. **Words first, symbols second.** Every formula is explained in a sentence before it is written down, every letter or symbol is named as a variable or a constant with example values ("What the symbols stand for"), and every step of a worked example says why it is done. This follows the Precalculus rules in `docs/precalculus-curriculum.md`.
3. **Tie every idea to something real.** Each lesson grounds the idea in an everyday or game situation and in what it does inside a real LLM.
4. **Build it, then use the library.** Core mechanisms are implemented in plain Python (standard library only) and graded in the sandbox. PyTorch and the Hugging Face stack appear only after the learner has built the idea by hand, in local labs on their own machine.
5. **Explain it to your team.** Every lesson ends with a short "Explain it to your team" section: a few sentences the learner could say out loud. Together these form the outline of the final presentation.
6. **Say what isn't known.** Where research is unsettled (why scaling laws hold, how much models "understand", interpretability), say so plainly instead of picking a side.
7. **Honest tooling notes.** Versions and tools move fast. Lessons that name a tool carry a version stamp and a source link, teach the stable idea (an OpenAI-compatible endpoint, a context limit, a chat template) and treat specific commands as current examples.

`CurriculumInvariantTests` enforces rules 1, 2 (a "What the symbols stand for" or "What the words mean" section) and 5 structurally, and checks that every code exercise has a sandbox grader.

## Grading

- **Code exercises** run in the `tools` sandbox image with Python 3.11 and the standard library only, no network, 1 vCPU and a 12-second limit. Learners write functions; the grader imports them and runs examples, edge cases and, where it fits, generated cases. Every grader has `*.pass.py` and `*.fail.py` reference solutions in `sandbox-broker/test/python-solutions/`, run by `npm test` and again inside the built image by `scripts/fixtures-in-image.mjs`.
- **Local labs** (PyTorch, training a GPT, fine-tuning, serving, opencode) can't run in the sandbox: no GPU, no network, short time limit. They are Presentation exercises with a concrete evidence checklist (loss curves, sample outputs, eval tables, config files) for the learner's portfolio.
- **Multiple choice** is used only where the skill is judgment rather than computation.

## Modules

| Module | Lessons |
| --- | --- |
| AI foundations | What AI is · the Python you need · data as numbers · models are functions with knobs |
| Math foundations, in words | Vectors and the dot product · matrices · probability · exponentials, logs and softmax · rates of change and the chain rule |
| Learning from data | Loss · gradient descent · generalization · classification |
| Neural networks | Neurons and layers · backpropagation (build an autograd engine) · training networks (minibatches, Adam) · PyTorch lab |
| Language as data | Text to numbers · tokenization with BPE · next-token prediction · embeddings |
| Transformers | Why attention · self-attention · multi-head attention and position · the transformer block · sampling and decoding · lab: train a tiny GPT |
| How frontier models are made | Pretraining at scale · instruction tuning · preference tuning and RL · evaluation |
| LLMs in real systems | Inference memory and quantization · context engineering · tool calling and agent harnesses · retrieval · safety and security |
| Capstone: your model in opencode | Dataset · LoRA fine-tuning · evaluation against the base and a frontier model · serving locally · opencode with Claude/Codex · presenting AI end to end |

## The capstone

The learner picks a narrow, checkable job a small model can learn, such as writing commit messages in their team's style, labelling support tickets, or reviewing test names. They:

1. build and validate a dataset of chat examples (graded validator);
2. fine-tune a small open-weight instruct model with LoRA on their own GPU, Apple Silicon (MLX-LM), or a free cloud notebook;
3. evaluate base vs. fine-tuned vs. a frontier model on a held-out set they wrote before training;
4. merge, convert to GGUF, quantize and serve it with llama.cpp's `llama-server` (or Ollama) behind an OpenAI-compatible endpoint;
5. register it in opencode as a custom provider and run it as a specialist subagent, with Claude or a Codex model as the primary agent;
6. present the whole pipeline, with their numbers, to a team or class.

Before that, the Transformers module ends with training a small GPT from scratch, so the learner has seen every stage once at a scale that runs on a laptop.

## Sources

- Andrej Karpathy, "Neural Networks: Zero to Hero", micrograd and nanoGPT (the build-it-by-hand sequence this course follows in spirit)
- Vaswani et al., "Attention Is All You Need" (2017)
- Hoffmann et al., "Training Compute-Optimal Large Language Models" (2022)
- Ouyang et al., "Training language models to follow instructions with human feedback" (2022); Rafailov et al., "Direct Preference Optimization" (2023)
- Hu et al., "LoRA: Low-Rank Adaptation of Large Language Models" (2021)
- Chen et al., "Evaluating Large Language Models Trained on Code" (2021), for pass@k
- PyTorch 2.14 documentation; Hugging Face Transformers v5, PEFT and TRL documentation; llama.cpp and Ollama documentation
- opencode documentation: providers and agents (V1 and V2 configuration, last checked 2026-10-26)
