static partial class Curriculum
{
    // Module 3 of the AI & LLM course: loss functions, gradient descent, generalization and
    // classification. Every learning algorithm in the rest of the course is built from these.
    private static LlmLessonSpec[] LlmLearningSpecs() =>
    [
        new(
            "llm-loss-functions",
            "Learning from data",
            "Loss: one number that says how wrong you are",
            "Squared error for numbers, cross-entropy for probabilities, and why cross-entropy is the loss that trains every LLM.",
            "A loss function turns a model's mistakes on a dataset into a single number to minimize. Mean squared error averages the squared gaps and punishes big misses hardest. Cross-entropy is the negative log of the probability the model gave to the right answer: near 0 when the model was confident and right, huge when it was confident and wrong. LLMs are trained to minimize the average cross-entropy of the next token.",
            [
                ("llm-models-are-functions", "you met mean absolute error there; this lesson adds the losses used in practice."),
                ("llm-exp-log-softmax", "cross-entropy is built from log-probabilities and log-softmax."),
            ],
            """
            ## The idea in plain words
            Training needs a target to aim at. A **loss function** takes the model's predictions on a set of examples and boils every mistake down to **one number**: the **loss**. Lower is better. Training is nothing more than turning the parameter knobs to make that number go down.

            Choosing the loss is choosing what "wrong" means, and different jobs need different definitions.

            ## Where you'll see it
            - **Predicting a number** (a price, a temperature, damage dealt): the gap between prediction and truth.
            - **Picking a category** (spam or not, which of 10 digits, which of 100,000 tokens comes next): how much probability the model gave the right answer.
            - **LLMs:** the loss during pretraining is the average cross-entropy of every next token in the training text. When a lab reports a training loss of 2.1, that's the number.

            ## Mean squared error: for numbers
            You've seen the **mean absolute error**: average the size of each gap. **Mean squared error** (MSE) squares each gap before averaging. Squaring does two useful things:

            1. **It punishes big misses much more than small ones.** A gap of 2 costs 4, but a gap of 10 costs 100. A model that's usually close but occasionally wildly off gets a bad score, which is usually what you want.
            2. **It has a smooth slope everywhere.** The absolute value has a sharp corner at 0, where the slope jumps from −1 to +1. The square curves smoothly, and its slope gets gentler as the gap shrinks, so gradient descent naturally takes smaller steps as it closes in. That makes it easier to optimize.

            The tradeoff: because big misses dominate, a few bad data points (outliers) can drag a squared-error model toward themselves.

            ## Cross-entropy: for probabilities
            When a model outputs a probability distribution, as softmax does, the question is: **how much probability did it put on the right answer?** Cross-entropy answers with **the negative log of that probability**:

            > loss for one example = −log(probability the model gave the correct answer)

            Walk through what that does:

            - Correct answer given probability **1.0**: −log(1) = **0**. Perfect.
            - Given **0.5**: −log(0.5) ≈ **0.69**.
            - Given **0.1**: −log(0.1) ≈ **2.30**.
            - Given **0.001**: −log(0.001) ≈ **6.91**. Confident and wrong is punished hard.
            - Given **0**: −log(0) is infinite. The model said "impossible" and it happened.

            Why the log? Three reasons. It turns "multiply the probabilities of every example" into "add up their logs", which avoids underflow (the softmax lesson). It makes the loss grow without limit as the model gets more confidently wrong, which pushes hard against overconfidence. And, as the classification lesson shows, its slope with respect to the model's scores comes out beautifully simple.

            Notice that cross-entropy only looks at the probability of the **correct** answer. The other probabilities still matter, though: softmax probabilities add up to 1, so any probability wasted on wrong answers is probability the right answer didn't get.

            ## Cross-entropy from scores, stably
            Models output scores (logits), not probabilities. You could run softmax, pick out the correct probability and take its log, but that rounds tiny probabilities to 0 and gives infinity. Instead, use log-softmax directly: the loss is **minus the log-softmax value at the correct index**. That's exactly what PyTorch's `cross_entropy` does internally, and what you'll implement here.

            ## Averaging over a dataset
            The loss for a whole dataset (or a batch) is the **average** of the per-example losses, so the number doesn't grow just because you used more examples. For an LLM, every token position in a document is one example: "given everything before, how much probability went to the token that actually came next?"

            ## What the symbols stand for
            - **ŷᵢ, yᵢ**: the prediction and the target for example i. In regression they're numbers; in classification, yᵢ is the index of the correct class.
            - **N**: the number of examples in the dataset or batch, a **constant** for one measurement.
            - **MSE** = (1/N) Σ (ŷᵢ − yᵢ)²: square each gap, add them up, divide by N.
            - **pᵢ**: the probability the model gave to example i's correct answer, between 0 and 1.
            - **Cross-entropy** = (1/N) Σ −log(pᵢ).
            - **zᵢ**: the logits for one example; the loss is −log-softmax(z)[correct index].

            ## Walk through one problem
            A three-way classifier sees two examples.

            1. Example A: probabilities [0.7, 0.2, 0.1], correct class 0. Loss = −log(0.7) ≈ 0.357.
            2. Example B: probabilities [0.6, 0.3, 0.1], correct class 2. Loss = −log(0.1) ≈ 2.303. The model was fairly sure of the wrong class.
            3. Average: (0.357 + 2.303) / 2 ≈ 1.330. Almost all of it comes from example B, which is the mistake training should fix first.

            ## Explain it to your team
            "Training needs one number that says how wrong the model is: the loss. For numeric predictions we use mean squared error, which punishes big misses hardest. For choosing among options, including choosing the next token, we use cross-entropy, the negative log of the probability the model gave the right answer. An LLM's training loss is just that, averaged over every token in the training data."
            """,
            """
            MSE:  predictions [3, 5, 9], targets [2, 5, 13]
                  gaps 1, 0, −4 → squares 1, 0, 16 → mean 17/3 ≈ 5.67

            cross-entropy for one example = −log(p_correct)
                  p = 1.0 → 0      p = 0.5 → 0.69      p = 0.1 → 2.30      p = 0.001 → 6.91

            from logits: loss = −log_softmax(logits)[correct]   (stable, never log(0))
            """,
            LlmCode(
                "Measure wrongness",
                "Implement the two losses that train almost every model. Plain Python and `math`; compute cross-entropy from logits without ever taking the log of a rounded-to-zero probability.",
                [
                    "`mean_squared_error(predictions, targets)` averages the squared gaps; raise `ValueError` for empty or mismatched lists",
                    "`cross_entropy(probabilities, correct)` returns −log(probabilities[correct]), and `math.inf` when that probability is 0",
                    "`cross_entropy_from_logits(logits, correct)` returns −(log-softmax of the logits)[correct], computed stably",
                    "`average_cross_entropy(batch_logits, targets)` returns the mean of `cross_entropy_from_logits` over a batch of examples",
                ],
                """
                import math


                def mean_squared_error(predictions, targets):
                    ...


                def cross_entropy(probabilities, correct):
                    ...


                def cross_entropy_from_logits(logits, correct):
                    ...


                def average_cross_entropy(batch_logits, targets):
                    ...
                """,
                "For the logits version, compute `m = max(logits)` and `log_sum_exp = m + math.log(sum(math.exp(z - m) for z in logits))`. The loss is `log_sum_exp - logits[correct]`.",
                ["mean squared error", "cross-entropy from probabilities", "stable cross-entropy from logits", "batch average"])
        ),
        new(
            "llm-gradient-descent",
            "Learning from data",
            "Gradient descent: walking downhill",
            "Fit a line to data the way every neural network learns: measure the slope of the loss for each knob, step the other way, repeat.",
            "Gradient descent repeatedly computes the gradient of the loss with respect to every parameter and moves each parameter a small step in the opposite direction. The learning rate sets the step size: too small and learning crawls, too large and the loss bounces or explodes. For a line fitted with squared error, the chain rule gives the gradient directly: the average of 2 × error × input for the weight, and of 2 × error for the bias.",
            [
                ("llm-derivatives-chain-rule", "the gradient and the chain rule tell us which way is downhill."),
                ("llm-loss-functions", "mean squared error is the loss we'll walk down."),
            ],
            """
            ## The idea in plain words
            Imagine you're on a foggy hillside and want to reach the valley floor. You can't see the valley, but you can feel which way the ground slopes under your feet. So you take a step downhill, feel the slope again, take another step, and repeat. Eventually you reach the bottom.

            That's **gradient descent**, and it's how every neural network learns, from a two-parameter line to a trillion-parameter LLM.

            - The **landscape** is the loss: for every possible setting of the parameters, there's a loss value.
            - **Where you stand** is the current parameter values.
            - **The slope underfoot** is the gradient, the list of partial derivatives of the loss with respect to each parameter. It points uphill.
            - **A step** moves every parameter a little in the direction *opposite* to its gradient.

            ## The update rule, in words
            For every parameter: **new value = old value − learning rate × that parameter's gradient.**

            - If the gradient is positive (the loss goes up as the parameter goes up), the parameter goes down.
            - If the gradient is negative, the parameter goes up.
            - If the gradient is near zero, the parameter barely moves; you're near a flat spot, hopefully the bottom.

            The **learning rate** is a hyperparameter you choose. It scales every step.

            ## Fitting a line
            Data: a few (x, y) points, say hours of practice and match rating. The model is a line, **ŷ = w·x + b**, with two parameters. The loss is mean squared error.

            To step downhill we need the gradient: how the loss changes with w and with b. Use the chain rule. For one example, the loss is (ŷ − y)², a chain of two steps:

            1. The **error** e = ŷ − y = w·x + b − y. Nudging w changes e at rate **x** (each unit of w adds x to the prediction). Nudging b changes e at rate **1**.
            2. The loss is e². Its rate with respect to e is **2e**.

            Multiply along the chain: the rate of the loss with respect to w is **2e·x**, and with respect to b is **2e**. Average those over all examples, because MSE is an average, and you have the gradient.

            In words: **the weight moves in proportion to the error times the input; the bias moves in proportion to the error.** Examples with big errors pull hardest. Examples with big inputs pull hardest on the weight, because the weight's effect is scaled by the input.

            ## Choosing the learning rate
            This is the most important hyperparameter, and you'll set it for every model you train.

            - **Too small:** each step is tiny, so training takes forever. The loss falls, but slowly.
            - **Just right:** the loss falls quickly and settles.
            - **Too large:** each step overshoots the valley floor and lands higher up the other side. The loss bounces, and if it's large enough, grows without limit: it **diverges**, and you'll see `inf` or `nan` in your logs.

            There's no universal right value. It depends on the scale of the data (another reason to standardize features) and the model. In practice people try a few values spread across powers of ten (0.1, 0.01, 0.001) and watch the loss curve. Large models use schedules that change the learning rate during training, covered in the training lesson.

            ## Starting point and stopping
            You need somewhere to start: for a line, w = 0 and b = 0 works. (For neural networks, starting everything at zero causes a problem the training lesson explains.) Then repeat for a fixed number of steps, or until the loss stops improving.

            For a line with squared error, the loss landscape is a single smooth bowl, so gradient descent with a sensible learning rate always finds the one best answer. Neural network landscapes are far bumpier, with many valleys and flat stretches, and yet gradient descent still works remarkably well on them. Exactly why is an active research topic.

            ## What the symbols stand for
            - **w, b**: the parameters, slope and intercept. **Variables** that change every step.
            - **xᵢ, yᵢ**: input and target for example i, **constants** from the data.
            - **eᵢ** = w·xᵢ + b − yᵢ: the error on example i.
            - **∂L/∂w** = (1/N) Σ 2·eᵢ·xᵢ and **∂L/∂b** = (1/N) Σ 2·eᵢ: the gradient of the MSE.
            - **η** ("eta"): the learning rate, a **hyperparameter** such as 0.05.
            - Update: **w ← w − η·∂L/∂w**, **b ← b − η·∂L/∂b**. The arrow means "replace with."

            ## Walk through one step
            Data: (1, 3) and (2, 5). That's the line y = 2x + 1, though the model doesn't know it. Start at w = 0, b = 0 with learning rate 0.1.

            1. Predictions: 0 and 0. Errors: 0 − 3 = −3 and 0 − 5 = −5.
            2. Weight gradient: average of 2·(−3)·1 and 2·(−5)·2, which is (−6 − 20) / 2 = −13. Bias gradient: average of 2·(−3) and 2·(−5), which is −8.
            3. Update: w = 0 − 0.1 × (−13) = 1.3, and b = 0 − 0.1 × (−8) = 0.8. Both gradients were negative, so both parameters went up.
            4. New predictions: 2.1 and 3.4. The loss fell from (9 + 25) / 2 = 17 to (0.81 + 2.56) / 2 ≈ 1.69. A few hundred more steps and w ≈ 2, b ≈ 1.

            ## Explain it to your team
            "Every model learns by gradient descent. We measure the loss, compute the gradient (which direction each parameter would have to move to make the loss go up), and move every parameter a small step the other way. The step size is the learning rate: too small and training crawls, too big and it blows up. An LLM does exactly this, just with billions of parameters and a much bumpier landscape."
            """,
            """
            model ŷ = w·x + b, loss MSE, learning rate η = 0.1, start w = 0, b = 0
            data (1, 3), (2, 5)

            errors      e = ŷ − y = [−3, −5]
            ∂L/∂w = mean(2·e·x) = (−6 − 20) / 2 = −13
            ∂L/∂b = mean(2·e)   = (−6 − 10) / 2 = −8
            w ← 0 − 0.1 × (−13) = 1.3      b ← 0 − 0.1 × (−8) = 0.8      loss 17 → 1.69
            """,
            LlmCode(
                "Fit a line by gradient descent",
                "Train a one-feature linear model with mean squared error, using gradients you derive yourself. Plain Python only.",
                [
                    "`mse_gradients(w, b, xs, ys)` returns `(dw, db)`: the averages of 2·error·x and 2·error, where error = w·x + b − y",
                    "`step(w, b, dw, db, learning_rate)` returns the updated `(w, b)` after one gradient-descent step",
                    "`fit_line(xs, ys, learning_rate, steps)` starts at w = 0, b = 0, takes `steps` steps, and returns `(w, b, losses)` where `losses` holds the MSE before each step",
                    "Your gradients must agree with a numerical check, and a good learning rate must drive the loss down",
                ],
                """
                def mse_gradients(w, b, xs, ys):
                    ...


                def step(w, b, dw, db, learning_rate):
                    ...


                def fit_line(xs, ys, learning_rate, steps):
                    ...
                """,
                "Compute `errors = [w * x + b - y for x, y in zip(xs, ys)]` once, then average `2 * e * x` and `2 * e`. In `fit_line`, record the loss, compute the gradients, and step, in that order, each time around the loop.",
                ["gradients match a numerical check", "a single step moves downhill", "the fitted line recovers the true slope and intercept", "an oversized learning rate diverges"])
        ),
        new(
            "llm-generalization",
            "Learning from data",
            "Generalization: doing well on data you haven't seen",
            "Training, validation and test sets, overfitting and underfitting, baselines, and the leaks that make a model look better than it is.",
            "A model is only useful if it works on new data, so you judge it on examples it never trained on. Split the data once, deterministically, into training, validation and test sets; tune on validation and look at test only at the end. A model that is great on training data and poor on held-out data has overfit. Leaks, such as duplicates across splits or statistics computed on the test set, make scores look better than reality.",
            [
                ("llm-gradient-descent", "you can now train a model; this lesson is about trusting what it learned."),
                ("llm-data-as-numbers", "standardization statistics are a classic source of leaks."),
            ],
            """
            ## The idea in plain words
            A student who memorizes last year's exam answers gets 100% on last year's exam and fails this year's. They **memorized** instead of **learning**. Models do the same thing, and with millions of parameters they're very good at memorizing.

            What we actually want is **generalization**: good performance on examples the model has never seen. The only honest way to measure it is to hold some data back, never train on it, and test on it.

            ## Three splits
            Split the data **once**, before you start:

            - **Training set** (often 80%): the model learns from this.
            - **Validation set** (often 10%): you check progress here and use it to make choices like the learning rate, model size or when to stop. It's held out from *training*, but your decisions are tuned to it, so it slowly stops being "unseen."
            - **Test set** (often 10%): looked at **once**, at the end, to report how good the final model is. If you go back and tweak things after seeing the test score, it becomes another validation set, and your reported number gets optimistic.

            The split should be **deterministic**: the same example should land in the same split every time you run the code, even if the dataset is reordered or grows. A good way is to hash each example's ID into a number from 0 to 1 and assign by range. Splitting with an unseeded shuffle sends examples to different splits on every run, which silently leaks test examples into training over time.

            ## Underfitting and overfitting
            Watch the training loss and the validation loss together:

            - **Underfitting:** both are high. The model is too simple, or hasn't trained long enough. A straight line can't fit a curve.
            - **Good fit:** both are low and close together.
            - **Overfitting:** training loss keeps falling while validation loss stops falling and starts rising. The model has started memorizing the training examples, noise included.

            Remedies for overfitting: more data, a smaller model, stopping early when validation loss bottoms out, and **regularization** (techniques that discourage memorization, such as dropout or weight decay, which shows up again in the training lesson).

            LLMs are an interesting case. They're trained on so much text that most training examples are seen only once or a few times, so classic overfitting is less of a problem than you'd expect. But they still memorize text that appears many times, such as famous quotes, licenses and boilerplate code, which matters for privacy and copyright.

            ## Baselines: compared to what?
            "92% accuracy" means nothing on its own. If 92% of emails aren't spam, a model that always answers "not spam" also scores 92%. Always compare against a **baseline**: the dumbest reasonable approach, such as always predicting the most common class or the average value. A model that can't beat the baseline hasn't learned anything useful, however impressive its number looks.

            ## Leaks: how scores lie
            A **leak** is any way information from held-out data gets into training or tuning. Common ones:

            - **Duplicates across splits.** The same example, or a near-copy with different capitalization or spacing, appears in both training and test. The model has literally seen the answer. This is the main worry with LLM benchmarks: if test questions were posted online, they may be in the pretraining data. It's called **contamination**, and the evaluation lesson covers it.
            - **Statistics from all the data.** Standardizing features with a mean and spread computed over *all* examples lets the test set influence training. Compute them on the training split, then apply those same numbers to the others.
            - **Time travel.** Predicting tomorrow's stock price with features that were only known after tomorrow. When data has a time order, split by time.
            - **Peeking.** Choosing hyperparameters by looking at the test score.

            ## What the words mean
            - **Generalization:** performing well on data the model didn't train on.
            - **Training / validation / test set:** data used to learn, to tune, and to report, respectively.
            - **Overfitting / underfitting:** too closely fitted to training data / too simple to fit it at all.
            - **Baseline:** the simplest reasonable approach, used as a point of comparison.
            - **Leak / contamination:** held-out information reaching training or tuning.
            - **Accuracy:** the fraction of predictions that are exactly right.

            ## Walk through one problem
            You have 1,000 support tickets labeled "billing" (700) or "technical" (300).

            1. Split by hashing each ticket ID: about 800 for training, 100 for validation, 100 for test.
            2. Baseline: always answer "billing", the most common label in training. That scores about 70% on test.
            3. Your model scores 74% on test. It beats the baseline, but by far less than "74%" suggests on its own.
            4. You check for leaks and find 30 test tickets that are copy-pasted duplicates of training tickets. Your model gets all 30 right. On the 70 genuinely new ones it gets 44 right, about 63%, which is *worse* than the baseline. The model memorized; it didn't learn. Deduplicate before you split.

            ## Explain it to your team
            "A model's score only means something on data it never trained on, so we split data once, deterministically, and keep a test set we look at only at the end. We always compare against a dumb baseline. And we hunt for leaks, especially duplicates between training and test, because for LLMs, benchmark questions that ended up in the training data make scores look far better than they are."
            """,
            """
            split by hash:   bucket = crc32(example_id) / 2³²     → [0, 0.8) train · [0.8, 0.9) validation · [0.9, 1) test
            leak check:      normalize text (lowercase, collapse spaces), look for any text in both train and test
            standardize:     mean, spread from TRAIN only → apply the same numbers to validation and test
            baseline:        always predict the most common training label
            """,
            LlmCode(
                "Split, check for leaks, and compare to a baseline",
                "Write the bookkeeping that keeps evaluation honest. Use `zlib.crc32` for hashing (Python's built-in `hash` changes between runs). Standard library only.",
                [
                    "`assign_split(example_id, validation_fraction, test_fraction)` hashes the id with `zlib.crc32(example_id.encode())`, divides by 2**32 to get a bucket in [0, 1), and returns `'test'` for buckets below `test_fraction`, `'validation'` below `test_fraction + validation_fraction`, otherwise `'train'`",
                    "`find_leaks(train_texts, test_texts)` returns the sorted list of normalized test texts that also appear in training, where normalizing means lowercasing and collapsing runs of whitespace to single spaces after stripping the ends",
                    "`fit_standardizer(train_values)` returns `(mean, spread)` from the training values only (population spread); `apply_standardizer(values, mean, spread)` returns the z-scores using those numbers",
                    "`majority_baseline_accuracy(train_labels, test_labels)` returns the accuracy on the test labels of always predicting the most common training label (earliest-seen wins a tie)",
                ],
                """
                import math
                import zlib


                def assign_split(example_id, validation_fraction, test_fraction):
                    ...


                def find_leaks(train_texts, test_texts):
                    ...


                def fit_standardizer(train_values):
                    ...


                def apply_standardizer(values, mean, spread):
                    ...


                def majority_baseline_accuracy(train_labels, test_labels):
                    ...
                """,
                "Normalize with `' '.join(text.lower().split())`, which lowercases and collapses all whitespace. For the baseline, count training labels in a dict in order of first appearance and pick the largest count with `max`, which keeps the first of any tie.",
                ["deterministic hash split", "leak detection after normalizing", "standardizer fitted on training data only", "majority baseline"])
        ),
        new(
            "llm-classification",
            "Learning from data",
            "Classification: logistic regression, thresholds and errors",
            "Turn a score into a probability with the sigmoid, train it with cross-entropy, and judge it with precision and recall rather than accuracy alone.",
            "Logistic regression computes a weighted score and squashes it with the sigmoid into a probability between 0 and 1. Trained with cross-entropy, its gradient is remarkably simple: (predicted probability − true label) × input. A threshold turns probabilities into yes/no decisions, and a confusion matrix of true and false positives and negatives shows which mistakes it makes; precision and recall summarize them.",
            [
                ("llm-loss-functions", "logistic regression is trained with cross-entropy."),
                ("llm-gradient-descent", "it learns by the same gradient steps."),
                ("llm-generalization", "you'll judge it on held-out data against a baseline."),
            ],
            """
            ## The idea in plain words
            Many questions have yes-or-no answers. Is this email spam? Will this player quit this week? Is this code change risky? A **classifier** answers them. The best ones don't just say yes or no; they give a **probability**, which you can then turn into a decision.

            **Logistic regression** is the simplest good classifier, and it's worth knowing well: it's a linear model plus a squashing function, and each output neuron of a neural network, and each next-token choice of an LLM, is a bigger version of the same idea.

            ## From score to probability: the sigmoid
            Start with the linear model: a weighted sum of the features plus a bias. Call it the **score** z. It can be any number, negative or positive. To turn it into a probability, pass it through the **sigmoid** function:

            > sigmoid(z) = 1 / (1 + exp(−z))

            In words: a very large positive score gives a probability near 1, a very negative score gives a probability near 0, and a score of exactly 0 gives 0.5, a coin flip. It's an S-shaped curve. The sigmoid is softmax for two options: if "yes" has score z and "no" has score 0, softmax gives "yes" a probability of exp(z) / (exp(z) + 1), which is exactly sigmoid(z).

            The same overflow trap applies. exp(1000) is too big for a float, so for very negative z compute it as exp(z) / (1 + exp(z)) instead, which never exponentiates a large positive number.

            ## Training: cross-entropy and a beautiful gradient
            For a yes/no label y (1 for yes, 0 for no) and predicted probability p, the cross-entropy loss is −log(p) when the answer is yes, and −log(1 − p) when it's no. This is often written as one formula, the **binary cross-entropy**: −[y·log(p) + (1 − y)·log(1 − p)].

            Now the chain rule. The loss depends on p, p depends on the score z through the sigmoid, and z depends on the weights. Work through the rates and something remarkable happens: the messy pieces cancel, and the rate of the loss with respect to the score is just

            > p − y

            In words: **predicted probability minus the true answer.** If the answer was yes (y = 1) and the model said 0.9, the score's gradient is −0.1: push the score up a little. If the model said 0.2, the gradient is −0.8: push it up a lot. One more link in the chain gives the gradient for each weight: **(p − y) × that feature**, averaged over the examples. The bias gets the average of (p − y).

            That's the same shape as the linear-regression gradient (error times input), which is no coincidence. The same simplification happens for softmax with cross-entropy, with many classes: the gradient on each logit is (predicted probability − 1 if it's the correct class, else 0). That's the gradient at the very top of every LLM during training.

            ## Thresholds: from probability to decision
            To act, you need a yes/no. The default is "yes if p ≥ 0.5," but the right **threshold** depends on what mistakes cost. A cancer screen should flag at a low probability, because missing a case is far worse than an extra test. A spam filter should only junk mail at a high probability, because losing a real email is worse than seeing some spam.

            ## Counting the mistakes
            Compare decisions with the truth, and every prediction lands in one of four boxes, called the **confusion matrix**:

            - **True positive (TP):** said yes, was yes.
            - **False positive (FP):** said yes, was no. A false alarm.
            - **False negative (FN):** said no, was yes. A miss.
            - **True negative (TN):** said no, was no.

            Two summaries matter more than accuracy, especially when "yes" is rare:

            - **Precision** = TP / (TP + FP): of everything flagged, how much was right? Low precision means lots of false alarms.
            - **Recall** = TP / (TP + FN): of everything that was really yes, how much did we catch? Low recall means lots of misses.

            Raising the threshold usually raises precision and lowers recall; lowering it does the opposite. If nothing is flagged, precision is undefined (zero divided by zero); report it as 0 and say why.

            ## What the symbols stand for
            - **x**: the feature vector of one example; **w**: the weights; **b**: the bias.
            - **z** = w · x + b: the score. **p** = sigmoid(z): the predicted probability of "yes."
            - **y**: the true label, 1 for yes and 0 for no.
            - **∂L/∂z** = p − y; **∂L/∂wⱼ** = average of (p − y)·xⱼ; **∂L/∂b** = average of (p − y).
            - **t**: the decision threshold, a **constant** you choose, such as 0.5.
            - **TP, FP, FN, TN**: counts of each kind of outcome.

            ## Walk through one problem
            A moderation model reviews 100 posts, 10 of which really break the rules. It flags 12. Of those 12, 8 really break the rules.

            1. TP = 8 (flagged and bad). FP = 12 − 8 = 4 (flagged but fine). FN = 10 − 8 = 2 (bad but missed). TN = 100 − 8 − 4 − 2 = 86.
            2. Accuracy = (8 + 86) / 100 = 94%. But "flag nothing" would score 90%, so accuracy flatters it.
            3. Precision = 8 / 12 ≈ 0.67: a third of the flags are false alarms.
            4. Recall = 8 / 10 = 0.8: it catches 80% of the bad posts.

            ## Explain it to your team
            "A classifier computes a score, and the sigmoid (or softmax, with more than two options) turns the score into a probability. Trained with cross-entropy, the gradient on the score is simply predicted probability minus the truth, which is the same update at the top of every LLM. To judge a classifier, look at precision and recall, not just accuracy, and set the decision threshold based on which mistakes cost more."
            """,
            """
            z = w · x + b           p = sigmoid(z) = 1 / (1 + exp(−z))
            sigmoid(0) = 0.5    sigmoid(4) ≈ 0.982    sigmoid(−4) ≈ 0.018
            loss = −[y·log p + (1 − y)·log(1 − p)]       ∂L/∂z = p − y
            ∂L/∂wⱼ = mean((p − y)·xⱼ)       ∂L/∂b = mean(p − y)

            precision = TP / (TP + FP)       recall = TP / (TP + FN)
            """,
            LlmCode(
                "Train and judge a classifier",
                "Build logistic regression from scratch: a stable sigmoid, a training loop driven by the (p − y) gradient, and the mistake counts used to judge it. Plain Python and `math`.",
                [
                    "`sigmoid(z)` returns 1 / (1 + exp(−z)) without overflowing for any z (use exp(z) / (1 + exp(z)) when z is negative)",
                    "`train_logistic(rows, labels, learning_rate, steps)` starts with zero weights and bias, takes `steps` full-batch gradient steps using the averaged (p − y)·x and (p − y), and returns `(weights, bias)`",
                    "`confusion_counts(predicted, actual)` returns a dict with keys `'tp'`, `'fp'`, `'fn'`, `'tn'` for lists of 0/1 labels",
                    "`precision_recall(counts)` returns `(precision, recall)`, using 0.0 for either one whose denominator is 0",
                ],
                """
                import math


                def sigmoid(z):
                    ...


                def train_logistic(rows, labels, learning_rate, steps):
                    ...


                def confusion_counts(predicted, actual):
                    ...


                def precision_recall(counts):
                    ...
                """,
                "Inside each training step, first compute every example's p, then the averaged gradients, then update all weights at once. Updating weights one example at a time inside the averaging loop mixes old and new values.",
                ["stable sigmoid", "training separates the classes", "confusion counts", "precision and recall with empty cases"])
        ),
    ];
}
