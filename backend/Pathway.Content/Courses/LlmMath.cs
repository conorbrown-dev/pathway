namespace Pathway.Content;

static partial class Curriculum
{
    // Module 2 of the AI & LLM course: the math every later lesson uses, each idea explained in
    // words before symbols and tied to a game or everyday situation and to where it shows up in an LLM.
    private static LlmLessonSpec[] LlmMathSpecs() =>
    [
        new(
            "llm-vectors-dot-product",
            "Math foundations, in words",
            "Vectors and the dot product",
            "A vector is a list of numbers that can be read as an arrow, and the dot product measures how much two arrows point the same way.",
            "A vector is an ordered list of numbers, which you can picture as an arrow with a direction and a length. The dot product multiplies matching entries and adds them up; it's large when two vectors point the same way, zero when they're at right angles, and negative when they point apart. Divided by both lengths, it gives cosine similarity, the standard measure of how alike two embeddings are.",
            [
                ("llm-models-are-functions", "the weighted sum in a linear model is the dot product, now with a name."),
                ("llm-data-as-numbers", "feature vectors are the vectors this lesson works with."),
            ],
            """
            ## The idea in plain words
            A **vector** is just an ordered list of numbers, like `[3, 4]` or a unit's `[health, speed, range]`. You've been using them since the feature-vector lesson. What this lesson adds is a picture: a vector with two numbers is an **arrow** on a map, going 3 steps east and 4 steps north from where you stand. Arrows have a **direction** (which way they point) and a **length** (how far they go). Vectors with 768 or 4,096 numbers, the size LLMs use, can't be drawn, but every rule you learn with 2-number arrows works the same way with any count.

            ## Where you'll see it
            - **Games:** a unit's velocity is a vector: `[2, -1]` means "2 per second east, 1 per second south." Adding the wind's push `[0.5, 0]` gives the new velocity `[2.5, -1]`.
            - **Recommendations:** a streaming service describes you as a "taste vector," with numbers for how much you like action, comedy and documentaries. It describes each film the same way, then recommends films whose vectors point the same way as yours.
            - **Inside an LLM:** every token becomes a vector (its embedding), and attention decides which tokens to focus on by taking dot products between vectors. This lesson is the foundation of both.

            ## Adding and scaling
            - **Add** two vectors of the same length entry by entry: `[1, 2] + [3, 5] = [4, 7]`. As arrows: walk the first one, then the second from where you ended.
            - **Scale** a vector by multiplying every entry by one number: `3 × [1, 2] = [3, 6]`. Same direction, three times as long. Multiplying by −1 flips it around.

            ## Length
            The length of `[3, 4]` comes from the Pythagorean theorem: the arrow is the long side of a right triangle with sides 3 and 4, so its length is √(3² + 4²) = √25 = 5. For any vector, **square every entry, add them up, take the square root**. This is also called the **norm**.

            ## The dot product
            Multiply matching entries and add the results:

            `[1, 2, 3] · [4, 5, 6] = 1×4 + 2×5 + 3×6 = 4 + 10 + 18 = 32`

            You already know this operation: it's the weighted sum from the linear-model lesson, where one vector held weights and the other held features.

            What it measures, in words: **how much two arrows point the same way, scaled by how long they are.**

            - Pointing the same way: large and positive. `[2, 0] · [3, 0] = 6`.
            - At right angles: exactly zero. `[2, 0] · [0, 3] = 0`. Neither has any "amount" in the other's direction.
            - Pointing opposite ways: negative. `[2, 0] · [-3, 0] = -6`.

            Game example: a turret faces direction `f = [1, 0]` (east). An enemy sits in direction `e = [0.6, 0.8]` from it. f · e = 0.6, which is positive, so the enemy is somewhere in front of the turret. A negative result would mean it's behind. Games run this check constantly because it's so cheap.

            ## Cosine similarity: direction only
            The dot product mixes direction with length: a very long vector gets big dot products with everything. To compare **direction only**, divide by both lengths:

            > cosine similarity = (a · b) / (length of a × length of b)

            The result always falls between −1 and 1. A value of 1 means exactly the same direction, 0 means unrelated (at right angles), and −1 means exactly opposite. It's called "cosine" because it equals the cosine of the angle between the arrows, which you may know from trigonometry, but you don't need trigonometry to use it. Search engines and retrieval systems rank documents by the cosine similarity of their embedding vectors, as you'll do in the retrieval lesson.

            A zero vector `[0, 0]` has no direction, so cosine similarity with it is undefined (it would divide by zero). Code should refuse, not return a made-up number.

            ## What the symbols stand for
            - **a, b**: two vectors of the same length n, the **variables** being compared. Their entries are a₁, a₂, …, aₙ.
            - **n**: how many entries each vector has, its **dimension**. A **constant** for a given model: 2 for map arrows, 4,096 for a mid-size LLM's token vectors.
            - **a · b**: the dot product, a₁b₁ + a₂b₂ + … + aₙbₙ, a single number.
            - **‖a‖**: the length (norm) of a, √(a₁² + a₂² + … + aₙ²).
            - **cos(a, b)**: cosine similarity, (a · b) / (‖a‖ × ‖b‖), between −1 and 1.

            ## Walk through one problem
            Two taste vectors over (action, comedy, documentary): you are `[4, 1, 0]` and a film is `[3, 0, 1]`.

            1. Dot product: 4×3 + 1×0 + 0×1 = 12. Positive, so the directions broadly agree.
            2. Your length: √(16 + 1 + 0) = √17 ≈ 4.123. The film's length: √(9 + 0 + 1) = √10 ≈ 3.162.
            3. Cosine similarity: 12 / (4.123 × 3.162) ≈ 12 / 13.04 ≈ 0.92. That's very close to 1, so it's a strong match.

            ## Explain it to your team
            "Models represent everything, including every token, as vectors: long lists of numbers you can think of as arrows. The dot product multiplies matching entries and adds them up, and it measures how much two arrows point the same way. Divide by their lengths and you get cosine similarity, which is how we measure whether two pieces of text mean similar things. Attention, the core of a transformer, is built out of dot products."
            """,
            """
            a = [1, 2, 3], b = [4, 5, 6]
            a + b        = [5, 7, 9]
            2 × a        = [2, 4, 6]
            a · b        = 1×4 + 2×5 + 3×6 = 32
            ‖a‖          = √(1 + 4 + 9) = √14 ≈ 3.742
            cos(a, b)    = 32 / (3.742 × 8.775) ≈ 0.975   (nearly the same direction)
            """,
            LlmCode(
                "Vector tools",
                "Write the vector operations the rest of the course uses, for vectors stored as Python lists. Use plain Python and `math` only.",
                [
                    "`add(a, b)` and `scale(k, a)` return new lists; `add` raises `ValueError` if the lengths differ",
                    "`dot(a, b)` returns the sum of matching products, and raises `ValueError` if the lengths differ",
                    "`norm(a)` returns the length √(sum of squares)",
                    "`cosine_similarity(a, b)` returns dot / (norm × norm), and raises `ValueError` if either vector has length 0 or the lengths differ",
                ],
                """
                import math


                def add(a, b):
                    ...


                def scale(k, a):
                    ...


                def dot(a, b):
                    ...


                def norm(a):
                    ...


                def cosine_similarity(a, b):
                    ...
                """,
                "Use `zip(a, b)` to pair up entries after checking `len(a) == len(b)`. `norm(a)` is `math.sqrt(dot(a, a))`, because a vector dotted with itself is the sum of its squares.",
                ["adding and scaling", "dot product", "length", "cosine similarity and its zero-vector error"])
        ),
        new(
            "llm-matrices",
            "Math foundations, in words",
            "Matrices: many weighted sums at once",
            "A matrix is a grid of numbers. Multiplying a matrix by a vector computes a whole layer of weighted sums in one step, and that is most of what a neural network does.",
            "A matrix is a grid of numbers with a shape of rows × columns. Multiplying a matrix by a vector takes the dot product of each row with the vector, so one matrix holds the weights for many linear models at once. Multiplying two matrices does that for many vectors at once, and only works when the inner dimensions match. Almost all of an LLM's computation is matrix multiplication.",
            [("llm-vectors-dot-product", "a matrix-vector product is one dot product per row.")],
            """
            ## The idea in plain words
            A **matrix** is a grid of numbers, stored in Python as a list of rows: `[[1, 2, 3], [4, 5, 6]]` is a matrix with 2 rows and 3 columns. Its **shape** is written **2 × 3**: rows first, then columns.

            Why grids? In the linear-model lesson, one set of weights turned a feature vector into one prediction. Real models want many outputs from the same input: a layer of a neural network might compute 4,096 different weighted sums of the same input vector. Stack each weight vector as a row, and you have a matrix. Multiply that matrix by the input vector and you get all 4,096 results in one operation.

            ## Where you'll see it
            - **A spreadsheet of prices:** rows are stores, columns are items (milk, bread, eggs), and your shopping list is a vector of quantities. Multiplying the price matrix by your quantity vector gives the total bill at each store in one go.
            - **A game's damage table:** rows are attacking unit types, columns are armor types. Combined with an army's composition vector, it gives total damage per armor type.
            - **Inside an LLM:** every layer is mostly a handful of matrix multiplications. The "parameters" of a 7-billion-parameter model are almost all entries of weight matrices. GPUs are chips designed to multiply matrices very fast, which is why AI runs on them.

            ## Matrix times vector
            To multiply a matrix by a vector, **take the dot product of each row with the vector**. Each row gives one number, so the result is a vector with one entry per row.

            ```
            [[1, 2, 3],      [1,          [1×1 + 2×0 + 3×2,        [7,
             [4, 5, 6]]  ×    0,    =      4×1 + 5×0 + 6×2]    =    16]
                              2]
            ```

            The vector's length must equal the number of **columns**, because each row has to pair up entry by entry with the vector. A 2 × 3 matrix accepts vectors of length 3 and produces vectors of length 2. So a matrix is a machine that turns vectors of one size into vectors of another size.

            ## Matrix times matrix
            Multiplying two matrices, A × B, is the same thing repeated: each **column** of B is treated as a vector and multiplied by A. Equivalently, the entry in row i, column j of the result is the dot product of **row i of A** with **column j of B**.

            The shape rule follows directly: if A is (m × n) and B is (n × p), the result is (m × p). The two inner numbers (both n) must match, because each dot product pairs A's rows (length n) with B's columns (length n). If they don't match, the multiplication is undefined, and in code that's a bug to catch with a clear error. "Shape mismatch" is the single most common error in machine learning code, so checking shapes is a habit worth building now.

            Order matters: A × B and B × A are generally different, and one of them may not even be defined.

            In practice, models multiply a weight matrix by many input vectors at once (a **batch**), by stacking the inputs as the rows of a matrix. That's how one GPU operation processes thousands of tokens.

            ## Transpose
            The **transpose** of a matrix flips it over its diagonal: rows become columns. The transpose of `[[1, 2, 3], [4, 5, 6]]` (2 × 3) is `[[1, 4], [2, 5], [3, 6]]` (3 × 2). It's mostly bookkeeping, used to make shapes line up. Attention, for example, multiplies one matrix by the transpose of another so every query row meets every key row.

            ## What the symbols stand for
            - **A, B**: matrices. **Aᵢⱼ** is the entry in row i, column j (counting from 1 in math notation, from 0 in Python).
            - **m, n, p**: sizes. A has shape **m × n** (m rows, n columns); B has shape **n × p**. They're **constants** for a given model: in an LLM layer, n might be 4,096 (the vector size) and m the number of tokens being processed.
            - **x**: a vector of length n, the input, a **variable**.
            - **Ax**: the matrix-vector product, a vector of length m whose i-th entry is (row i of A) · x.
            - **Aᵀ**: the transpose of A, shape n × m.

            ## Walk through one problem
            Prices (rows: Store 1, Store 2; columns: milk, bread, eggs) = `[[3, 2, 4], [2, 3, 5]]`. Shopping list: 2 milk, 1 bread, 1 dozen eggs, `[2, 1, 1]`.

            1. Check the shapes: the matrix is 2 × 3 and the vector has length 3, so the multiplication works and gives a length-2 result.
            2. Store 1: [3, 2, 4] · [2, 1, 1] = 6 + 2 + 4 = 12 dollars.
            3. Store 2: [2, 3, 5] · [2, 1, 1] = 4 + 3 + 5 = 12 dollars. A tie! Change the list to 3 milk and Store 2 wins by a dollar.

            ## Explain it to your team
            "A matrix is a grid of weights. Multiplying it by a vector takes the dot product of every row with that vector, so one matrix computes many weighted sums at once, which is exactly what a layer of a neural network does. Nearly all of an LLM's parameters live in matrices, and nearly all its computation is matrix multiplication, which is why it runs on GPUs. The rule that matters most in practice: the inner dimensions must match."
            """,
            """
            A = [[1, 2, 3],          shape 2 × 3
                 [4, 5, 6]]
            x = [1, 0, 2]            length 3 (must equal A's columns)
            A x = [1×1 + 2×0 + 3×2, 4×1 + 5×0 + 6×2] = [7, 16]     length 2

            (m × n) × (n × p) → (m × p)     the inner n's must match
            Aᵀ = [[1, 4], [2, 5], [3, 6]]   shape 3 × 2
            """,
            LlmCode(
                "Matrix tools",
                "Implement the matrix operations a neural network layer needs, with matrices as lists of rows. Plain Python only.",
                [
                    "`shape(matrix)` returns `(rows, columns)` and raises `ValueError` if the rows have different lengths",
                    "`transpose(matrix)` returns a new matrix with rows and columns swapped",
                    "`matvec(matrix, vector)` returns the list of row · vector dot products, raising `ValueError` on a shape mismatch",
                    "`matmul(a, b)` returns the matrix product (row i of a · column j of b), raising `ValueError` when a's column count differs from b's row count",
                ],
                """
                def shape(matrix):
                    ...


                def transpose(matrix):
                    ...


                def matvec(matrix, vector):
                    ...


                def matmul(a, b):
                    ...
                """,
                "Write a small `dot` helper first. `transpose` can be `[list(column) for column in zip(*matrix)]`. Then `matmul(a, b)` is `matvec(a, column)` for each column of b, transposed back, or a double loop over rows of a and columns of b.",
                ["shape, including ragged rows", "transpose", "matrix times vector", "matrix times matrix and the shape rule"])
        ),
        new(
            "llm-probability",
            "Math foundations, in words",
            "Probability: how likely is the next word?",
            "Distributions, turning counts into probabilities, conditional probability, and how to draw a random outcome with the right odds: the way an LLM picks every token it writes.",
            "A probability distribution assigns each possible outcome a number from 0 to 1, and the numbers add up to 1. Counting outcomes and dividing by the total turns data into a distribution; counting only the cases where some condition holds gives a conditional probability, such as the chance of each next word given the previous one. To sample, lay the probabilities end to end on a line from 0 to 1 and see where a uniform random number lands.",
            [("llm-python-toolkit", "you'll count with dictionaries and draw seeded random numbers.")],
            """
            ## The idea in plain words
            A **probability** is a number from 0 to 1 that says how likely something is: 0 means it never happens, 1 means it always does, 0.25 means it happens about a quarter of the time over many tries. A **probability distribution** lists every possible outcome with its probability, and those probabilities must add up to exactly 1, because *something* happens every time.

            An LLM is a machine for producing distributions. At every step it outputs one probability for every token in its vocabulary ("the" 0.31, "a" 0.12, "cat" 0.0004, …) and then picks one. Everything in this lesson is a piece of that process.

            ## Where you'll see it
            - **Dice and loot drops:** a fair die gives each face 1/6. A loot table might say common 0.70, rare 0.25, legendary 0.05, which adds up to 1.
            - **Weather:** "30% chance of rain" is a probability for one outcome; "no rain" gets the remaining 70%.
            - **Phone keyboards:** after you type "see you", the keyboard suggests "soon", "tomorrow" and "later". It's showing the top of a distribution over next words.

            ## From counts to probabilities
            The simplest way to estimate a distribution from data is to count and divide. A player opened 200 loot boxes and got 142 common, 48 rare and 10 legendary items. Estimated probabilities: 142/200 = 0.71, 48/200 = 0.24, 10/200 = 0.05. Dividing every count by the total is called **normalizing**, and it guarantees the results add up to 1.

            ## Conditional probability: "given that"
            Often the odds depend on what just happened. The chance the next word is "Francisco" is tiny in general, but large if the previous word was "San". That's a **conditional probability**, read "the probability of Francisco *given* San", and written P(Francisco | San). The vertical bar means "given."

            To estimate it from text, count only the places where the condition holds. In "the cat sat on the mat the end", the word "the" appears 3 times and is followed by "cat", "mat" and "end" once each. So P(cat | the) = 1/3, P(mat | the) = 1/3 and P(end | the) = 1/3. Each conditional distribution, one per "given" word, adds up to 1 on its own.

            This is exactly the **bigram language model** you'll build in the language module: a table of next-word distributions, one for each previous word. A transformer is a vastly better version of the same idea: a distribution over the next token given *all* the previous tokens.

            ## Sampling: drawing with the right odds
            To pick an outcome at random with the right odds, use a uniform random number **u** between 0 and 1 (Python's `random.random()` gives one). Lay the probabilities end to end along the line from 0 to 1:

            - common covers 0 up to 0.70,
            - rare covers 0.70 up to 0.95,
            - legendary covers 0.95 up to 1.

            Then see which stretch u lands in. If u = 0.83, you get rare. Each outcome's stretch is exactly as long as its probability, so over many draws each one comes up at the right rate. The running totals (0.70, 0.95, 1.00) are the **cumulative** probabilities. Every token an LLM writes is chosen this way, after some reshaping of the distribution that the sampling lesson covers.

            Because u comes from a seeded generator, the same seed gives the same draws, which makes sampling reproducible.

            ## Expected value: the long-run average
            If outcomes are numbers, the **expected value** is the average you'd get over many tries: multiply each outcome by its probability and add up. A die's expected value is (1 + 2 + 3 + 4 + 5 + 6) × 1/6 = 3.5. You'll never roll 3.5, but it's the long-run average, and it's how loss functions average error over a dataset.

            ## Independence, and why we'll need logs
            If two events don't affect each other, the probability that both happen is their product: two coin flips both landing heads is 0.5 × 0.5 = 0.25. A sentence's probability, under a language model, is the product of each token's conditional probability. Multiplying many numbers below 1 gives a very small number: 100 tokens at 0.1 each gives 10⁻¹⁰⁰, and a few hundred more gets smaller than a computer's floats can represent. The fix is logarithms, the subject of the next lesson.

            ## What the symbols stand for
            - **P(x)**: the probability of outcome x, a number from 0 to 1. P(legendary) = 0.05.
            - **P(x | c)**: the probability of x given that condition c holds. P(Francisco | San) is high.
            - **count(x)**: how many times x appeared in the data, a **constant** once the data is fixed.
            - **u**: a uniform random number in [0, 1), a fresh **variable** for each draw.
            - **E[X]**: the expected value of a numeric outcome X, Σ value × probability. (Σ, the Greek capital sigma, means "add up over all of them.")

            ## Walk through one problem
            Text: "a b a c a b". What is the distribution of the next word after "a", and which word does u = 0.7 pick?

            1. Find each "a" that has a next word: positions 1, 3 and 5, followed by "b", "c" and "b".
            2. Counts after "a": b = 2, c = 1, total 3.
            3. Normalize: P(b | a) = 2/3 ≈ 0.667, P(c | a) = 1/3 ≈ 0.333.
            4. Lay them out in order: b covers [0, 0.667), c covers [0.667, 1). u = 0.7 lands in c's stretch, so the sample is "c".

            ## Explain it to your team
            "At every step, an LLM outputs a probability for every possible next token, and those probabilities add up to one. Conceptually it's like counting which words follow which in a huge amount of text, except the model conditions on everything that came before, not just the last word. To write, it draws a token at random with those odds, which is why the same prompt can give different answers."
            """,
            """
            counts after "the": {"cat": 1, "mat": 1, "end": 1}  → P(· | the) = 1/3 each
            loot table: common 0.70 | rare 0.25 | legendary 0.05
            cumulative:  0.70        0.95        1.00
            u = 0.83 → lands in [0.70, 0.95) → rare
            expected value of a die = (1+2+3+4+5+6) × 1/6 = 3.5
            """,
            LlmCode(
                "Count, condition, and sample",
                "Build the probability tools a bigram language model needs. Plain Python only; keep outcomes in the order they appear in the input.",
                [
                    "`normalize(counts)` turns a dict of outcome → count into outcome → probability (same key order), raising `ValueError` if the total is not positive",
                    "`sample(probabilities, u)` returns the outcome whose cumulative stretch contains `u` (0 ≤ u < 1), walking the dict in its own order",
                    "`next_word_distribution(words)` returns a dict mapping each word to the normalized distribution of the words that follow it, in order of first appearance",
                    "`expected_value(probabilities)` returns the sum of value × probability for a dict of number → probability",
                ],
                """
                def normalize(counts):
                    ...


                def sample(probabilities, u):
                    ...


                def next_word_distribution(words):
                    ...


                def expected_value(probabilities):
                    ...
                """,
                "In `sample`, keep a running total; return the first outcome where `u < running_total`. Floats can leave the final total a hair under 1, so if the loop finishes without returning, return the last outcome. For `next_word_distribution`, count pairs with `zip(words, words[1:])`, then normalize each inner dict.",
                ["normalizing counts", "sampling by cumulative stretches", "conditional next-word distributions", "expected value"])
        ),
        new(
            "llm-exp-log-softmax",
            "Math foundations, in words",
            "Exponentials, logarithms, and softmax",
            "Why models turn raw scores into probabilities with exponentials, why they keep probabilities as logarithms, and how to do both without overflowing.",
            "Softmax turns any list of scores (logits) into a probability distribution: raise e to each score so everything is positive, then divide by the total. Subtracting the largest score first gives the same answer without overflow. Logarithms turn tiny products of probabilities into manageable sums, so models work with log-probabilities, and log-softmax computes them directly and stably.",
            [
                ("llm-probability", "softmax produces a probability distribution, and log-probabilities fix the underflow problem from that lesson."),
                ("llm-python-toolkit", "you'll use `math.exp` and `math.log`."),
            ],
            """
            ## The problem softmax solves
            The last layer of an LLM produces one raw score per token in its vocabulary, called a **logit**. Logits can be any number: 7.2, −3.1, 0. A higher logit means the model favors that token more, but logits aren't probabilities. They can be negative and they don't add up to 1. We need a way to turn any list of scores into a proper distribution that:

            1. makes every value positive,
            2. keeps the order (a higher score still means a higher probability), and
            3. adds up to 1.

            The tool for step 1 is the exponential function.

            ## Exponentials, briefly
            **Exponential growth** means multiplying by the same factor at every step: an economy that grows 10% per minute multiplies by 1.1 each minute. The number **e ≈ 2.718** is a special growth factor that shows up whenever growth is continuous, like interest compounded every instant. The function **exp(x) = eˣ** has three properties that make it perfect here:

            - It's **always positive**: exp(−5) ≈ 0.0067, exp(0) = 1, exp(3) ≈ 20.1. Even very negative scores become small positive numbers.
            - It **keeps order**: a bigger x always gives a bigger exp(x).
            - It **turns differences into ratios**: exp(a) / exp(b) = exp(a − b). A logit that's 1 higher means about 2.718 times as likely; 2 higher, about 7.4 times. So the model's scores are really "log-odds": differences between scores set the ratios between probabilities.

            ## Softmax
            **Softmax** applies exp to every score, then divides each result by their total so they add up to 1:

            > softmax(scores)ᵢ = exp(scoreᵢ) / (sum of exp(score) over all scores)

            Scores [2, 1, 0] → exp gives [7.389, 2.718, 1.000], total 11.107 → probabilities [0.665, 0.245, 0.090]. They're positive, in the same order, and add up to 1.

            ## The overflow trap, and the fix
            `math.exp(1000)` is too big for a float, and Python raises an `OverflowError`. Real logits can be large, so a naive softmax crashes or returns garbage. The fix uses the ratio property: **subtracting the same number from every score doesn't change the softmax**, because exp(a − c) / exp(b − c) = exp(a) / exp(b). So subtract the **largest** score first. Then the biggest value fed to exp is 0 (giving exactly 1), everything else is negative (giving numbers between 0 and 1), and nothing can overflow.

            Scores [1000, 1001] → subtract 1001 → [−1, 0] → exp → [0.368, 1.000] → divide by 1.368 → [0.269, 0.731].

            ## Logarithms: the undo button for exp
            The **natural logarithm** log(y) answers "e to what power gives y?" So log(e³) = 3, log(1) = 0, and the log of any number between 0 and 1 is negative (log(0.5) ≈ −0.693). Log undoes exp, and exp undoes log.

            The property that matters most: **the log of a product is the sum of the logs**. log(a × b) = log(a) + log(b).

            Remember the underflow problem from the probability lesson? A sentence's probability is a product of hundreds of token probabilities, and that product gets too small for a float to hold. Its log is a *sum* of hundreds of moderate negative numbers, like −2.3 + −0.4 + −5.1 + …, which floats handle easily. So models, training code and evaluations work with **log-probabilities** almost everywhere. A log-probability of 0 means certain; −0.1 means very likely; −10 means very unlikely.

            ## Log-sum-exp and log-softmax
            The log of softmax's denominator, log(sum of exp(scores)), shows up so often it has a name: **log-sum-exp**. It overflows for the same reason softmax does, and it's fixed with the same trick: subtract the largest score m, then add it back after taking the log:

            > log-sum-exp(scores) = m + log(sum of exp(score − m))

            **Log-softmax** gives log-probabilities directly: each score minus the log-sum-exp. It's stable, and more accurate than computing softmax and then taking logs, because it never rounds a tiny probability down to 0 (whose log is minus infinity). Training code uses log-softmax for exactly this reason.

            ## What the symbols stand for
            - **zᵢ**: the score (logit) for option i, a **variable** produced by the model. In an LLM there's one per vocabulary token.
            - **e**: Euler's number, a **constant** ≈ 2.71828.
            - **exp(x)**: e raised to the power x.
            - **log(y)**: the natural logarithm, the power you'd raise e to in order to get y.
            - **m**: the largest score, max(zᵢ), used only to keep the arithmetic safe.
            - **softmax(z)ᵢ** = exp(zᵢ − m) / Σⱼ exp(zⱼ − m): the probability of option i. (Σⱼ means "add up over every option j.")
            - **log-softmax(z)ᵢ** = zᵢ − (m + log Σⱼ exp(zⱼ − m)): its log-probability.

            ## Walk through one problem
            A tiny model scores three next tokens: "cat" 3.0, "dog" 1.0, "the" 0.0. What's P(cat)?

            1. The largest score m is 3.0. Shifted scores: 0, −2, −3.
            2. Exponentials: exp(0) = 1, exp(−2) ≈ 0.1353, exp(−3) ≈ 0.0498. Total ≈ 1.1851.
            3. P(cat) = 1 / 1.1851 ≈ 0.844. P(dog) ≈ 0.114. P(the) ≈ 0.042. They add up to 1.
            4. Log-probability of "cat": log(0.844) ≈ −0.170. Via log-softmax: 3.0 − (3.0 + log(1.1851)) = −log(1.1851) ≈ −0.170. Same answer, no division needed.

            ## Explain it to your team
            "The model's last layer gives a raw score per token. Softmax turns those scores into probabilities by exponentiating and normalizing, and we subtract the largest score first so it never overflows. Because a whole sentence's probability is a product of hundreds of small numbers, we work with log-probabilities instead, which turns the product into a sum. Log-softmax computes those directly."
            """,
            """
            scores        [3.0, 1.0, 0.0]
            minus max     [0.0, −2.0, −3.0]           (same softmax, no overflow)
            exp           [1.000, 0.135, 0.050]       total 1.185
            softmax       [0.844, 0.114, 0.042]       adds up to 1
            log-softmax   [−0.170, −2.170, −3.170]    = score − (max + log(total))
            """,
            LlmCode(
                "Stable softmax",
                "Implement softmax and its log-space relatives so they never overflow or underflow, even for scores in the thousands. Use `math` only.",
                [
                    "`softmax(scores)` returns probabilities that add up to 1, computed after subtracting the largest score",
                    "`log_sum_exp(scores)` returns log(Σ exp(score)) stably, as m + log Σ exp(score − m)",
                    "`log_softmax(scores)` returns each score minus `log_sum_exp(scores)`",
                    "`sequence_log_prob(probabilities)` returns the sum of the logs of a list of probabilities (all greater than 0)",
                ],
                """
                import math


                def softmax(scores):
                    ...


                def log_sum_exp(scores):
                    ...


                def log_softmax(scores):
                    ...


                def sequence_log_prob(probabilities):
                    ...
                """,
                "Find `m = max(scores)` first. `softmax` is `[math.exp(s - m) / total for s in scores]` where `total = sum(math.exp(s - m) for s in scores)`. Never call `math.exp` on an unshifted score.",
                ["softmax values and their sum", "no overflow for large scores", "log-sum-exp and log-softmax", "log-probability of a sequence"])
        ),
        new(
            "llm-derivatives-chain-rule",
            "Math foundations, in words",
            "Rates of change and the chain rule",
            "A derivative says how fast an output changes when you nudge an input. The gradient does that for every knob at once, and the chain rule links the rates along a chain of steps, which is how networks learn.",
            "The derivative of a function at a point is the slope there: how much the output changes per tiny nudge of the input. You can estimate it by nudging the input a little each way and dividing the output change by the input change. With several inputs, the list of one-at-a-time slopes is the gradient, which points uphill. For a chain of steps, the rate of the whole chain is the rates of the steps multiplied together, the chain rule. Backpropagation is the chain rule applied to a whole network.",
            [
                ("llm-models-are-functions", "learning means adjusting parameters, and derivatives say which way to adjust them."),
                ("llm-exp-log-softmax", "you'll find the slopes of exp and log."),
            ],
            """
            ## The idea in plain words
            You're driving, and the speedometer reads 60 mph. That number is a **rate of change**: how fast your position changes as time passes. It's not your total distance; it's "how much more distance per bit more time, right now."

            A **derivative** is that same idea for any function: how fast the output changes when you nudge the input, at a particular point. On a graph, it's the **slope** of the curve right there. Steep uphill means a large positive derivative, flat means zero, and downhill means negative.

            Why does machine learning care? Because training is about turning knobs to make the error smaller. The derivative of the error with respect to a knob tells you two things at once: which way to turn the knob (the sign) and how sensitive the error is to it (the size). That's all gradient descent needs.

            ## Where you'll see it
            - **Driving:** speed is the derivative of position over time; acceleration is the derivative of speed.
            - **Strategy games:** your income per second is the derivative of your bank balance. Building a mine raises that derivative.
            - **Business:** "marginal cost" is the derivative of total cost with respect to units made: how much the next unit adds.
            - **Training a model:** the derivative of the loss with respect to each parameter tells training which way to move that parameter.

            ## From average slope to slope at a point
            The slope between two points is "rise over run": the change in output divided by the change in input. For f(x) = x², between x = 3 and x = 4, the output goes from 9 to 16, so the average slope is 7 / 1 = 7.

            Now bring the points closer. Between 3 and 3.1: (9.61 − 9) / 0.1 = 6.1. Between 3 and 3.001: about 6.001. As the gap shrinks, the slope settles on **6**. That limiting value is the derivative of x² at x = 3.

            For x², the derivative at any x is 2x. In words: the steepness of x² doubles as fast as x itself grows. A few more rules worth knowing, each in words first:

            - A constant (like 5) doesn't change, so its derivative is **0**.
            - **x** grows one-for-one, so its derivative is **1**. And **3x** changes 3 times as fast, so its derivative is **3**.
            - The derivative of a **sum** is the sum of the derivatives: if two things both push the output, their rates add.
            - **exp(x)** is its own derivative: the higher it is, the faster it grows. That's the defining property of e.
            - **log(x)** has derivative **1/x**: it climbs steeply near 0 and flattens out for large x.

            ## Estimating a derivative numerically
            You don't always have a rule handy, but you can always *measure*. Nudge the input a tiny amount h each way, and divide the change in output by the change in input:

            > slope ≈ (f(x + h) − f(x − h)) / (2h)

            This is called a **central difference**, and with h around 0.00001 it's usually accurate to many decimal places. Training doesn't use it, because it needs two extra function evaluations per parameter, which is far too slow for billions of parameters. But it's the standard way to **check** that a hand-written derivative is right, called **gradient checking**. You'll use it to test your own backpropagation engine.

            ## Many knobs: partial derivatives and the gradient
            Most functions in ML have many inputs. The error of a linear model depends on every weight and the bias. Nudge **one** knob while holding the others fixed and measure the rate: that's a **partial derivative**. Do it for every knob and put the results in a list, and you have the **gradient**.

            The gradient is a vector that points in the direction in which the function **increases fastest**. Its negative points downhill, toward lower error, and that's the direction gradient descent steps in.

            ## The chain rule: rates multiply along a chain
            Real models are chains of steps: the output of one step feeds the next. Say a factory makes 3 tanks per minute, and each tank adds 2 points of army strength. How fast does strength grow? 3 tanks/minute × 2 strength/tank = **6 strength per minute**. The rate through a chain is the **product of the rates of each link**.

            That's the **chain rule**. If y depends on u, and u depends on x, then:

            > (rate of y with respect to x) = (rate of y with respect to u) × (rate of u with respect to x)

            Example: y = (3x + 1)². The inner step is u = 3x + 1, with rate 3. The outer step is y = u², with rate 2u. At x = 1, u = 4, so the outer rate is 8, and the whole chain's rate is 8 × 3 = **24**. Check numerically: (3 × 1.0001 + 1)² − (3 × 0.9999 + 1)² = 16.00240009 − 15.99760009 = 0.0048, divided by 0.0002 gives 24. ✓

            A neural network is a chain of hundreds of steps. **Backpropagation** applies the chain rule from the error back through every step, multiplying rates as it goes, to get the gradient for every parameter in a single backward sweep. You'll build exactly that in the neural networks module.

            ## What the symbols stand for
            - **f(x)**: a function of x; x is the **variable** you nudge.
            - **h**: the nudge size, a small **constant** you choose, like 0.00001.
            - **f′(x)**, also written **df/dx**: the derivative of f at x, read "the rate of f per unit of x."
            - **∂L/∂w**: a partial derivative, "the rate of L per unit of w, with everything else held fixed." The curly ∂ just signals that there are other inputs being held still.
            - **∇L** ("del L"): the gradient, the list of all partial derivatives of L.
            - Chain rule: **dy/dx = (dy/du) × (du/dx)**.

            ## Walk through one problem
            A loss L(w) = (2w − 6)² measures how wrong a one-knob model is. At w = 1, which way should w move, and how steep is it?

            1. It's a chain: the inner step is u = 2w − 6 (rate 2), and the outer step is L = u² (rate 2u).
            2. At w = 1: u = 2 − 6 = −4. Outer rate: 2 × (−4) = −8. Chain: −8 × 2 = **−16**.
            3. A negative derivative means increasing w *decreases* the loss, so move w up. The size 16 says the loss is very sensitive to w here.
            4. Sanity check: the loss is 0 when 2w = 6, at w = 3, which is indeed above 1.

            ## Explain it to your team
            "A derivative is a rate of change: how much the error moves when you nudge one parameter. The gradient collects that rate for every parameter, and it points uphill, so training steps the opposite way. A network is a long chain of steps, and the chain rule says the rate through a chain is the product of the rates of its links. Backpropagation is just the chain rule applied efficiently to the whole network."
            """,
            """
            f(x) = x²       slope between 3 and 3.001 ≈ 6.001  →  f′(3) = 6
            central difference: f′(x) ≈ (f(x + h) − f(x − h)) / 2h,  h = 0.00001
            gradient of f(a, b) = [∂f/∂a, ∂f/∂b]      (nudge one knob at a time)
            chain rule: y = (3x + 1)²  →  dy/dx = 2(3x + 1) × 3  →  at x = 1: 8 × 3 = 24
            """,
            LlmCode(
                "Measure slopes",
                "Write numerical tools for derivatives, gradients and the chain rule. You'll use them to check your own backpropagation engine later. Plain Python only.",
                [
                    "`derivative(f, x, h=1e-5)` returns the central-difference estimate (f(x + h) − f(x − h)) / (2h)",
                    "`gradient(f, point, h=1e-5)` takes a function of a list of numbers and returns its partial derivatives at `point`, nudging one entry at a time without changing `point`",
                    "`chain_derivative(outer_rate, inner, inner_rate, x)` returns outer_rate(inner(x)) × inner_rate(x), the chain rule",
                    "`tangent_line(f, x0)` returns `(slope, intercept)` of the line touching f at x0, using `derivative`",
                ],
                """
                def derivative(f, x, h=1e-5):
                    ...


                def gradient(f, point, h=1e-5):
                    ...


                def chain_derivative(outer_rate, inner, inner_rate, x):
                    ...


                def tangent_line(f, x0):
                    ...
                """,
                "For `gradient`, make a copy of the point for each entry, add h to that entry in one copy and subtract h in another, and use the central difference. The tangent line passes through (x0, f(x0)) with the derivative as its slope, so its intercept is f(x0) − slope × x0.",
                ["central difference", "gradient with copies", "chain rule", "tangent line"])
        ),
    ];
}
