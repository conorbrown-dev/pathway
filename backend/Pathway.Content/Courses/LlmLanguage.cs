namespace Pathway.Content;

static partial class Curriculum
{
    // Module 5 of the AI & LLM course: how text becomes numbers. Unicode and UTF-8 bytes, byte-pair
    // encoding, a bigram language model with perplexity, and embeddings.
    private static LlmLessonSpec[] LlmLanguageSpecs() =>
    [
        new(
            "llm-text-to-numbers",
            "Language as data",
            "Text to numbers: characters, Unicode and bytes",
            "Every character has a number in Unicode, and UTF-8 stores each number as one to four bytes. Starting from bytes means a model never meets a character it can't read.",
            "Unicode gives every character in every writing system a number called a code point. UTF-8 stores each code point as 1 to 4 bytes (numbers from 0 to 255): plain English letters take one byte, accented letters two, most other scripts three, and emoji four. Modern tokenizers start from these bytes, so any text, in any language, can be represented with a starting vocabulary of just 256 symbols.",
            [
                ("llm-data-as-numbers", "a vocabulary maps symbols to numbers, as with categories."),
                ("llm-python-toolkit", "you'll work with strings, lists and dictionaries."),
            ],
            """
            ## The idea in plain words
            A model only sees numbers, so text has to become numbers before an LLM can read it. The first step is agreeing on a number for every character, and that's been solved: **Unicode** is a worldwide standard that gives every character in every writing system a unique number, its **code point**. "A" is 65. "é" is 233. "€" is 8,364. "猫" (cat, in Chinese and Japanese) is 29,483. "😀" is 128,512. There are over 150,000 assigned characters, with room for more than a million.

            In Python, `ord("é")` gives 233, and `chr(233)` gives back "é".

            ## Bytes and UTF-8
            Computers store and send data as **bytes**. A byte is a number from 0 to 255. One byte can't hold 128,512, so code points have to be split across several bytes. **UTF-8** is the standard way to do it, used by almost every web page and file today. Its rules, in words:

            - Code points **0 to 127** (English letters, digits, basic punctuation) take **one byte**, equal to the code point itself. So plain English text in UTF-8 is just one byte per character.
            - Code points **128 to 2,047** (accented Latin letters, Greek, Cyrillic, Hebrew, Arabic) take **two bytes**.
            - Code points **2,048 to 65,535** (most Chinese, Japanese and Korean characters, and many symbols) take **three bytes**.
            - Code points **65,536 and up** (emoji, rare scripts) take **four bytes**.

            How the splitting works: the code point is broken into chunks of 64 values (6 bits each, if you know binary). Each **continuation byte** carries one chunk and is written as 128 plus that chunk, so continuation bytes always fall between 128 and 191. The **first byte** carries what's left, plus a marker that says how many bytes follow: it starts at 192 for a two-byte character, 224 for three, and 240 for four. A program reading UTF-8 can always tell where each character starts, even when it begins reading in the middle of a file.

            Worked out for "é" (233): 233 = 3 × 64 + 41. The last chunk is 41, so the continuation byte is 128 + 41 = 169. What's left is 3, so the first byte is 192 + 3 = 195. "é" is the two bytes [195, 169]. Check it in Python: `list("é".encode("utf-8"))` gives `[195, 169]`.

            ## Why tokenizers start from bytes
            Say you built a vocabulary of characters from your training text. Someday a user types a character that wasn't in it, a rare emoji or a letter from a script you never saw, and the model has no way to represent it. Older systems mapped those to a special "unknown" token and lost the information.

            Byte-level tokenizers avoid that completely. **Any** text is a sequence of bytes, and there are only 256 possible bytes. Start the vocabulary with those 256, and nothing is ever unknown. The next lesson shows how the tokenizer then merges common byte sequences into bigger tokens, so English words don't cost one token per letter.

            One cost to know about: languages whose characters take 2 to 3 bytes start out more expensive in bytes, and if the tokenizer's merges were learned mostly from English, those languages end up needing more tokens for the same meaning. That makes them slower and more expensive to process, and leaves less room in the context window. It's a real fairness issue in multilingual AI.

            ## Characters versus bytes versus tokens
            Three different counts of "length" will matter from now on:

            - **Characters:** what a person sees. `len("héllo")` is 5 in Python.
            - **Bytes:** what's stored. `len("héllo".encode())` is 6, because é takes two.
            - **Tokens:** what a model processes, and what API pricing and context limits count. Usually fewer than bytes, and for English text, roughly one token per four characters.

            Mixing these up causes real bugs: cutting text at a byte position can split a character in half and produce invalid UTF-8.

            ## What the symbols stand for
            - **Code point:** a character's Unicode number. `ord(c)` in Python.
            - **Byte:** a number from 0 to 255, the unit of storage.
            - **UTF-8:** the rule for turning code points into 1 to 4 bytes.
            - **Continuation byte:** a byte from 128 to 191 that carries one 64-value chunk of a code point.
            - **Vocabulary:** the mapping between symbols (characters, bytes or tokens) and ID numbers.

            ## Walk through one problem
            How does UTF-8 store "€" (code point 8,364)?

            1. 8,364 is between 2,048 and 65,535, so it takes three bytes.
            2. Split off the last 64-value chunk: 8,364 = 130 × 64 + 44. Last continuation byte: 128 + 44 = 172.
            3. Split again: 130 = 2 × 64 + 2. Middle continuation byte: 128 + 2 = 130.
            4. What's left is 2. A three-byte first byte starts at 224, so it's 224 + 2 = 226.
            5. "€" is [226, 130, 172]. Python agrees: `list("€".encode())`.

            ## Explain it to your team
            "Every character has a Unicode number, and UTF-8 stores that number as one to four bytes. Tokenizers start from those 256 possible bytes, so a model can represent any text in any language, and nothing is ever unknown. Characters, bytes and tokens are three different lengths, and token count is the one that sets cost and context limits."
            """,
            """
            ord("é") = 233     chr(233) = "é"
            UTF-8 sizes:  0–127 → 1 byte   128–2,047 → 2   2,048–65,535 → 3   65,536+ → 4
            "é" = 233 = 3 × 64 + 41      → [192 + 3, 128 + 41]            = [195, 169]
            "€" = 8,364 = (2 × 64 + 2) × 64 + 44 → [224 + 2, 128 + 2, 128 + 44] = [226, 130, 172]
            len("héllo") = 5 characters      len("héllo".encode()) = 6 bytes
            """,
            LlmCode(
                "Encode text to bytes by hand",
                "Implement UTF-8 encoding yourself, then build the character-level vocabulary a tiny language model could use. Don't call `str.encode` inside `utf8_encode_char`; the grader compares your bytes with Python's.",
                [
                    "`utf8_encode_char(code_point)` returns the list of UTF-8 byte values for one code point, using the size ranges and the 64-value chunks from the lesson",
                    "`utf8_encode(text)` returns the byte list for a whole string by encoding each character's `ord` value",
                    "`char_vocabulary(text)` returns a dict from each distinct character to an ID, numbered in sorted character order",
                    "`encode_chars(text, vocabulary)` and `decode_chars(ids, vocabulary)` convert between text and ID lists, and decoding an encoding gives back the original text",
                ],
                """
                def utf8_encode_char(code_point):
                    ...


                def utf8_encode(text):
                    ...


                def char_vocabulary(text):
                    ...


                def encode_chars(text, vocabulary):
                    ...


                def decode_chars(ids, vocabulary):
                    ...
                """,
                "Peel chunks off the end: `chunk = code_point % 64`, then `code_point //= 64`. Collect continuation bytes `128 + chunk`, then add the first byte (192, 224 or 240 plus what's left) at the front. For decoding, invert the vocabulary: `{i: c for c, i in vocabulary.items()}`.",
                ["one-byte and multi-byte characters", "whole strings, including emoji", "sorted character vocabulary", "encode and decode round trip"])
        ),
        new(
            "llm-tokenization-bpe",
            "Language as data",
            "Tokenization: build a byte-pair encoder",
            "Start from bytes, repeatedly merge the most frequent adjacent pair into a new token, and you get the kind of tokenizer every LLM uses.",
            "Byte-pair encoding (BPE) builds a vocabulary by counting every adjacent pair of tokens in the training text, merging the most frequent pair into a new token, and repeating. Common words become single tokens while rare words split into pieces, so any text can be encoded and common text is short. Encoding new text applies the learned merges in the order they were learned.",
            [("llm-text-to-numbers", "BPE starts from the 256 UTF-8 byte values.")],
            """
            ## The idea in plain words
            A model needs text cut into units, and the choice of unit is a balancing act.

            - **Whole words** make short sequences, but the vocabulary explodes ("run", "runs", "running", "rerun", every name, every typo), and any word not in the vocabulary can't be represented.
            - **Single bytes** never miss anything, but sequences become very long: a 1,000-word essay is about 6,000 bytes. Attention, as you'll see, gets much more expensive as sequences get longer.

            **Subword tokens** sit in between. Frequent words like " the" become one token; rarer words split into frequent pieces, like "tokenization" → "token" + "ization". **Byte-pair encoding** (BPE) is the algorithm that learns which pieces to use, straight from data. GPT models, Llama, Qwen and most other LLMs use a variant of it.

            ## The training algorithm
            1. **Start with bytes.** Encode the training text as UTF-8 bytes. Token IDs 0–255 are the bytes themselves.
            2. **Count pairs.** Count how often each adjacent pair of tokens appears. In "aaabdaaabac", the pair (a, a) appears 4 times.
            3. **Merge the most frequent pair.** Invent a new token ID, starting at 256, meaning "this pair." Replace every occurrence of the pair, scanning left to right without overlapping, with the new ID. Record the merge.
            4. **Repeat** until you've done as many merges as you want. Vocabulary size = 256 + the number of merges.

            Merged tokens can merge again, so after enough merges, " the", " and" and even long common words become single tokens. The result of training is just the ordered list of merges.

            **Ties:** when two pairs are equally frequent, you need a fixed rule, or two runs could produce different tokenizers. In this lesson, the pair that appears **first** in the sequence wins.

            ## Encoding new text
            To tokenize new text, turn it into bytes, then **apply the merges in the order they were learned**: merge 256 everywhere it occurs, then 257, and so on. Order matters, because later merges are built from earlier ones. Decoding goes the other way: expand each merged token back into its two parts, recursively, until only bytes remain, then decode the bytes as UTF-8.

            ## Real tokenizers add a few things
            - **Pre-tokenization:** text is first split into chunks (words, runs of digits, punctuation) with a pattern, and merges never cross chunk boundaries. That's why the space usually attaches to the *start* of a word: " cat" is one token, distinct from "cat" at the start of a line.
            - **Vocabulary size:** typically 32,000 to 260,000 tokens. Bigger vocabularies make sequences shorter but the embedding table larger.
            - **Special tokens:** reserved IDs that never come from ordinary text, marking things like "end of text" or "start of the user's turn." The instruction-tuning lesson uses them.

            ## Tokenization explains some famous failures
            Models see token IDs, not letters. "strawberry" might be the tokens "str", "aw", "berry", so a question like "how many r's are in strawberry?" asks about letters the model never directly saw. Arithmetic suffers when "1234" splits as "123" + "4" one day and "12" + "34" the next. Some tokenizers split digits one by one for exactly this reason. And as the previous lesson noted, languages underrepresented in the merge training data need more tokens for the same meaning.

            ## What the words mean
            - **Token:** a unit of text with an ID: a byte, or a merged sequence of bytes.
            - **Pair count:** how many times two tokens appear next to each other.
            - **Merge:** a rule "replace pair (a, b) with new token n," learned during training.
            - **Vocabulary size:** 256 + number of merges, for a byte-level BPE.
            - **Compression ratio:** bytes per token. Higher means shorter sequences.

            ## Walk through one problem
            Train two merges on "aaabdaaabac". The byte values are a = 97, b = 98, c = 99, d = 100; letters are used below for readability.

            1. Pair counts: (a, a) 4, (a, b) 2, (b, d) 1, (d, a) 1, (b, a) 1, (a, c) 1. Most frequent: (a, a). Merge it as token 256, written Z. Scanning left to right: "aaabdaaabac" → "ZabdZabac". Inside "aaa" only the first two a's merge, because merges don't overlap.
            2. Recount: (Z, a) 2, (a, b) 2, (b, d) 1, (d, Z) 1, (b, a) 1, (a, c) 1. There's a tie at 2. (Z, a) appears first in the sequence, so it wins. Merge it as token 257, written Y: "YbdYbac".
            3. The text went from 11 bytes to 7 tokens. With more merges on real text, English typically lands around 4 bytes per token.

            ## Explain it to your team
            "Tokenizers cut text into subword pieces. Byte-pair encoding learns them from data: start from bytes, repeatedly merge the most common adjacent pair into a new token, and keep the list of merges. Common words become one token and rare words split into pieces, so nothing is unknown and common text stays short. Token boundaries explain several odd model failures, like counting letters, and why some languages cost more."
            """,
            """
            "aaabdaaabac"  bytes: a a a b d a a a b a c          (11 tokens)
            merge 1: (a, a) → 256 (Z)     Z a b d Z a b a c                 (pair counted 4×)
            merge 2: (Z, a) → 257 (Y)     Y b d Y b a c                      (tie with (a, b); (Z, a) appears first)
            encode new text: bytes → apply merge 256, then 257, … in training order
            decode: expand 257 → (256, a) → (a, a, a) … then bytes → UTF-8 text
            """,
            LlmCode(
                "Train a BPE tokenizer",
                "Implement byte-level BPE training, encoding and decoding. Standard library only. New token IDs start at 256, and a tie between equally frequent pairs goes to the pair that appears first in the sequence.",
                [
                    "`get_pair_counts(ids)` returns a dict of adjacent pair `(a, b)` → count, with pairs in order of first appearance",
                    "`merge(ids, pair, new_id)` returns a new list with every non-overlapping left-to-right occurrence of `pair` replaced by `new_id`",
                    "`train_bpe(text, num_merges)` starts from `list(text.encode('utf-8'))` and returns the list of merges as `((a, b), new_id)`, stopping early if no pair occurs at least twice",
                    "`encode(text, merges)` applies the merges in order, and `decode(ids, merges)` expands tokens back to bytes and returns the text; decoding an encoding gives back the original",
                ],
                """
                def get_pair_counts(ids):
                    ...


                def merge(ids, pair, new_id):
                    ...


                def train_bpe(text, num_merges):
                    ...


                def encode(text, merges):
                    ...


                def decode(ids, merges):
                    ...
                """,
                "Python's `max(counts, key=counts.get)` returns the first key among equals, and your dict is in first-appearance order, so it implements the tie rule. In `merge`, walk with an index `i`: if `ids[i:i + 2]` matches the pair, append `new_id` and jump by 2; otherwise append `ids[i]` and move by 1. For `decode`, build a dict from each new ID to its bytes: `vocab[new_id] = vocab[a] + vocab[b]`.",
                ["pair counting", "non-overlapping merges", "training with the tie rule", "encode and decode round trip"])
        ),
        new(
            "llm-next-token-prediction",
            "Language as data",
            "Next-token prediction: a bigram language model",
            "Build the simplest real language model from counts, measure it with cross-entropy and perplexity, and generate text from it, the same three steps used for every LLM.",
            "A language model assigns a probability to every possible next token given the text so far. A bigram model conditions only on the previous token, estimated by counting pairs with a little smoothing so unseen pairs aren't impossible. It's scored by average cross-entropy over held-out text, often reported as perplexity (e raised to that average), and it generates by repeatedly sampling a next token and appending it.",
            [
                ("llm-probability", "a bigram model is a table of conditional distributions."),
                ("llm-loss-functions", "it's scored with cross-entropy."),
                ("llm-tokenization-bpe", "it predicts tokens, which here are IDs from a vocabulary."),
            ],
            """
            ## The idea in plain words
            A **language model** answers one question: **given the text so far, how likely is each possible next token?** That's all. Everything an LLM does, from answering questions to writing code, comes from asking that question over and over and picking a token each time.

            The simplest language model looks only at the **previous token**. It's called a **bigram model** ("bi" for two: the previous token and the next). You built its counting core in the probability lesson. This lesson turns it into a complete model: train it, score it, and generate text with it.

            ## Training: counting with smoothing
            Training a bigram model means counting, for every token, which tokens follow it, then normalizing each row of counts into probabilities. Store it as a table with one row per previous token and one column per next token: row 7, column 12 holds P(next = 12 | previous = 7).

            Raw counts have a problem. Any pair that never appeared in training gets probability **zero**, and a single zero-probability token in test text makes the cross-entropy infinite. The model declared something impossible and it happened. The fix is **smoothing**: add a small constant k to every count before normalizing (**add-k smoothing**; k = 1 is called Laplace smoothing). Unseen pairs get a small but nonzero probability, and seen pairs lose a little. Neural networks avoid the problem naturally, because softmax never outputs exactly zero.

            ## Scoring: cross-entropy and perplexity
            Score the model on **held-out** text, as the generalization lesson insists. For each position, look up the probability the model gave to the token that actually came next, take −log, and average over all positions. That's the **average cross-entropy**, also called the average **negative log-likelihood**, and it's exactly the loss an LLM is trained to minimize.

            Cross-entropy is a log, so it's hard to picture. **Perplexity** turns it into something intuitive: **perplexity = exp(average cross-entropy)**. It's the model's effective number of equally likely choices at each step:

            - A model that guesses uniformly among 50 tokens has perplexity exactly 50.
            - A model with perplexity 5 is, on average, as uncertain as choosing among 5 options.
            - A perfect model has perplexity 1.

            Lower is better. Perplexity only compares fairly between models with the same tokenizer, because different tokenizers cut the same text into different numbers of tokens.

            ## Generating: sample, append, repeat
            To write text, start with a token, then loop:

            1. Look up the distribution for the current token (one row of the table).
            2. **Sample** a next token from it, using a uniform random number and cumulative stretches, as in the probability lesson.
            3. Append it, make it the current token, and repeat.

            This loop, **autoregressive generation**, is exactly how an LLM writes, one token at a time, each one fed back in as input. The sampling lesson covers the tricks (temperature, top-k, top-p) that shape the distribution before sampling.

            ## Why bigrams aren't enough
            Bigram text looks locally plausible and globally meaningless: "the cat sat on the mat the dog ran to the cat sat". It can't remember the subject of a sentence, close a bracket opened ten tokens ago, or stay on topic, because it only ever sees one token back. Fixing that, letting every position draw on all the previous ones, is what the transformer does. The next module builds it.

            ## What the symbols stand for
            - **V**: the vocabulary size, the number of possible tokens. A **constant** for a given tokenizer.
            - **count(a, b)**: how many times token b followed token a in training.
            - **k**: the smoothing constant, a **hyperparameter** such as 0.1 or 1.
            - **P(b | a)** = (count(a, b) + k) / (count of a as a previous token + k·V): the smoothed probability.
            - **NLL** = average of −log P(next | previous) over the N predictions in the text.
            - **Perplexity** = exp(NLL).

            ## Walk through one problem
            Vocabulary of 3 tokens {0, 1, 2}. Training sequence: 0 1 0 1 0 2. Use k = 1.

            1. Pairs: (0,1) twice, (1,0) twice, (0,2) once. Token 0 is a previous token 3 times, token 1 twice, token 2 never.
            2. Row for 0: counts [0, 2, 1] + 1 each = [1, 3, 2], total 6, so P(· | 0) = [1/6, 3/6, 2/6].
            3. Row for 2: counts [0, 0, 0] + 1 = [1, 1, 1], so P(· | 2) = [1/3, 1/3, 1/3]. Smoothing gave a never-seen token a uniform guess instead of a division by zero.
            4. Score the held-out text "0 1": one prediction, P(1 | 0) = 0.5, NLL = −log(0.5) ≈ 0.693, perplexity = exp(0.693) = 2. On this text the model is as unsure as a coin flip.

            ## Explain it to your team
            "A language model gives a probability for every possible next token. The simplest one, a bigram model, just counts which token follows which. We score language models with cross-entropy on held-out text, or perplexity, its exponential, which you can read as 'the effective number of choices per token.' To generate, we sample a token, append it, and repeat. An LLM does exactly this, but conditions on the entire context instead of one token."
            """,
            """
            train:     table[a][b] = (count(a, b) + k) / (count(a, ·) + k × V)
            score:     NLL = mean of −log table[prev][next] over held-out pairs
                       perplexity = exp(NLL)          (uniform over 50 tokens → 50)
            generate:  token = start
                       repeat: u = rng.random(); token = first b where u < cumulative(table[token]); append
            """,
            LlmCode(
                "Train, score, and sample a bigram model",
                "Build a complete bigram language model over token IDs 0 … vocab_size − 1. Standard library only.",
                [
                    "`train_bigram(tokens, vocab_size, k)` returns a table (list of rows) where `table[a][b]` = (count of a followed by b + k) / (count of a as a previous token + k × vocab_size)",
                    "`average_nll(table, tokens)` returns the mean of −log `table[prev][next]` over every adjacent pair in `tokens`",
                    "`perplexity(table, tokens)` returns exp of the average NLL",
                    "`generate(table, start, length, seed)` returns `length` tokens beginning with `start`: for each next token, draw `u = rng.random()` from one `random.Random(seed)` and pick the first token ID whose cumulative probability exceeds `u` (the last ID if none does)",
                ],
                """
                import math
                import random


                def train_bigram(tokens, vocab_size, k):
                    ...


                def average_nll(table, tokens):
                    ...


                def perplexity(table, tokens):
                    ...


                def generate(table, start, length, seed):
                    ...
                """,
                "Count into a `vocab_size` × `vocab_size` grid of zeros first, then build each row with `[(c + k) / (sum(row) + k * vocab_size) for c in row]`. Make the random generator once, before the loop, so the whole sequence comes from one seed.",
                ["smoothed counts", "average negative log-likelihood", "perplexity", "reproducible generation"])
        ),
        new(
            "llm-embeddings",
            "Language as data",
            "Embeddings: a learned vector for every token",
            "An embedding table gives each token ID its own row of numbers, learned by gradient descent like any other weight, so tokens used in similar ways end up with similar vectors.",
            "An embedding is a learned vector for each token, stored as one row of a table with one row per vocabulary entry. Looking up a row is the same as multiplying a one-hot vector by the table, which is why the table can be trained by backpropagation: only the rows of tokens that appeared receive gradient, and a token used twice gets the sum. Because training rewards tokens that play similar roles with similar vectors, cosine similarity between embeddings tracks similarity of meaning and use.",
            [
                ("llm-vectors-dot-product", "you'll compare embeddings with cosine similarity."),
                ("llm-matrices", "the table is a matrix, and lookup equals one-hot × matrix."),
                ("llm-backpropagation", "gradients flow into the rows that were looked up, and accumulate."),
                ("llm-next-token-prediction", "embeddings are the first layer of a neural language model."),
            ],
            """
            ## The idea in plain words
            A token ID like 4,592 is just a label. The number itself means nothing: token 4,592 isn't "bigger" than token 12 in any useful sense. So the first thing every neural language model does is swap each ID for a **vector**, a list of a few hundred to several thousand numbers. That vector is the token's **embedding**.

            The embeddings live in a table, the **embedding matrix**, with one row per token in the vocabulary and one column per dimension. To embed token 4,592, take row 4,592. The values start random and are **learned** during training, by gradient descent, just like every other weight.

            ## Lookup is a matrix multiplication
            Remember one-hot vectors: all zeros except a 1 in one slot. Multiply a one-hot row vector for token i by the embedding matrix, and every row of the matrix is multiplied by zero except row i, which is multiplied by one. The result is exactly row i. So:

            > embedding lookup = one-hot vector × embedding matrix

            Nobody computes it that way, since copying the row is far faster than multiplying by a vector of 50,000 zeros. But it shows that the embedding table is an ordinary linear layer. That's why it trains by backpropagation with no special rules.

            ## How embeddings learn
            During training, the loss's gradient flows back into the embedding table, but only into **the rows that were looked up** in this batch. Every other row's gradient is zero, because those tokens didn't take part. If the same token appears several times in a batch, its row's gradient is the **sum** of the gradients from each appearance, which is the same accumulation rule as in your autograd engine.

            What ends up in the vectors? Training pushes each token's embedding toward whatever helps predict the text around it. Tokens that appear in similar contexts get similar gradients, so they drift toward similar vectors. "cat" and "dog" both appear before "barked" or "sat on the", after "the", "my" and "a"; their embeddings end up close. "cat" and "carburetor" don't share contexts, so they end up far apart. This is the **distributional hypothesis**: a word is characterized by the company it keeps. The classic 2013 model **word2vec** trained embeddings with exactly this objective and nothing else.

            ## Measuring similarity
            Compare two embeddings with **cosine similarity**, from the vectors lesson: near 1 means the two point in nearly the same direction, near 0 means unrelated. "Nearest neighbors" of a token are the tokens whose embeddings have the highest cosine similarity with it. Semantic search, retrieval and recommendation all run on nearest neighbors in an embedding space.

            You may have seen "king − man + woman ≈ queen." It's real but fragile: some relationships do line up as consistent directions in embedding space, but plenty don't, and the famous examples were partly cherry-picked. Treat it as an illustration, not a law.

            ## Embeddings in an LLM
            - The **input embedding** turns each token ID into a vector at the bottom of the network.
            - Inside, each layer rewrites those vectors using context. After a few layers, the vector at the position of "bank" already reflects whether the sentence is about rivers or money. These are **contextual embeddings**.
            - At the top, the final vector is turned back into a score for every token (the logits). Many models reuse the input embedding table for this step, which is called **weight tying**.
            - **Embedding models** used for search are trained separately to produce one vector for a whole sentence or document; the retrieval lesson uses them.

            ## What the symbols stand for
            - **V**: vocabulary size, the number of rows. **d**: embedding dimension, the number of columns. Both **hyperparameters**; GPT-2 small used V = 50,257 and d = 768.
            - **E**: the embedding matrix, shape V × d, a learned **parameter**.
            - **E[i]**: row i, the embedding of token i.
            - **onehot(i) · E = E[i]**: lookup as a matrix product.
            - **∂L/∂E[i]**: the gradient for row i, the sum of the gradients at every position where token i appeared, and zero for tokens that didn't appear.

            ## Walk through one problem
            V = 4 tokens, d = 2. E = [[0.1, 0.2], [0.5, −0.3], [0.0, 0.9], [0.4, 0.4]]. A batch contains tokens [2, 0, 2], and backpropagation hands back gradients [0.1, 0.0], [0.3, 0.3] and [−0.2, 0.5] for the three positions.

            1. Lookup: [E[2], E[0], E[2]] = [[0.0, 0.9], [0.1, 0.2], [0.0, 0.9]].
            2. Gradient for row 2: token 2 appeared at positions 1 and 3, so add their gradients: [0.1 − 0.2, 0.0 + 0.5] = [−0.1, 0.5].
            3. Gradient for row 0: [0.3, 0.3]. Rows 1 and 3: [0, 0], since those tokens weren't used.
            4. With learning rate 0.1, row 2 becomes [0.0 + 0.01, 0.9 − 0.05] = [0.01, 0.85]. Only the rows that were used changed.

            ## Explain it to your team
            "Token IDs are just labels, so the model's first layer swaps each one for a learned vector called an embedding: one row of a big table. It's an ordinary layer, trained by backpropagation, and only the rows for tokens in the batch get updated. Because training rewards tokens that appear in similar contexts with similar vectors, similarity between embeddings tracks similarity of meaning, which is also what powers semantic search."
            """,
            """
            E (V = 4 tokens × d = 2 dimensions):
              token 0 → [0.1,  0.2]     token 1 → [0.5, −0.3]
              token 2 → [0.0,  0.9]     token 3 → [0.4,  0.4]
            lookup [2, 0, 2]         → [[0.0, 0.9], [0.1, 0.2], [0.0, 0.9]]
            one-hot(2) · E           = [0, 0, 1, 0] · E = E[2]
            gradient for row 2       = sum of the gradients at both positions where token 2 appeared
            """,
            LlmCode(
                "An embedding table",
                "Implement embedding lookup, its one-hot equivalent, nearest-neighbor search, and the gradient for the table. Plain Python and `math`.",
                [
                    "`lookup(table, ids)` returns the list of rows for the given token IDs (copies, so changing the result doesn't change the table)",
                    "`one_hot_times_table(token_id, table)` multiplies a one-hot row vector by the table the long way (sum over every row of onehot[r] × row) and returns the result",
                    "`nearest(table, token_id, k)` returns the IDs of the `k` other tokens with the highest cosine similarity to `token_id`, best first (smaller ID wins a tie)",
                    "`embedding_gradient(vocab_size, dim, ids, upstream)` returns a vocab_size × dim gradient table where each row is the sum of the upstream gradients at the positions where that token appeared, and zero elsewhere",
                ],
                """
                import math


                def lookup(table, ids):
                    ...


                def one_hot_times_table(token_id, table):
                    ...


                def nearest(table, token_id, k):
                    ...


                def embedding_gradient(vocab_size, dim, ids, upstream):
                    ...
                """,
                "For `nearest`, compute `(similarity, token)` for every other token and sort with a key of `(-similarity, token)`. For the gradient, start from `[[0.0] * dim for _ in range(vocab_size)]` and add each upstream row into the row for its token, entry by entry.",
                ["lookup copies rows", "one-hot times the table equals lookup", "nearest neighbors by cosine similarity", "gradients accumulate into used rows only"])
        ),
    ];
}
