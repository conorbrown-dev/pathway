static partial class Curriculum
{
    // Module 6 of the AI & LLM course: the transformer, piece by piece. Why fixed windows fail,
    // self-attention, heads and positions (including RoPE), the block with residuals and norms,
    // decoding strategies, and a local lab that trains a small GPT.
    private static LlmLessonSpec[] LlmTransformerSpecs() =>
    [
        new(
            "llm-why-attention",
            "Transformers",
            "Why context needs attention",
            "Counting-based models run out of data as soon as they look further back, and older neural designs squeeze the whole past into one vector. Attention lets every position look directly at every earlier one.",
            "A model that conditions on the previous n − 1 tokens needs a table entry for every possible context, and the number of possible contexts grows so fast that almost every long context in new text has never been seen before. Recurrent networks avoid the table by carrying one summary vector forward, but that vector is a bottleneck and must be computed one step at a time. Attention instead lets each position compute a weighted mix of every earlier position's information, chosen by content, all in parallel.",
            [
                ("llm-next-token-prediction", "the bigram model is the starting point this lesson stretches until it breaks."),
                ("llm-embeddings", "attention mixes embedding vectors rather than counting token IDs."),
            ],
            """
            ## The problem: one token of memory isn't enough
            The bigram model sees one token back. To predict the last word of "The keys that I left on the kitchen table **are** …" (rather than "is"), a model has to remember "keys", eight tokens earlier. Real text is full of such long-range links: a function's return type decides a line forty lines later, and a character named on page one is "she" on page three.

            ## Attempt 1: look further back with bigger tables
            The obvious fix: condition on the previous **n − 1** tokens instead of one, an **n-gram model**. A trigram model uses two previous tokens, a 5-gram model uses four. The table needs one row for every possible context. With a vocabulary of V tokens there are V^(n−1) possible contexts. For V = 50,000:

            - bigram (n = 2): 50,000 contexts;
            - trigram: 2.5 billion;
            - 5-gram: 6 × 10¹⁸, more than any dataset could ever fill.

            So the table is almost entirely empty. That's **data sparsity**: the longer the context, the less likely it is that this exact context ever appeared in training. In the exercise you'll measure it on real text, and watch the fraction of never-seen contexts climb toward 100% as n grows. Smoothing can't fix it, because a never-seen context gives the model nothing to go on except a guess.

            The deeper flaw: an n-gram table treats every context as unrelated to every other. "the cat sat on the" and "the dog sat on the" are separate rows with separate counts, even though they predict nearly the same next words. Nothing is shared.

            ## Attempt 2: neural networks with a fixed window
            Replace the table with a neural network that reads the **embeddings** of the last few tokens. Similar tokens have similar embeddings, so similar contexts now share what they learn: the sparsity problem is softened. But the window is still fixed. With a window of 8, the model can never use token 9 back, however important.

            ## Attempt 3: recurrent networks
            A **recurrent neural network** (RNN) reads tokens one at a time and carries a **hidden state**, one vector updated at every step to summarize everything so far. No window limit, in principle. In practice, two problems:

            1. **The bottleneck.** Everything about a 2,000-token history must be squeezed into one fixed-size vector. Details from long ago get overwritten. Improved designs (LSTMs, GRUs) helped but didn't remove the limit.
            2. **It's sequential.** Step 1,000 can't be computed until step 999 is done. Training can't spread across the thousands of parallel units in a GPU, so training on internet-scale data takes far too long.

            ## The fix: attention
            **Attention** takes a different approach. Instead of squeezing the past into one vector, keep **every** earlier position's vector around, and let each position **look back directly** at all of them, deciding by content which ones matter. At "are", the model can put most of its weight on the vector at "keys" and nearly none on "kitchen", no matter how far apart they are.

            Three properties made this win:

            - **Direct access:** any earlier position is one step away, so long-range links don't fade with distance.
            - **Content-based:** what to look at is decided by comparing vectors (with dot products), so the same mechanism works for any kind of link: subject–verb, bracket matching, a variable's definition.
            - **Parallel:** during training, every position's attention can be computed at once with matrix multiplications, which is exactly what GPUs are good at.

            The 2017 paper "Attention Is All You Need" built a whole network, the **transformer**, out of attention plus simple feed-forward layers. Every major LLM since uses it. The cost: each position compares itself with every earlier position, so the work grows with the **square** of the sequence length. Doubling the context roughly quadruples attention's cost, which is why long context windows are expensive.

            ## What the words mean
            - **n-gram model:** predicts the next token from the previous n − 1 tokens using a table of counts.
            - **Context:** the tokens a model conditions on when predicting the next one.
            - **Data sparsity:** most possible contexts never appear in the training data.
            - **Recurrent neural network (RNN):** reads tokens one at a time, carrying a hidden state vector forward.
            - **Attention:** each position computes a weighted average of other positions' vectors, with weights based on content.
            - **Transformer:** a network built from attention and feed-forward layers.

            ## Walk through one problem
            A corpus has 1,000,000 tokens and a vocabulary of 10,000. How full can a trigram table possibly be?

            1. Possible contexts (two previous tokens): 10,000 × 10,000 = 100,000,000.
            2. Contexts the corpus can contain: at most one per position, about 1,000,000, and many of those repeat.
            3. So at most 1% of possible contexts can ever be observed, and in practice far fewer. A new sentence will constantly hit contexts with no data.

            ## Explain it to your team
            "Counting-based models break as soon as they look a few tokens back: the number of possible contexts explodes, so almost every long context is new. Older neural networks squeezed the whole past into one vector and had to process tokens one at a time. Attention keeps every position's vector and lets each position look back at all of them directly, choosing what's relevant by content, and it trains in parallel on GPUs. That's the core of the transformer."
            """,
            """
            contexts an n-gram table must cover = V^(n − 1)
              V = 50,000:  n = 2 → 5 × 10⁴     n = 3 → 2.5 × 10⁹     n = 5 → 6.25 × 10¹⁸
            unseen fraction of test contexts climbs toward 1 as n grows (you'll measure it)
            attention: each position looks directly at every earlier position, weighted by relevance
            cost: grows with (sequence length)²
            """,
            LlmCode(
                "Measure data sparsity",
                "Show why counting longer contexts fails: count the contexts an n-gram model would need, and measure how many contexts in new text were never seen in training. Standard library only.",
                [
                    "`possible_contexts(vocab_size, n)` returns vocab_size ** (n − 1), the number of rows an n-gram table needs",
                    "`contexts(tokens, n)` returns the list of (n − 1)-token contexts as tuples, one for each position that has a next token: `tokens[i:i + n - 1]` for i from 0 while `i + n - 1 < len(tokens)`",
                    "`unseen_fraction(train_tokens, test_tokens, n)` returns the fraction of test contexts (counting repeats) whose tuple never appears among the training contexts",
                    "`sparsity_curve(train_tokens, test_tokens, max_n)` returns `[unseen_fraction(..., n) for n in 2 … max_n]`",
                ],
                """
                def possible_contexts(vocab_size, n):
                    ...


                def contexts(tokens, n):
                    ...


                def unseen_fraction(train_tokens, test_tokens, n):
                    ...


                def sparsity_curve(train_tokens, test_tokens, max_n):
                    ...
                """,
                "Put the training contexts in a `set` so membership checks are fast. A context of length n − 1 is `tuple(tokens[i:i + n - 1])`.",
                ["counting possible contexts", "extracting contexts", "unseen fraction", "sparsity grows with n on real text"])
        ),
        new(
            "llm-self-attention",
            "Transformers",
            "Self-attention: queries, keys and values",
            "Each position asks a question (query), every position advertises what it holds (key), and each position collects a blend of the values whose keys match its question. A causal mask keeps it from peeking at the future.",
            "Self-attention projects each position's vector into a query, a key and a value. Each query is compared with every key using a dot product scaled by 1/√d; a causal mask removes future positions; softmax turns each row of scores into weights; and the output at each position is the weighted sum of the values. In matrix form it is softmax(QKᵀ/√d + mask)V, computed for every position at once.",
            [
                ("llm-why-attention", "this lesson builds the mechanism that lesson motivated."),
                ("llm-matrices", "queries, keys and values are matrix products, and scores are QKᵀ."),
                ("llm-exp-log-softmax", "each row of scores becomes weights with a stable softmax."),
            ],
            """
            ## The idea in plain words
            Think of a library search. You walk in with a **question** ("books about sailing"). Every book has a **label** on its spine. You compare your question with every label, and the closer the match, the more of that book's **contents** you take home. Self-attention works the same way, for every position in a sequence at once:

            - Each position makes a **query**: what am I looking for?
            - Each position offers a **key**: what do I contain, for matching purposes?
            - Each position offers a **value**: what do I hand over if I'm chosen?

            The query of one position is compared with the keys of all positions; good matches get high weights; the output is the weighted blend of the values.

            The query, key and value are all made from the same input vector (that's the "self" in self-attention) by multiplying it by three different learned matrices, **W_Q**, **W_K** and **W_V**. Training decides what to ask and what to advertise. At the position of "are" in "the keys … are", training might produce a query that means "where's my subject?", and the key at "keys" might mean "I'm a plural noun subject."

            ## Step by step
            Take a sequence of T positions, each with a vector of size d.

            1. **Project.** For each position, compute q = x·W_Q, k = x·W_K, v = x·W_V. Stack them as rows: Q, K and V are each T × d matrices.
            2. **Score.** For each pair (i, j), the score is the dot product of query i with key j: how well what i is looking for matches what j offers. That's the matrix **QKᵀ**, size T × T.
            3. **Scale.** Divide every score by **√d**. Dot products of longer vectors are naturally bigger: with d = 64 numbers, a dot product of random vectors has a typical size about 8 (√64) times bigger than with one number. Big scores make softmax nearly all-or-nothing, which leaves tiny gradients and stalls learning. Dividing by √d keeps scores in a comfortable range whatever d is.
            4. **Mask.** In a language model, position i must not look at positions after it, because during training it's predicting the next token, and seeing it would be cheating. So set every score with j > i to **−∞**. This is the **causal mask**. After softmax, those become weight 0.
            5. **Softmax each row.** Row i becomes a set of weights over positions 0 … i that are positive and add up to 1: where position i "looks."
            6. **Blend.** Output for position i = the weighted sum of the value rows. In matrix form: weights × V.

            All together: **output = softmax(QKᵀ / √d + mask) · V**. One formula, a few matrix multiplications, and every position is computed at once, which is the parallelism the previous lesson promised.

            ## Why the causal mask makes training efficient
            With the mask, position i's output depends only on positions 0 … i. So one forward pass over a 1,000-token document gives 1,000 next-token predictions at once: position 0 predicts token 1, position 1 predicts token 2, and so on, each seeing only its own past. Every prediction contributes to the loss. That's why LLM pretraining is so efficient per document.

            ## The cost
            The score matrix has T × T entries. At T = 1,000 that's a million scores per attention layer; at T = 100,000, ten billion. Compute and memory for attention grow with the **square** of the context length, the main reason long contexts are expensive. A lot of engineering (FlashAttention, sliding windows, sparse patterns) goes into managing this.

            ## What the symbols stand for
            - **T**: the number of positions (tokens) in the sequence, a **variable** per input.
            - **d**: the size of each query/key vector, a **hyperparameter** such as 64 or 128.
            - **X**: the input, T × d_model, one row per position.
            - **W_Q, W_K, W_V**: learned projection matrices, **parameters**.
            - **Q, K, V**: the T × d matrices of queries, keys and values.
            - **QKᵀ**: the T × T matrix of scores; entry (i, j) is query i · key j.
            - **mask**: 0 where j ≤ i and −∞ where j > i.
            - **A** = softmax(QKᵀ/√d + mask): the attention weights; each row adds up to 1.
            - **Output** = A·V, T × d.

            ## Walk through one problem
            Two positions, d = 2. Q = [[1, 0], [1, 1]], K = [[1, 0], [0, 1]], V = [[10, 0], [0, 10]]. Causal.

            1. Scores: q₀·k₀ = 1, q₀·k₁ = 0; q₁·k₀ = 1, q₁·k₁ = 1. Divide by √2 ≈ 1.414: [[0.707, 0], [0.707, 0.707]].
            2. Mask: position 0 can't see position 1, so its second score becomes −∞: [[0.707, −∞], [0.707, 0.707]].
            3. Softmax per row: row 0 → [1, 0] (only itself to look at). Row 1 → [0.5, 0.5] (equal scores).
            4. Blend: position 0 outputs V[0] = [10, 0]. Position 1 outputs 0.5·[10, 0] + 0.5·[0, 10] = [5, 5].

            ## Explain it to your team
            "In self-attention, each token makes a query (what it's looking for), a key (what it offers) and a value (what it hands over), all through learned projections. Each query is compared with every earlier key by dot product, scaled, turned into weights with softmax, and used to blend the values. The causal mask stops tokens from seeing the future, which lets one pass over a document train on every position at once. The cost grows with the square of the context length."
            """,
            """
            Q = X·W_Q    K = X·W_K    V = X·W_V                 (each T × d)
            scores = Q Kᵀ / √d                                  (T × T)
            causal mask: scores[i][j] = −∞ for j > i
            weights = softmax(each row of scores)               (rows add up to 1)
            output  = weights · V                               (T × d)
            """,
            LlmCode(
                "Implement causal self-attention",
                "Write scaled dot-product attention with an optional causal mask, on matrices stored as lists of rows. Plain Python and `math`. Use a stable softmax, and treat masked positions as weight exactly 0.",
                [
                    "`attention_scores(Q, K)` returns the T × T matrix of query·key dot products divided by √d, where d is the length of each query",
                    "`causal_mask(scores)` returns a copy with every entry above the diagonal (j > i) set to `-math.inf`",
                    "`row_softmax(scores)` applies a stable softmax to each row, giving exactly 0 for `-inf` entries",
                    "`attention(Q, K, V, causal)` returns `(output, weights)` where weights = row_softmax of the (optionally masked) scores and output = weights · V",
                ],
                """
                import math


                def attention_scores(Q, K):
                    ...


                def causal_mask(scores):
                    ...


                def row_softmax(scores):
                    ...


                def attention(Q, K, V, causal):
                    ...
                """,
                "`math.exp(-math.inf - m)` is exactly 0.0 in Python, so a stable softmax (subtract the row's max) handles masked entries naturally. The output entry (i, c) is the sum over j of `weights[i][j] * V[j][c]`.",
                ["scaled scores", "causal mask", "softmax rows add up to 1", "attention output, with and without the mask"])
        ),
        new(
            "llm-multi-head-position",
            "Transformers",
            "Multiple heads, and how attention knows word order",
            "Several attention heads look for different relationships in parallel, and position information has to be added on purpose because attention by itself ignores order. Modern LLMs use RoPE, which rotates queries and keys by their position.",
            "Multi-head attention splits the model's vector into several smaller heads that each attend independently, then concatenates their outputs and mixes them with one more learned matrix. Attention on its own is blind to order, so models add position information: originally as fixed sine and cosine patterns added to the embeddings, and now usually as rotary embeddings (RoPE), which rotate each pair of query and key numbers by an angle proportional to the position, so their dot product depends only on how far apart the two tokens are.",
            [
                ("llm-self-attention", "each head is one self-attention computation."),
                ("llm-vectors-dot-product", "RoPE is designed around what rotation does to dot products."),
            ],
            """
            ## Several heads
            One attention computation produces one set of weights per position: one "way of looking." But a token usually needs several kinds of information at once. In "she gave him the book", the token "gave" might want its subject, its object and its recipient. So transformers run several attention computations side by side, called **heads**, each with its own W_Q, W_K and W_V.

            To keep the cost the same, the model's vector of size d_model is split among the heads: with d_model = 768 and 12 heads, each head works with vectors of size 64. Each head attends independently. Their outputs (12 vectors of 64 numbers each) are **concatenated** back into one vector of 768, then multiplied by one more learned matrix, **W_O**, which mixes what the heads found.

            Researchers studying trained models have found heads with recognizable jobs: some attend to the previous token, some track matching brackets, some copy a token that appeared earlier after the same prefix (**induction heads**, thought to be central to in-context learning). Many heads have no clear single job. Interpretability research is still working out how they divide the work.

            ## The order problem
            Look back at the attention formula. Scores compare contents; weights blend values. Nothing in it refers to *where* a token is. If you shuffled the input tokens (and the mask), each token would get the same output as before, just in a different place. "dog bites man" and "man bites dog" would look identical apart from the mask. Attention is **order-blind**, so position must be supplied separately.

            ## Fix 1: add a position vector
            The original transformer added a fixed **positional encoding** to each token's embedding before the first layer: a vector of sines and cosines of the position at many different frequencies, like a set of clock hands spinning at different speeds. Each position gets a unique pattern, and nearby positions get similar patterns. GPT-2 instead **learned** a position embedding table, one row per position, which works well but can't handle positions beyond the table's length.

            ## Fix 2: rotate queries and keys (RoPE)
            Most current LLMs, including Llama, Qwen and Mistral, use **rotary position embeddings (RoPE)**. Instead of adding anything to the embedding, RoPE modifies the queries and keys inside every attention layer:

            1. Group each query and key vector into pairs of numbers: (q₀, q₁), (q₂, q₃), and so on. Picture each pair as a little 2-D arrow.
            2. At position m, **rotate** each pair's arrow by an angle of m × θᵢ, where θᵢ is a different fixed speed for each pair (fast for the first pairs, slow for later ones).
            3. Compute attention as usual.

            Why rotation? Because the dot product of two arrows depends only on their lengths and the **angle between them**. Rotate the query by m·θ and the key by n·θ, and the angle between them changes by (m − n)·θ. So the score between a query at position m and a key at position n depends on their **relative distance**, m − n, not on where they sit in the document. "The word two before me" means the same thing at position 10 and at position 10,000. That relative behavior is exactly what language needs, and it's why RoPE models can be stretched to longer contexts than they were trained on, with some adjustments.

            The rotation of a pair (a, b) by angle φ, in words: the new first number is a·cos φ − b·sin φ, and the new second number is a·sin φ + b·cos φ. This turns the arrow without changing its length. The speeds are θᵢ = base^(−2i/d), with the base usually 10,000, so the pairs spin at a range of speeds from one radian per position down to very slow.

            ## What the symbols stand for
            - **h**: the number of heads, a **hyperparameter** such as 12. **d_head** = d_model / h, the size of each head's vectors.
            - **W_O**: the output projection that mixes the concatenated heads, a learned **parameter**.
            - **m, n**: the positions of a query and a key, counting from 0.
            - **θᵢ** = base^(−2i/d): the rotation speed for pair i; **base** is a **constant**, usually 10,000.
            - **Rotation by φ**: (a, b) → (a·cos φ − b·sin φ, a·sin φ + b·cos φ).
            - **RoPE property:** rope(q, m) · rope(k, n) depends only on q, k and m − n.

            ## Walk through one problem
            One pair, θ = 0.5 radians per position. Query (1, 0) at position 3; key (1, 0) at position 1.

            1. Rotate the query by 3 × 0.5 = 1.5 radians: (cos 1.5, sin 1.5) ≈ (0.071, 0.997).
            2. Rotate the key by 1 × 0.5 = 0.5 radians: (cos 0.5, sin 0.5) ≈ (0.878, 0.479).
            3. Dot product ≈ 0.071 × 0.878 + 0.997 × 0.479 ≈ 0.540, which is cos(1.0): it depends only on the gap of 2 positions.
            4. Move both by 100 positions (103 and 101). The gap is still 2, so the dot product is still cos(1.0) ≈ 0.540.

            ## Explain it to your team
            "Transformers run several attention heads in parallel, each free to look for a different relationship, then mix their results. Attention by itself ignores word order, so position is added deliberately. Modern LLMs use rotary embeddings: they rotate queries and keys by an angle proportional to position, so attention scores depend on how far apart two tokens are, not where they sit."
            """,
            """
            split heads: a vector of 8 numbers with 2 heads → [first 4] [last 4]; concatenate to merge back
            sinusoidal encoding at position p, pair i:  sin(p / 10000^(2i/d)),  cos(p / 10000^(2i/d))
            RoPE: rotate pair (a, b) at position m by φ = m·θᵢ:
                  (a, b) → (a·cos φ − b·sin φ,  a·sin φ + b·cos φ)
            rope(q, m) · rope(k, n) depends only on m − n
            """,
            LlmCode(
                "Heads and rotary positions",
                "Implement splitting into heads, sinusoidal position encodings, and RoPE. The grader checks RoPE's key property: shifting both positions by the same amount leaves the score unchanged. Plain Python and `math`.",
                [
                    "`split_heads(vector, n_heads)` returns `n_heads` consecutive equal slices of the vector (raise `ValueError` if the length doesn't divide evenly), and `merge_heads(heads)` concatenates them back",
                    "`sinusoidal_positions(seq_len, d)` returns seq_len rows of d numbers where entries 2i and 2i + 1 of row p are sin and cos of p / 10000 ** (2i / d)",
                    "`rope(vector, position, base=10000.0)` rotates each pair (vector[2i], vector[2i + 1]) by the angle position × base ** (−2i / d), where d is the vector length (even)",
                    "Rotation must keep each vector's length, and `rope(q, m) · rope(k, n)` must depend only on m − n",
                ],
                """
                import math


                def split_heads(vector, n_heads):
                    ...


                def merge_heads(heads):
                    ...


                def sinusoidal_positions(seq_len, d):
                    ...


                def rope(vector, position, base=10000.0):
                    ...
                """,
                "Loop over pair index i from 0 to d // 2 − 1. The angle is `position * base ** (-2 * i / d)`; read `a, b = vector[2 * i], vector[2 * i + 1]` and write `a * cos - b * sin` and `a * sin + b * cos` into a new list.",
                ["splitting and merging heads", "sinusoidal encodings", "RoPE keeps vector lengths", "RoPE scores depend only on relative position"])
        ),
        new(
            "llm-transformer-block",
            "Transformers",
            "The transformer block, stacked",
            "Attention plus a feed-forward network, each wrapped in normalization and a residual connection, repeated dozens of times: that is the whole architecture. You'll count GPT-2's 124 million parameters exactly.",
            "A transformer block normalizes its input, applies attention, and adds the result back to its input (a residual connection); then it normalizes again, applies a feed-forward network that widens each vector about fourfold and narrows it back, and adds that back too. The running sum that flows through every block is the residual stream. Layer normalization rescales each vector to mean 0 and spread 1. Stacking L blocks between an embedding table and an output projection gives a GPT, with roughly 12·L·d² parameters in its blocks.",
            [
                ("llm-multi-head-position", "the attention sublayer is multi-head attention."),
                ("llm-neurons-layers", "the feed-forward sublayer is a two-layer network."),
                ("llm-data-as-numbers", "layer norm is standardization applied to one vector."),
                ("llm-embeddings", "the model starts with an embedding lookup and may reuse that table at the output."),
            ],
            """
            ## The block
            A transformer is the same **block** repeated many times: 12 times in GPT-2 small, 80 or more in big models. Each block has two sublayers, and each sublayer is wrapped the same way:

            1. **x = x + Attention(Norm(x))**: normalize, let every position gather information from other positions, then **add** the result back onto x.
            2. **x = x + FeedForward(Norm(x))**: normalize, process each position's vector on its own with a small two-layer network, then add that back too.

            That's it. The rest of this lesson explains the three ingredients that aren't attention: the residual connections (the "x +"), normalization, and the feed-forward network.

            ## Residual connections and the residual stream
            Each sublayer doesn't replace x; it computes a **change** and adds it on. That running sum, flowing from the embedding at the bottom through every block to the output at the top, is called the **residual stream**. Think of it as a shared workspace: each sublayer reads the stream, writes an update into it, and the next sublayer sees the result.

            Residual connections matter for two reasons. First, gradients flow straight back through the additions (addition passes gradient through unchanged, as you saw in your autograd engine), so even an 80-block network trains; without them, very deep networks barely learn. Second, a block that has nothing useful to add can write roughly zero and leave the stream intact.

            ## Normalization
            The numbers in the stream can drift to very large or very small sizes as blocks add to it, which destabilizes training. **Layer normalization** fixes the scale of each vector before a sublayer reads it, by standardizing it: subtract the vector's own mean, divide by its own spread (with a tiny ε added so nothing divides by zero), then multiply by a learned **gain** γ and add a learned **shift** β, entry by entry, so the model can still choose its own scale. It's the z-score from the data lesson, applied to one vector at a time.

            Many modern LLMs use **RMSNorm**, a simpler version: skip the mean subtraction, divide each entry by the root-mean-square of the vector (the square root of the average of the squares), then multiply by γ. It works as well and is cheaper.

            Normalizing *before* each sublayer (**pre-norm**, as above) is the modern standard because it trains more stably than the original transformer's post-norm layout. A final norm is applied once more at the top, before the output projection.

            ## The feed-forward network
            Attention moves information **between** positions. The **feed-forward network** (also called the MLP) then processes each position **independently**: a dense layer that widens the vector, typically from d to 4d, a nonlinearity (GELU, or a gated variant called SwiGLU in many LLMs), and a dense layer back down to d. About two-thirds of a transformer's parameters live here. Interpretability research suggests that much of a model's factual knowledge ("Paris is in France") is stored in these layers.

            ## From tokens to logits
            The whole GPT-style model:

            1. **Embed:** look up each token's embedding (plus a learned position embedding in GPT-2; RoPE models skip this).
            2. **Blocks:** run the residual stream through L blocks.
            3. **Final norm**, then the **unembedding**: multiply by a d × V matrix to get one logit per vocabulary token at every position. Many models reuse the embedding table here (**weight tying**), saving V × d parameters.
            4. Softmax and cross-entropy for training; sampling for generation.

            ## Counting the parameters
            For a GPT-2-style model with vocabulary V, context length C, width d and L blocks (layer norms with gain and shift, biases everywhere, a 4d-wide MLP):

            - Token embeddings: V·d. Position embeddings: C·d.
            - Per block: two layer norms, 2·2d; attention's combined Q, K, V projection, 3d·d + 3d; attention output projection, d·d + d; MLP up, d·4d + 4d; MLP down, 4d·d + d. Altogether that's 12d² + 13d per block.
            - Final layer norm: 2d. Output projection: V·d, or nothing extra if tied.

            For GPT-2 small (V = 50,257, C = 1,024, d = 768, L = 12, tied), this comes to exactly **124,439,808** parameters, the "124M" in its name. For big models the 12·L·d² term dominates: a model with d = 8,192 and L = 80 has about 64 billion parameters in its blocks.

            ## What the symbols stand for
            - **x**: one position's vector in the residual stream, length **d** (the model width, a **hyperparameter**).
            - **μ, σ**: the mean and spread of the entries of x. **ε**: a tiny **constant** such as 1e-5.
            - **γ, β**: learned gain and shift vectors, length d, **parameters**.
            - LayerNorm(x)ᵢ = γᵢ · (xᵢ − μ) / √(σ² + ε) + βᵢ. RMSNorm(x)ᵢ = γᵢ · xᵢ / √(mean of x² + ε).
            - **L**: number of blocks. **V**: vocabulary size. **C**: context length.

            ## Walk through one problem
            Layer-normalize x = [2, 4, 6] with γ = [1, 1, 1], β = [0, 0, 0] and ε = 0.

            1. Mean μ = 4. Distances: [−2, 0, 2]. Squares average to 8/3, so σ = √(8/3) ≈ 1.633.
            2. Divide: [−2, 0, 2] / 1.633 ≈ [−1.225, 0, 1.225].
            3. With γ = 1 and β = 0, that's the output. Any vector, however large or small its entries, comes out with mean 0 and spread 1 before the gain and shift.

            ## Explain it to your team
            "A transformer is one block repeated dozens of times. Each block does attention, so positions share information, then a feed-forward network that processes each position on its own. Each step is normalized first and added back onto a running sum, the residual stream, which keeps very deep networks trainable. Embeddings at the bottom, a projection to vocabulary scores at the top, and that's the whole architecture. GPT-2 small's 124 million parameters fall straight out of that recipe."
            """,
            """
            block (pre-norm):
              x = x + Attention(LayerNorm(x))
              x = x + FeedForward(LayerNorm(x))        FeedForward: d → 4d → GELU → d
            model: embed → L blocks → final norm → unembed (tied to the embedding table) → logits

            LayerNorm([2, 4, 6]) ≈ [−1.225, 0, 1.225]      (mean 0, spread 1, then γ and β)
            GPT-2 small: V = 50,257, C = 1,024, d = 768, L = 12, tied → 124,439,808 parameters
            """,
            LlmCode(
                "Normalize, compose, and count",
                "Implement layer norm and RMSNorm, compose a pre-norm block from given sublayers, and count a GPT's parameters exactly. Plain Python and `math`.",
                [
                    "`layer_norm(x, gamma, beta, eps=1e-5)` and `rms_norm(x, gamma, eps=1e-5)` normalize one vector as defined in the lesson (population variance)",
                    "`block_forward(x, attention, feed_forward, norm1, norm2)` applies the two pre-norm residual steps, where each sublayer and norm is a function from a vector to a vector",
                    "`gpt_parameters(vocab_size, context, d_model, n_layers, tied)` returns the exact count from the lesson's recipe (biases everywhere, 4·d MLP, learned position embeddings, layer norms with gain and shift, a final norm, and an output projection only when `tied` is false)",
                    "GPT-2 small (50,257, 1,024, 768, 12, tied) must come out at 124,439,808",
                ],
                """
                import math


                def layer_norm(x, gamma, beta, eps=1e-5):
                    ...


                def rms_norm(x, gamma, eps=1e-5):
                    ...


                def block_forward(x, attention, feed_forward, norm1, norm2):
                    ...


                def gpt_parameters(vocab_size, context, d_model, n_layers, tied):
                    ...
                """,
                "In `block_forward`, the first residual step is `h = [a + b for a, b in zip(x, attention(norm1(x)))]`, and the second applies `norm2` and `feed_forward` to `h`, not to `x`. For the count, write each piece from the lesson's list as its own line and add them up.",
                ["layer norm", "RMSNorm", "pre-norm residual composition", "GPT-2's exact parameter count"])
        ),
        new(
            "llm-sampling-decoding",
            "Transformers",
            "Decoding: temperature, top-k and top-p",
            "Turning a model's scores into text is a choice: always the top token, or a sample shaped by temperature and cut-offs. Generation is one token at a time, so a cache of keys and values keeps it fast.",
            "At each step the model produces logits for every token. Greedy decoding takes the highest; sampling draws from the softmax. Temperature divides the logits before softmax, sharpening the distribution below 1 and flattening it above 1. Top-k keeps only the k highest-scoring tokens, and top-p keeps the smallest set whose probabilities add up to at least p. Generation feeds each chosen token back in, and a KV cache stores earlier positions' keys and values so each new token costs one position of work.",
            [
                ("llm-next-token-prediction", "decoding is the generation loop from the bigram model, with better choices at each step."),
                ("llm-exp-log-softmax", "temperature works by scaling logits before softmax."),
                ("llm-self-attention", "the KV cache stores attention's keys and values."),
            ],
            """
            ## The idea in plain words
            The model gives a score to every token in its vocabulary. **Decoding** is the rule for picking one, and it changes the character of the output more than most people expect. The same model can be repetitive and bland, or creative and occasionally unhinged, depending only on decoding settings.

            ## Greedy: always the top token
            **Greedy decoding** picks the highest-scoring token every time. It's deterministic: the same prompt always gives the same output. It's good for short factual answers and code completion, but in long text it tends to fall into loops ("I think that I think that I think…") and produces flat, predictable prose, because the single most likely continuation is rarely the most interesting one.

            ## Sampling and temperature
            **Sampling** draws a token at random, in proportion to its softmax probability, as you've done since the probability lesson. **Temperature** reshapes the distribution first: divide every logit by a number T, then apply softmax.

            - **T = 1:** the model's own distribution.
            - **T below 1** (say 0.5): dividing by a small number stretches the gaps between logits, so the top tokens get even more of the probability. Output becomes more focused and predictable.
            - **T above 1** (say 1.5): dividing shrinks the gaps, so unlikely tokens get a bigger share. Output becomes more varied, and more error-prone.
            - **T → 0:** the top token gets all the probability. That's greedy, which is why APIs treat temperature 0 as greedy (you can't actually divide by zero, so code special-cases it).

            Because softmax depends only on differences between logits, temperature works by scaling those differences: a logit 2 higher than another means 7.4 times as likely at T = 1, but 55 times at T = 0.5.

            ## Cutting off the tail: top-k and top-p
            Even at T = 1, thousands of junk tokens each hold a tiny probability, and together they add up: sample 500 tokens and you'll probably hit some of them. Truncation removes the tail before sampling:

            - **Top-k** keeps only the k highest-scoring tokens (say 40), sets the rest to −∞ (probability 0), and samples among those k.
            - **Top-p**, or **nucleus sampling**, keeps the smallest set of top tokens whose probabilities add up to at least p (say 0.9). When the model is confident, that might be 2 tokens; when it's unsure, 200. It adapts, which is why it's usually preferred over a fixed k.

            The usual order is: apply temperature to the logits, apply top-k, apply top-p, then sample. Other controls you'll see in APIs: a **maximum token count**, **stop sequences** that end generation when they appear, a **seed** for reproducibility, and **repetition penalties** that lower the scores of tokens already used.

            ## Generation is sequential: the KV cache
            Generating 500 tokens takes 500 forward passes, each depending on the token chosen before it. That's why responses stream out word by word, and why output tokens are slower (and usually pricier) than input tokens.

            Done naively, each pass recomputes attention over the whole sequence so far. But the keys and values of earlier positions never change: position 10's key is the same whether you're generating token 11 or token 400, thanks to the causal mask. So inference engines save them in a **KV cache**. Each new token computes its own query, key and value, appends its key and value to the cache, and attends over the cached ones. The work per new token then grows with the context length instead of its square. The cost is memory: the cache for a long conversation can be gigabytes, which the inference lesson quantifies.

            Processing the prompt itself is different: all prompt tokens are known up front, so they go through in one parallel pass, called **prefill**. Then decoding proceeds one token at a time.

            ## What the symbols stand for
            - **z**: the vector of logits, one per vocabulary token.
            - **T**: the temperature, a **setting** you choose, usually between 0 and 2.
            - **softmax(z / T)**: the tempered distribution.
            - **k**: how many top tokens top-k keeps, a **setting** such as 40.
            - **p**: the probability mass top-p keeps, a **setting** such as 0.9.
            - **u**: the uniform random number used for the draw, as in the probability lesson.

            ## Walk through one problem
            Logits for four tokens: [2.0, 1.0, 0.5, −1.0]. Temperature 1, then top-p with p = 0.8.

            1. Softmax: exponentials of the logits minus 2 are [1, 0.368, 0.223, 0.050], total 1.641, so the probabilities are about [0.609, 0.224, 0.136, 0.030].
            2. Sorted from highest: 0.609, then 0.609 + 0.224 = 0.833, which reaches 0.8. Keep the first two tokens and drop the other two.
            3. Renormalize the survivors: 0.609 / 0.833 ≈ 0.731 and 0.224 / 0.833 ≈ 0.269.
            4. Sample with u = 0.75: the first token covers [0, 0.731), so u lands on the second token.

            ## Explain it to your team
            "The model scores every token; decoding chooses one. Greedy takes the top token and is repeatable but can loop. Sampling with a temperature adds controlled variety: lower is more focused, higher more creative. Top-k and top-p cut off the long tail of junk tokens. Generation happens one token at a time, and a cache of earlier keys and values keeps each step cheap, at the cost of memory."
            """,
            """
            greedy:        argmax(logits)                      (temperature 0)
            temperature:   softmax(logits / T)                 T < 1 sharper · T > 1 flatter
            top-k:         keep the k highest logits, set the rest to −∞
            top-p:         keep the fewest top tokens whose probabilities reach p
            order:         temperature → top-k → top-p → renormalize → sample with u
            KV cache:      store each position's key and value; a new token attends over the cache
            """,
            LlmCode(
                "A decoding pipeline",
                "Implement greedy decoding, temperature, top-k and top-p, and combine them into one sampling function. Plain Python and `math`. Removed tokens must end with probability exactly 0.",
                [
                    "`apply_temperature(logits, temperature)` returns the logits divided by the temperature (temperature > 0)",
                    "`top_k_filter(logits, k)` keeps the k highest logits and sets the rest to `-math.inf`; on ties at the cut-off, keep the lower token IDs",
                    "`top_p_filter(logits, p)` keeps the smallest set of highest-probability tokens whose softmax probabilities add up to at least `p`, setting the rest to `-math.inf`",
                    "`sample_next(logits, temperature, top_k, top_p, u)` returns the greedy argmax when temperature is 0; otherwise applies temperature, then top-k if `top_k` is not None, then top-p if `top_p` is not None, softmaxes, and picks the first token ID whose cumulative probability exceeds `u`",
                ],
                """
                import math


                def apply_temperature(logits, temperature):
                    ...


                def top_k_filter(logits, k):
                    ...


                def top_p_filter(logits, p):
                    ...


                def sample_next(logits, temperature, top_k, top_p, u):
                    ...
                """,
                "Sort token IDs with `sorted(range(len(logits)), key=lambda i: (-logits[i], i))` to rank them, which also breaks ties toward lower IDs. For top-p, walk that order adding probabilities and stop once the running total reaches p. A stable softmax turns `-inf` logits into probability 0.",
                ["temperature scaling", "top-k", "top-p", "the full pipeline, including greedy"])
        ),
        new(
            "llm-build-tiny-gpt",
            "Transformers",
            "Lab: train a tiny GPT on your machine",
            "Put every piece together in about 100 lines of PyTorch: a byte-level tokenizer, embeddings, causal attention with heads, pre-norm blocks, AdamW with warmup, and sampling. Train it on text you choose and watch it learn to write.",
            "A GPT is an embedding table, a stack of pre-norm transformer blocks with causal multi-head attention, a final norm and a tied output projection, trained with cross-entropy on next-token prediction. A model with a few million parameters trains on a laptop CPU or Apple GPU in tens of minutes and learns spelling, word shapes and the style of its training text, which makes every idea from the course concrete.",
            [
                ("llm-transformer-block", "you'll implement the block exactly as described."),
                ("llm-sampling-decoding", "you'll generate with temperature and top-k."),
                ("llm-pytorch-lab", "the model is written in PyTorch."),
                ("llm-tokenization-bpe", "the lab starts byte-level, then you can swap in your BPE tokenizer."),
            ],
            """
            ## What you'll build
            A small GPT, trained from scratch on a text file you pick: public-domain books from Project Gutenberg, your own notes, the source code of a project you like (check its license). It runs on a laptop CPU, an Apple Silicon GPU (`mps`), or a free cloud notebook GPU. On a laptop expect roughly 20–60 minutes for a decent result, depending on hardware.

            The architecture is everything from this module, at small scale: byte tokens, a learned position embedding, 4 pre-norm blocks with 4 heads each, d_model = 256, and context length 128, for 3,257,856 parameters.

            ## The code
            Save this as `tiny_gpt.py` next to a text file called `input.txt`.

            ```python
            import math, time, torch
            from torch import nn
            from torch.nn import functional as F

            torch.manual_seed(1337)
            device = "cuda" if torch.cuda.is_available() else "mps" if torch.backends.mps.is_available() else "cpu"
            context, d_model, n_heads, n_layers = 128, 256, 4, 4
            batch_size, steps, peak_lr, warmup = 32, 3000, 1e-3, 200

            # Byte-level tokenizer: 256 tokens, nothing is ever unknown.
            data = torch.tensor(list(open("input.txt", "rb").read()), dtype=torch.long)
            split = int(0.9 * len(data))
            train, val = data[:split], data[split:]
            vocab = 256

            def batch(source):
                starts = torch.randint(len(source) - context - 1, (batch_size,))
                x = torch.stack([source[s:s + context] for s in starts])
                y = torch.stack([source[s + 1:s + context + 1] for s in starts])   # targets: shifted by one
                return x.to(device), y.to(device)

            class Block(nn.Module):
                def __init__(self):
                    super().__init__()
                    self.norm1, self.norm2 = nn.LayerNorm(d_model), nn.LayerNorm(d_model)
                    self.qkv = nn.Linear(d_model, 3 * d_model)                   # W_Q, W_K, W_V in one matrix
                    self.proj = nn.Linear(d_model, d_model)                      # W_O
                    self.mlp = nn.Sequential(nn.Linear(d_model, 4 * d_model), nn.GELU(), nn.Linear(4 * d_model, d_model))

                def attention(self, x):
                    B, T, C = x.shape
                    q, k, v = self.qkv(x).split(C, dim=2)
                    # (B, T, C) → (B, heads, T, head size): each head attends separately
                    q, k, v = (t.view(B, T, n_heads, C // n_heads).transpose(1, 2) for t in (q, k, v))
                    out = F.scaled_dot_product_attention(q, k, v, is_causal=True)   # softmax(QKᵀ/√d + mask)V
                    return self.proj(out.transpose(1, 2).reshape(B, T, C))         # merge heads, mix with W_O

                def forward(self, x):
                    x = x + self.attention(self.norm1(x))                        # pre-norm residual step 1
                    return x + self.mlp(self.norm2(x))                           # pre-norm residual step 2

            class GPT(nn.Module):
                def __init__(self):
                    super().__init__()
                    self.tokens = nn.Embedding(vocab, d_model)
                    self.positions = nn.Embedding(context, d_model)
                    self.blocks = nn.Sequential(*[Block() for _ in range(n_layers)])
                    self.norm = nn.LayerNorm(d_model)
                    # Small starting embeddings, so the first logits are near zero and the first loss is a uniform guess.
                    nn.init.normal_(self.tokens.weight, std=0.02)
                    nn.init.normal_(self.positions.weight, std=0.02)

                def forward(self, idx, targets=None):
                    T = idx.shape[1]
                    x = self.tokens(idx) + self.positions(torch.arange(T, device=idx.device))
                    x = self.norm(self.blocks(x))
                    logits = x @ self.tokens.weight.T                            # tied unembedding
                    loss = None if targets is None else F.cross_entropy(logits.view(-1, vocab), targets.view(-1))
                    return logits, loss

                @torch.no_grad()
                def generate(self, idx, new_tokens, temperature=0.8, top_k=40):
                    for _ in range(new_tokens):
                        logits, _ = self(idx[:, -context:])
                        logits = logits[:, -1, :] / temperature
                        cutoff = torch.topk(logits, top_k).values[:, [-1]]
                        logits[logits < cutoff] = -math.inf                      # top-k
                        next_token = torch.multinomial(F.softmax(logits, dim=-1), 1)
                        idx = torch.cat([idx, next_token], dim=1)
                    return idx

            model = GPT().to(device)
            print(f"{sum(p.numel() for p in model.parameters()):,} parameters on {device}")
            optimizer = torch.optim.AdamW(model.parameters(), lr=peak_lr, weight_decay=0.1)

            def lr_at(step):
                if step < warmup:
                    return peak_lr * step / warmup
                progress = (step - warmup) / (steps - warmup)
                return 0.1 * peak_lr + 0.9 * peak_lr * (1 + math.cos(math.pi * progress)) / 2

            start = time.time()
            for step in range(steps + 1):
                if step % 250 == 0:
                    model.eval()
                    with torch.no_grad():
                        val_loss = sum(model(*batch(val))[1].item() for _ in range(20)) / 20
                    print(f"step {step:5d}  val loss {val_loss:.3f}  {time.time() - start:6.0f}s")
                    model.train()
                for group in optimizer.param_groups:
                    group["lr"] = lr_at(step)
                _, loss = model(*batch(train))
                optimizer.zero_grad(set_to_none=True)
                loss.backward()
                torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
                optimizer.step()

            prompt = torch.tensor([list("The ".encode())], device=device)
            print(bytes(model.generate(prompt, 400)[0].tolist()).decode("utf-8", errors="replace"))
            ```

            ## Reading it against the course
            - `batch` makes inputs and targets shifted by one: every position predicts the next byte, the causal-mask efficiency from the attention lesson.
            - `Block.attention` is your self-attention lesson plus heads; `F.scaled_dot_product_attention` computes softmax(QKᵀ/√d + mask)V with a fast kernel.
            - `Block.forward` is the pre-norm block, exactly.
            - `x @ self.tokens.weight.T` is weight tying. The printed parameter count should match your `gpt_parameters` function with V = 256, C = 128, d = 256, L = 4 and tied = True.
            - `generate` is your decoding pipeline: temperature, top-k, then sampling.
            - `lr_at`, AdamW and clipping are your training-toolkit lesson.

            ## What to expect
            A byte-level model starts at a loss near log(256) ≈ 5.5, which is a uniform guess. Within a few hundred steps it learns which bytes are common (loss about 2.5), then spelling and spaces, then common words. By 3,000 steps on a few megabytes of English, validation loss is typically somewhere around 1.5–1.9, and samples look like the source text's style with plausible words and invented ones. It won't make sense, and that gap between "looks like language" and "means something" is what scale and post-training close.

            ## Experiments to run
            1. **Temperature:** generate at 0.5, 0.8 and 1.2 and describe the differences.
            2. **Context:** retrain with context = 32. Validation loss should be worse. Why, in terms of the why-attention lesson?
            3. **Overfitting:** train on a tiny file (50 KB) for 3,000 steps and watch train and validation loss separate.
            4. **Your tokenizer (stretch):** encode `input.txt` with your BPE from the tokenization lesson (500 merges), set vocab = 756, and compare samples. Compare losses per byte, not per token, since a token now covers several bytes.

            ## What the words mean
            - **Checkpoint:** a saved copy of the model's parameters (`torch.save(model.state_dict(), "tiny.pt")`).
            - **Validation loss:** cross-entropy on held-out text, the honest measure of progress.
            - **`model.eval()` / `model.train()`:** switch layers that behave differently in training (like dropout) between modes.
            - **`torch.no_grad()`:** skip building the autograd graph when you don't need gradients, saving memory and time.

            ## Explain it to your team
            "I trained a GPT from scratch: a byte tokenizer, embeddings, four transformer blocks of causal multi-head attention and feed-forward layers, trained with cross-entropy and AdamW. With three million parameters it learns spelling and style in under an hour on a laptop, but not meaning. Frontier models are this same architecture with thousands of times more parameters and data, plus post-training."
            """,
            """
            tokens (bytes) → embedding + position → 4 × [x + Attn(LN(x)); x + MLP(LN(x))] → LN → tied unembedding → logits
            ≈ 3.3 M parameters · context 128 · d_model 256 · 4 heads · 4 layers
            loss: starts ≈ log(256) ≈ 5.5 → ≈ 2.5 (common bytes) → ≈ 1.5–1.9 (words and style)
            """,
            LlmLab(
                "Train your GPT",
                "Train the tiny GPT on a text of your choice and keep the evidence below in your capstone repository. This is milestone 1 of your capstone.",
                [
                    "The training log: parameter count, device, and validation loss every 250 steps",
                    "Your gpt_parameters function's count for this configuration, matching the printed count",
                    "Samples at temperatures 0.5, 0.8 and 1.2, with a sentence on each",
                    "The context-length experiment: validation loss at context 32 vs 128, and your explanation",
                    "A loss-curve plot (train and validation) and three sentences on what the model learned and didn't",
                ],
                "If training is slow on CPU, halve d_model to 128 and steps to 2,000; the lessons still show. If you see `nan` losses, lower peak_lr to 5e-4."),
            LlmLabStamp("PyTorch 2.14", "https://docs.pytorch.org/docs/stable/generated/torch.nn.functional.scaled_dot_product_attention.html")),
    ];
}
