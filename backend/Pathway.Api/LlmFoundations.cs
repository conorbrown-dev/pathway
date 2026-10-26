static partial class Curriculum
{
    // Module 1 of the AI & LLM course: what AI is, the Python the course uses, turning the world
    // into numbers, and the idea of a model as a function with adjustable knobs.
    private static LlmLessonSpec[] LlmFoundationsSpecs() =>
    [
        new(
            "llm-what-ai-is",
            "AI foundations",
            "What AI is, and the map of this course",
            "Rules you write versus rules a machine finds from examples, and where large language models sit in that picture.",
            "Machine learning replaces hand-written rules with a procedure that adjusts a program's numbers until its outputs match examples. A large language model is a very large learned program whose single training job was predicting the next piece of text, later tuned to be a helpful assistant and wrapped in software that lets it use tools.",
            [],
            """
            ## The idea in plain words
            There are two ways to get a computer to do something.

            The first is the way most software is written: **you write the rules**. A tax program follows the tax code line by line. A game's scripted enemy follows a build order a designer typed in: "build two barracks, then attack at minute five." If the rules are right, the program is right, and you can read exactly why it did what it did.

            The second way is **learning from examples**. Instead of writing the rules, you write a program full of adjustable numbers, show it thousands or billions of examples of the behavior you want, and run a procedure that nudges those numbers until the program's outputs match the examples. Nobody types the final rules in. They end up stored, spread across the numbers, in a form that is hard to read.

            Why bother with the second way? Because for many tasks nobody can write the rules down. Try writing rules that recognize a cat in any photo, or that continue any sentence in a sensible way, or that decide whether an email is spam when spammers change tactics every week. People do these things without being able to explain how. Learning from examples lets a computer do them too.

            ## The map
            These terms are nested, each inside the one before it:

            - **Artificial intelligence (AI)** is the broadest term: any technique that makes a computer do something we'd call intelligent if a person did it. That includes hand-written rule systems, search (chess engines exploring moves), planning (a delivery route optimizer) and learning.
            - **Machine learning (ML)** is the part of AI where behavior is learned from data instead of written by hand.
            - **Deep learning** is machine learning with **neural networks**: programs built from many layers of simple numeric operations. "Deep" just means many layers. You'll build one from scratch in the neural networks module.
            - **Generative models** are models that produce new content: text, images, audio, code.
            - **Large language models (LLMs)** are deep neural networks trained on huge amounts of text to predict the next piece of text. Claude, GPT and Gemini models, Llama, Qwen and Mistral models are all LLMs.

            A chat assistant like Claude is more than the raw model. It's an LLM that has been through extra training to follow instructions and behave safely (**post-training**), served on large computers, and run inside software (a **harness**) that formats the conversation, lets the model call tools such as a file editor or a web search, and shows you the result. Claude Code, OpenAI's Codex CLI and opencode are harnesses for coding. Part of this course's project is plugging a model you trained into one of them.

            ## Two phases: training and inference
            Every machine-learned system lives two lives.

            - **Training** is when the adjustable numbers change. It happens before you ever use the model, and for a frontier LLM it takes months on thousands of specialized chips. The numbers are called **parameters** or **weights**.
            - **Inference** is using the trained model: you give it an input and it produces an output. The parameters are frozen during inference.

            This clears up a common misunderstanding. When you chat with an assistant, the model is not learning from you in that moment; its weights don't change. If a tool "remembers" something from yesterday, the harness saved some text and put it back into what the model reads today. Its **context**, the text it can see right now, changed. The model didn't.

            ## What the words mean
            - **Model:** the learned program, meaning its structure plus its parameter values.
            - **Parameters (weights):** the adjustable numbers inside a model. Small models have thousands; current LLMs have billions to trillions.
            - **Dataset:** the collection of examples a model learns from.
            - **Training:** the procedure that adjusts parameters so outputs match the examples better.
            - **Inference:** running a trained model to get outputs.
            - **Token:** a small chunk of text (often a word or part of a word) that an LLM reads and writes one at a time. You'll build a tokenizer later.
            - **Context window:** how much text, measured in tokens, a model can take into account at once.
            - **Harness:** the software around a model that manages the conversation, tools and permissions.
            - **Open-weight model:** a model whose trained parameters are published so you can download and run them yourself. **Closed** models, such as Claude, are only available through the company's service.

            ## The pipeline this course follows
            Every LLM you've used went through the same stages, and the modules follow them in order:

            1. **Math and learning basics.** How a program can learn at all: numbers, functions, error, and nudging parameters downhill.
            2. **Neural networks.** Layers of simple operations, and backpropagation, the method that works out how to nudge millions of parameters at once.
            3. **Text as data.** Turning text into tokens, and tokens into numbers.
            4. **The transformer.** The network design behind every modern LLM, built around attention, which lets each token look at the others.
            5. **Pretraining.** Training on a huge text collection to predict the next token. This is where most knowledge and skill comes from.
            6. **Post-training.** Instruction tuning and preference tuning turn a text predictor into an assistant.
            7. **Evaluation.** Measuring what a model can and can't do, honestly.
            8. **Serving and harnesses.** Running models efficiently, giving them tools, and keeping them safe.
            9. **Your capstone.** You fine-tune a small open-weight model for a job you choose, measure it, serve it on your own machine, and run it in opencode alongside Claude or OpenAI's Codex models.

            ## Honest limits and common myths
            - **"It's just autocomplete."** The training objective really is predicting the next token. But predicting text well, across code, math and conversation, turns out to require a lot of structure about the world, so "just" undersells it. Both halves are true: the objective is simple, and what it produces is capable.
            - **"It looks answers up in a database."** A plain LLM doesn't. What it "knows" is stored in its weights, imperfectly, which is why it can state false things confidently (**hallucination**). Tools like web search and retrieval, covered later, give it real lookups.
            - **"Does it understand?"** That's an open research question, and the honest position is to describe what can be measured: what a model gets right, where it fails, and how reliably.

            ## Explain it to your team
            "Traditional software follows rules we write. Machine learning finds the rules itself by adjusting numbers until its outputs match examples. An LLM is a huge learned program trained to predict the next chunk of text. That training gives it broad knowledge and skill. Extra training turns it into an assistant, and a harness gives it tools. The model doesn't learn while we chat; only its context changes."
            """,
            """
            Hand-written rule (traditional program):
                if message contains "free money": mark as spam

            Learned behavior (machine learning):
                spam_score = 0.8 × (count of "free") + 1.3 × (links to unknown sites) − 0.6 × (sender in contacts) + …
                (the 0.8, 1.3, −0.6 … were found by training on thousands of labelled emails)

            Training: examples in, parameters change.
            Inference: parameters frozen, input in, output out.
            """,
            LlmChoice(
                "Where did that memory come from?",
                "A teammate says, \"Yesterday I told the coding assistant our naming conventions, and today it followed them in a brand-new chat. It learned them overnight.\" What is the most accurate explanation?",
                "context",
                [
                    new("weights", "The model's weights were updated overnight from yesterday's conversation"),
                    new("context", "The tool saved some text, such as a project instructions file or a memory note, and put it into the model's context today"),
                    new("database", "The model searches a database of every past conversation before it answers"),
                ],
                "During inference a model's parameters are frozen. Think about what changes between two chats if the weights don't.",
                "Distinguishes training from inference and context")
        ),
        new(
            "llm-python-toolkit",
            "AI foundations",
            "The Python you need for this course",
            "Values, lists, dictionaries, loops, functions and reproducible randomness: enough Python to build every idea in the course by hand.",
            "This course builds every mechanism in plain Python before using a library. You need values and their types, lists and dictionaries to hold data, loops to walk through it, functions with return values to package each step, and a seeded random number generator so experiments repeat exactly.",
            [("llm-what-ai-is", "you know what we're going to build with this code.")],
            """
            ## Why Python, and why by hand
            Almost all machine learning work happens in Python, because its libraries (PyTorch, NumPy, Hugging Face) are written for it. But libraries hide the ideas. In this course you first write each mechanism yourself, with nothing but plain Python: a tokenizer, a neural network, attention, a sampler, an agent loop. Once you've built it, the library version is just a faster copy of something you understand.

            This lesson covers exactly the Python the course uses, and nothing more. If you already know Python, skim it and do the exercise.

            ## Running Python
            Install Python 3.11 or newer from python.org (on macOS or Linux it may already be there: try `python3 --version`). Put code in a file such as `practice.py` and run it with `python3 practice.py`. Running `python3` with no file opens an interactive prompt where each line runs as you type it, which is handy for experiments.

            ## Values and types
            Every piece of data is a **value**, and every value has a **type** that decides what you can do with it:

            - `int`: whole numbers like `3` or `-40`.
            - `float`: numbers with a fractional part, like `0.25` or `-1.5e-3` (which means −0.0015). Machine learning is mostly floats.
            - `str`: text, written in quotes: `"hello"`.
            - `bool`: `True` or `False`.
            - `None`: "no value here."

            A **variable** is a name that points at a value: `learning_rate = 0.1`. The name is yours to choose; pick names that say what the value means.

            Two float facts that matter later. First, floats are approximations: `0.1 + 0.2` prints `0.30000000000000004`, because most decimals can't be stored exactly in binary. So never compare floats with `==`; check that they're close instead, as in `abs(a - b) < 1e-9`. Second, `7 / 2` is `3.5` (true division) while `7 // 2` is `3` (floor division, which drops the fraction).

            ## Lists: ordered collections
            A **list** holds values in order: `scores = [0.9, 0.4, 0.7]`.

            - Positions (indexes) start at **0**: `scores[0]` is `0.9`, and `scores[-1]` is the last item.
            - `len(scores)` is how many items there are: 3.
            - A **slice** takes a range: `scores[0:2]` is `[0.9, 0.4]`, from index 0 up to but not including 2.
            - `scores.append(0.2)` adds to the end. Lists can hold lists: `[[1, 2], [3, 4]]` is how we'll store a grid of numbers (a matrix) later.

            Lists are **mutable**: functions can change them in place. If a function shouldn't change its input, copy it first with `list(items)` or `items[:]`. Changing a caller's data by surprise is a classic bug, and this lesson's exercise checks for it.

            ## Dictionaries: look things up by key
            A **dictionary** maps keys to values: `counts = {"the": 12, "cat": 3}`. `counts["cat"]` is `3`. `counts.get("dog", 0)` returns `0` instead of failing when the key is missing. Assigning `counts["dog"] = 1` adds or replaces an entry. Dictionaries remember insertion order. We'll use them for vocabularies (word to number) and for counting.

            ## Loops
            A `for` loop runs its body once per item:

            ```python
            total = 0.0
            for score in scores:
                total += score          # same as total = total + score
            ```

            Three helpers you'll use constantly:

            - `range(n)` produces 0, 1, …, n − 1. `for i in range(len(scores))` walks the indexes.
            - `enumerate(items)` gives each index *and* item: `for i, score in enumerate(scores):`.
            - `zip(a, b)` walks two lists side by side: `for x, y in zip(inputs, targets):`.

            A **list comprehension** builds a new list in one line: `[s * 2 for s in scores]` doubles each score, and `[s for s in scores if s > 0.5]` keeps only the high ones.

            ## Functions
            A **function** packages a step under a name. It takes **parameters** (inputs) and **returns** a result:

            ```python
            def mean(values):
                if not values:
                    raise ValueError("mean of an empty list is undefined")
                return sum(values) / len(values)
            ```

            `return` hands a value back to the caller; a function that only `print`s gives the caller nothing to work with. `raise ValueError(...)` stops with an error when the input makes no sense, which is better than quietly returning a wrong number. Built-ins worth knowing: `sum`, `min`, `max`, `abs`, `round`, `sorted`.

            One more word collision to watch for: a Python function's inputs are also called "parameters," but a *model's* parameters are its learned numbers. Context tells you which is meant.

            ## The math module
            `import math` gives you `math.exp`, `math.log` (natural log), `math.sqrt` and `math.inf`. You'll learn what exp and log mean, and why ML needs them, in the math module.

            ## Reproducible randomness
            Machine learning uses randomness everywhere: starting values for parameters, shuffling examples, sampling words. But you want experiments you can repeat exactly, so a bug that appears once appears again. The trick is a **seed**:

            ```python
            import random
            rng = random.Random(42)   # its own generator, seeded with 42
            rng.random()              # a float in [0, 1); the same value every run with seed 42
            rng.shuffle(items)        # shuffles items in place, the same way every run
            ```

            The same seed always gives the same sequence. Keep your own `random.Random(seed)` object instead of the shared `random.shuffle`, so other code can't disturb your sequence.

            ## What the words mean
            - **Value / type:** a piece of data, and the kind of data it is (`int`, `float`, `str`, `bool`, `None`).
            - **Variable:** a name that refers to a value.
            - **Index:** a position in a list, starting at 0.
            - **Mutable:** can be changed in place; lists and dictionaries are.
            - **Return value:** what a function hands back to its caller.
            - **Seed:** the starting number for a random generator; the same seed gives the same sequence.

            ## Explain it to your team
            "We build each idea in plain Python before reaching for a library. That needs only lists, dictionaries, loops and functions that return values. We seed every random generator, so a run can be repeated exactly and a bug that shows up once can be reproduced."
            """,
            """
            import random

            def mean(values):
                if not values:
                    raise ValueError("mean of an empty list is undefined")
                return sum(values) / len(values)

            scores = [0.9, 0.4, 0.7]
            print(mean(scores))                       # 0.6666666666666666
            print([s for s in scores if s > 0.5])     # [0.9, 0.7]
            rng = random.Random(42)
            print(rng.random())                       # same number every run
            """,
            LlmCode(
                "Four small tools",
                "Write four functions you'll reuse all course. Use plain Python (no imports except `random`). Don't print; return values. Don't change the lists you're given.",
                [
                    "`mean(values)` returns the average of a non-empty list of numbers and raises `ValueError` for an empty list",
                    "`argmax(values)` returns the index of the largest number; if several tie, return the first of them",
                    "`word_counts(text)` lowercases the text, splits it on whitespace and returns a dict of word → count",
                    "`shuffled(items, seed)` returns a new list with the items shuffled by `random.Random(seed)`, leaving `items` unchanged",
                ],
                """
                import random


                def mean(values):
                    ...


                def argmax(values):
                    ...


                def word_counts(text):
                    ...


                def shuffled(items, seed):
                    ...
                """,
                "For `argmax`, keep the best index so far and replace it only when you see a *strictly* larger value. For `shuffled`, copy the list with `list(items)`, then call `random.Random(seed).shuffle(copy)` and return the copy.",
                ["mean and its empty-list error", "argmax with ties", "word counting", "seeded shuffle that leaves the input alone"])
        ),
        new(
            "llm-data-as-numbers",
            "AI foundations",
            "Data as numbers: features and encodings",
            "A model only ever sees numbers, so every input, from a game unit to a word, has to be turned into a list of numbers first.",
            "Models compute with numbers, so each example becomes a feature vector: a fixed-length list of numbers in an agreed order. Categories become one-hot vectors (a 1 in their own slot, 0 everywhere else) so the model doesn't invent a false order, and numeric features are standardized so no feature dominates just because of its units.",
            [("llm-python-toolkit", "you'll store examples as lists and vocabularies as dictionaries.")],
            """
            ## The idea in plain words
            A model is arithmetic. It can multiply and add numbers, and nothing else. It can't read the word "archer" or look at a photo. So before any learning happens, every example has to be turned into a list of numbers, always in the same order. That list is called a **feature vector**, and each position in it is a **feature**.

            How you turn things into numbers matters a lot. A bad encoding can tell the model things that aren't true, or hide things that are.

            ## Where you'll see it
            - **A game unit** might become `[health, speed, attack_range, is_flying]`: `[120, 2.5, 6, 0]`. The `is_flying` feature is 1 for yes and 0 for no.
            - **A house listing** becomes `[square_feet, bedrooms, age_in_years, distance_to_school_miles]`.
            - **A photo** is already numbers: a grid of pixels, each with red, green and blue brightness from 0 to 255. A 28 × 28 grayscale image is 784 numbers.
            - **Sound** is a list of air-pressure samples, often 16,000 per second.
            - **Text** is the hard one, and it gets its own module. As a preview, every word or word-piece gets an ID number from a **vocabulary**, and then a learned list of numbers called an **embedding**.

            ## Categories: why not just number them?
            Say units belong to a faction: Aeon, Cybran or UEF. The tempting move is Aeon = 0, Cybran = 1, UEF = 2. But then the model sees UEF as "twice Cybran" and Cybran as "halfway between Aeon and UEF." Those statements are nonsense, and a model that multiplies and adds will happily use them anyway.

            The fix is **one-hot encoding**: give each category its own slot, and set exactly one slot to 1.

            - Aeon → `[1, 0, 0]`
            - Cybran → `[0, 1, 0]`
            - UEF → `[0, 0, 1]`

            Now every faction is equally different from every other, which is the truth. To build this you need a **vocabulary**: a dictionary from each category to its slot number, such as `{"aeon": 0, "cybran": 1, "uef": 2}`. Vocabularies are usually built in order of first appearance, so the same data always produces the same slot numbers.

            This is exactly how an LLM starts reading text: each token in its vocabulary of tens of thousands of entries has an ID, and conceptually the input is a one-hot vector with a 1 in that token's slot. (In practice it skips building the giant vector and just looks the ID up, which you'll see in the embeddings lesson.)

            ## Numeric features: scale matters
            Square feet run in the thousands; bedrooms run from 1 to 5. If both feed into a weighted sum, the square-footage numbers are so much bigger that they swamp everything else at first, and training struggles. The usual fix is **standardization**: re-express each value as "how many typical spreads away from average is this?"

            In words, for one feature across all examples:

            1. Find the **mean**, the average value.
            2. Find the **standard deviation**, the typical distance of values from the mean. To get it, take each value's distance from the mean, square it (so negatives don't cancel positives), average those squares, then take the square root to get back to the original units.
            3. For each value, subtract the mean and divide by the standard deviation. The result is called a **z-score**.

            After standardizing, every feature has mean 0 and a spread of about 1, so they all start on equal footing. A z-score of +2 means "two standard deviations above average" whether the feature was feet or bedrooms.

            Edge case: if every value is identical, the standard deviation is 0 and you can't divide by it. The feature carries no information, so its standardized values are all 0.

            ## What the symbols stand for
            The z-score of a value is written **z = (x − μ) / σ**.

            - **x** is one value of the feature, a **variable**: say a house with 2,400 square feet.
            - **μ** (the Greek letter "mu") is the mean of that feature over the dataset, a **constant** once the data is fixed: say 1,800.
            - **σ** ("sigma") is the standard deviation, also a **constant** for the dataset: say 400.
            - **z** is the result: (2,400 − 1,800) / 400 = 1.5, so this house is one and a half spreads larger than average.

            ## Walk through one problem
            Unit speeds: 2, 4, 4, 6.

            1. Mean: (2 + 4 + 4 + 6) / 4 = 4. This is the center the values are measured from.
            2. Distances from the mean: −2, 0, 0, +2. Squared: 4, 0, 0, 4. Average of the squares: 8 / 4 = 2. Square root: about 1.414. That's the standard deviation.
            3. Z-scores: −2 / 1.414 ≈ −1.414; 0; 0; +2 / 1.414 ≈ +1.414. The slow unit is about 1.4 spreads below average, the fast one about 1.4 above.

            One subtlety: when you later split data into training and test sets, compute μ and σ from the training data only, then apply those same numbers to the test data. Otherwise information about the test set leaks into training. The generalization lesson covers this.

            ## Explain it to your team
            "A model only does arithmetic, so we turn every example into a fixed-length list of numbers. Categories get one-hot vectors so the model doesn't invent an order that isn't there. Numeric features get standardized so big units don't drown out small ones. Text works the same way: every token gets an ID from a vocabulary."
            """,
            """
            vocabulary = {"aeon": 0, "cybran": 1, "uef": 2}   # built in order of first appearance
            one_hot("cybran") → [0, 1, 0]

            speeds:     2      4    4    6
            mean μ = 4, standard deviation σ = √((4 + 0 + 0 + 4) / 4) = √2 ≈ 1.414
            z-scores: −1.414   0    0    1.414
            """,
            LlmCode(
                "Encode a unit roster",
                "Turn categories and measurements into model-ready numbers. Use plain Python and `math` only, and return values without printing.",
                [
                    "`one_hot(index, size)` returns a list of `size` zeros with a 1 at `index`; raise `ValueError` if `index` is outside 0 … size − 1",
                    "`build_vocabulary(labels)` returns a dict from each distinct label to a slot number, numbered 0, 1, 2 … in order of first appearance",
                    "`encode_labels(labels, vocabulary)` returns one one-hot list per label (raise `KeyError` for a label not in the vocabulary)",
                    "`standardize(values)` returns the z-scores using the population standard deviation (divide by the count); if all values are equal, return all zeros",
                ],
                """
                import math


                def one_hot(index, size):
                    ...


                def build_vocabulary(labels):
                    ...


                def encode_labels(labels, vocabulary):
                    ...


                def standardize(values):
                    ...
                """,
                "Build the vocabulary by walking the labels and adding each new one with `vocabulary[label] = len(vocabulary)`. For the standard deviation: average the squared distances from the mean, then take `math.sqrt`.",
                ["one-hot vectors and bad indexes", "vocabulary order", "label encoding", "z-scores, including a constant feature"])
        ),
        new(
            "llm-models-are-functions",
            "AI foundations",
            "A model is a function with knobs",
            "Inputs go in, a prediction comes out, and the parameters are the knobs that decide which prediction. Learning means finding good knob settings.",
            "A model is a function whose behavior is set by its parameters. A linear model multiplies each feature by a weight, adds them up and adds a bias. Learning means searching for parameter values that make predictions close to the targets, and an error measure such as the mean absolute error says how close.",
            [("llm-data-as-numbers", "a model's inputs are the feature vectors you built there.")],
            """
            ## The idea in plain words
            A **function** takes an input and gives back an output: put in a temperature in Celsius, get back Fahrenheit. A **model** is a function with adjustable **knobs**. Turning the knobs changes which output you get for the same input. Those knobs are the model's **parameters**, and **learning** is the process of finding knob settings that make the outputs useful.

            ## The simplest real model: weighted sum plus bias
            Suppose you want to predict how much damage a unit deals from two features: its level and its weapon tier. A natural guess is:

            > predicted damage = (some amount per level) × level + (some amount per tier) × tier + (a base amount)

            The "amount per level" and "amount per tier" are **weights**: how much each feature matters. The "base amount" is the **bias**: the prediction when every feature is zero. Multiply each feature by its weight, add those up, add the bias. That's a **linear model**, and it's the building block of every neural network, including the ones inside an LLM.

            ## What the symbols stand for
            The linear model is written **ŷ = w₁x₁ + w₂x₂ + b**.

            - **x₁, x₂** are the input features, **variables** that change with every example: level 5, tier 2.
            - **w₁, w₂** are the weights, **parameters** the model learns: say 12 damage per level and 30 per tier.
            - **b** is the bias, also a learned **parameter**: say 10.
            - **ŷ** ("y-hat") is the prediction: 12 × 5 + 30 × 2 + 10 = 130 damage. The hat means "estimated."
            - **y** (no hat) is the **target**, the true value from the data: say the unit really dealt 136.

            With more features you just keep going: w₁x₁ + w₂x₂ + w₃x₃ + … + b. Multiplying matching entries of two lists and adding them up has a name, the **dot product**, and the next module studies it properly.

            ## Three kinds of numbers
            It's easy to mix these up, so keep them apart from the start:

            - **Inputs (features)** come from each example. They change every time the model runs.
            - **Parameters** are learned during training and frozen during inference. They're the same for every example.
            - **Hyperparameters** are choices *you* make about the learning process, such as how many features to use, how big the model is, or how fast to adjust parameters. Training doesn't change them.

            When you hear "a 7B model," that means 7 billion parameters. Each one is a knob like w₁ above. Our damage model has 3.

            ## Measuring how wrong a prediction is
            To pick good parameters you need a score for "how wrong." The plainest one is the **mean absolute error** (MAE): for each example, take the gap between prediction and target, ignore the sign, and average those gaps.

            - **Error** for one example: ŷ − y. Predicting 130 when the truth is 136 gives −6.
            - **Absolute error:** the size of the gap, ignoring direction: 6.
            - **Mean absolute error:** the average of those sizes across the dataset. An MAE of 6 means "off by 6 damage on average."

            Lower is better, and 0 means perfect. The next module introduces other error measures (called **loss functions**) and why some are easier to learn with.

            ## Learning as search
            The crudest possible learning algorithm: try a lot of parameter values and keep the best one. Suppose the weights are already good and you only need the bias. Try b = 0, 5, 10, 15, 20; compute the MAE for each; keep the b with the lowest MAE. That's genuinely learning from data: the data picked the parameter.

            It doesn't scale, though. With 3 parameters and 100 candidate values each, there are 100 × 100 × 100 = a million combinations. With 7 billion parameters, there's no way to search blindly. The math module builds the tool that solves this, the **gradient**, which tells you which direction to turn every knob at once. Gradient descent, the learning method behind every LLM, is just "follow the gradient downhill."

            ## Walk through one problem
            Weights: 12 per level and 30 per tier. Three units, as (level, tier, true damage): (5, 2, 136), (1, 1, 50), (3, 3, 135). Which bias is better, 5 or 10?

            1. Predictions without the bias: 12 × 5 + 30 × 2 = 120; 12 × 1 + 30 × 1 = 42; 12 × 3 + 30 × 3 = 126.
            2. With b = 5: predictions 125, 47, 131. Gaps from truth: 11, 3, 4. MAE = 18 / 3 = 6.
            3. With b = 10: predictions 130, 52, 136. Gaps: 6, 2, 1. MAE = 9 / 3 = 3.
            4. b = 10 has the lower error, so it's the better choice of the two.

            ## Explain it to your team
            "A model is a function with knobs called parameters. The simplest one multiplies each input by a weight, adds them up and adds a bias, and every neural network is built from that piece. Training means finding knob settings that make predictions close to the right answers, measured by an error score. Blind search works for three knobs; for billions we need gradients, which come next."
            """,
            """
            ŷ = w₁·x₁ + w₂·x₂ + b
            weights w = [12, 30] (damage per level, per tier), bias b = 10
            unit (level 5, tier 2):  12×5 + 30×2 + 10 = 130   target y = 136   error = −6

            mean absolute error over a dataset = average of |ŷ − y|
            learning as search: try several b values, keep the one with the lowest error
            """,
            LlmCode(
                "Predict, score, and search",
                "Build a tiny linear model and the crudest learning algorithm: trying candidate biases and keeping the best. Plain Python, no printing.",
                [
                    "`predict(weights, bias, features)` returns the sum of each weight times its feature, plus the bias; raise `ValueError` if the lists differ in length",
                    "`predict_all(weights, bias, rows)` returns one prediction per row",
                    "`mean_absolute_error(predictions, targets)` returns the average absolute gap (raise `ValueError` for empty or mismatched lists)",
                    "`best_bias(weights, rows, targets, candidates)` returns the candidate bias with the lowest mean absolute error, keeping the earliest candidate on a tie",
                ],
                """
                def predict(weights, bias, features):
                    ...


                def predict_all(weights, bias, rows):
                    ...


                def mean_absolute_error(predictions, targets):
                    ...


                def best_bias(weights, rows, targets, candidates):
                    ...
                """,
                "`predict` is `sum(w * x for w, x in zip(weights, features)) + bias`, after checking lengths. In `best_bias`, track the best error so far and replace it only when a candidate is strictly better.",
                ["weighted sum plus bias", "predictions for many rows", "mean absolute error", "search picks the best bias"])
        ),
    ];
}
