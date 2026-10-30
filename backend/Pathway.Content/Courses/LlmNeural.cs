namespace Pathway.Content;

static partial class Curriculum
{
    // Module 4 of the AI & LLM course: neurons and layers, backpropagation (the learner builds a
    // small autograd engine), training at scale (minibatches, Adam, schedules, clipping) and a local
    // PyTorch lab that maps each hand-built piece to the library.
    private static LlmLessonSpec[] LlmNeuralSpecs() =>
    [
        new(
            "llm-neurons-layers",
            "Neural networks",
            "Neurons, layers and why nonlinearity matters",
            "Stack linear models with a bend between them and you get a neural network, which can represent patterns no single straight line can, such as XOR.",
            "A neuron is a linear model followed by a nonlinear activation such as ReLU. A layer is many neurons sharing the same input: a matrix-vector product, plus a bias vector, then the activation. Stacking layers without activations collapses into one linear model, so the bend is what gives depth its power: a two-layer network can compute XOR, which no single linear model can.",
            [
                ("llm-matrices", "a layer is a matrix-vector product."),
                ("llm-classification", "a neuron is logistic regression's linear score with a different squashing function."),
            ],
            """
            ## The idea in plain words
            A **neuron** in a neural network is a small, simple thing: take a weighted sum of the inputs, add a bias (exactly the linear model you already know), then pass the result through a fixed bending function called an **activation**. That's all. The name comes from a loose analogy with brain cells, but it's best to think of it as "linear model plus a bend."

            A **layer** is many neurons that all read the same inputs, each with its own weights. Stack each neuron's weights as a row, and the whole layer is one matrix-vector product, plus a bias vector, then the activation applied to every entry. A **network** feeds one layer's outputs into the next layer's inputs.

            - The **input layer** is just your feature vector.
            - The **hidden layers** are the ones in the middle. Their outputs aren't predictions; they're intermediate features the network invents for itself.
            - The **output layer** produces the prediction: a number, a probability, or a score per class.

            Networks of this shape, where every neuron reads every output of the previous layer, are called **multilayer perceptrons (MLPs)** or **feed-forward networks**. Every transformer block in an LLM contains one.

            ## Why the bend matters
            Here's a surprising fact: if you stack linear layers with no activation in between, you gain nothing. A linear function of a linear function is still a linear function. Ten layers of matrix multiplications multiply out to a single matrix. All that depth would be wasted.

            The activation breaks that. The most common one today is **ReLU** ("rectified linear unit"), which is simply: **if the value is negative, output 0; otherwise pass it through unchanged.** It's a bend at zero. Others you'll meet:

            - **tanh** squashes any number into the range −1 to 1, with an S shape.
            - **sigmoid** squashes into 0 to 1, as in logistic regression.
            - **GELU** and **SiLU** are smooth cousins of ReLU, used in most modern LLMs.

            With bends between the layers, each layer can fold and reshape the space, and a network can approximate almost any pattern given enough neurons. This is the **universal approximation** result, in words: a network with one hidden layer and enough neurons can get as close as you like to any reasonable function. "Enough" can be enormous, which is why practice favors depth, many layers, over one gigantic layer.

            ## XOR: the classic proof
            **XOR** ("exclusive or") is true when exactly one of two inputs is 1: (0, 0) → 0, (0, 1) → 1, (1, 0) → 1, (1, 1) → 0. Plot the four points and try to draw one straight line with the 1s on one side and the 0s on the other. You can't: the 1s sit on one diagonal and the 0s on the other. So no single linear model, including logistic regression, can compute XOR.

            Two hidden ReLU neurons fix it:

            - Neuron A computes ReLU(x₁ + x₂). It counts how many inputs are on: 0, 1 or 2.
            - Neuron B computes ReLU(x₁ + x₂ − 1). It's only positive when both are on, where it gives 1.
            - The output is A − 2B. For (0, 0): 0 − 0 = 0. For one input on: 1 − 0 = 1. For both on: 2 − 2 = 0. That's XOR.

            The hidden layer invented a useful feature, "both on," that wasn't in the raw input. That's what hidden layers do, at vast scale, inside an LLM: they compute features like "this token is inside a string literal" or "the subject of this sentence is plural." Nobody designs those features; training finds them. The research field that tries to read them back out is called **interpretability**.

            ## Counting parameters
            A layer that takes **n** inputs and has **m** neurons has an m × n weight matrix plus m biases: **m·n + m** parameters. A network with layer sizes 784 → 128 → 10 (a classic digit recognizer) has 784·128 + 128 = 100,480 parameters in the first layer and 128·10 + 10 = 1,290 in the second, for 101,770 in total. A 7-billion-parameter LLM is the same arithmetic with much bigger numbers.

            ## What the symbols stand for
            - **x**: the input vector to a layer, length n.
            - **W**: the layer's weight matrix, shape m × n (one row per neuron). **b**: its bias vector, length m. Both are learned **parameters**.
            - **z** = Wx + b: the pre-activations, one per neuron.
            - **h** = f(z): the layer's output, where **f** is the activation applied to each entry, such as ReLU(v) = max(0, v).
            - **m, n**: layer sizes, **hyperparameters** you choose.

            ## Walk through one problem
            A layer has W = [[1, −1], [2, 0]], b = [0, −1], ReLU activation, and input x = [3, 4].

            1. First neuron: 1·3 + (−1)·4 + 0 = −1. ReLU gives 0.
            2. Second neuron: 2·3 + 0·4 − 1 = 5. ReLU gives 5.
            3. The layer outputs [0, 5]. The first neuron is "off" for this input: negative pre-activations are cut to zero, which is the bend at work.

            ## Explain it to your team
            "A neural network is layers of simple units. Each one is a weighted sum plus a bias, followed by a bend like ReLU. Without the bend, stacking layers would just give one big linear model; with it, the network can represent almost any pattern, and its hidden layers invent their own useful features. Every transformer layer in an LLM contains one of these networks."
            """,
            """
            layer:   h = ReLU(W x + b)          ReLU(v) = max(0, v)
            W = [[1, −1], [2, 0]], b = [0, −1], x = [3, 4]
            W x + b = [−1, 5]  →  ReLU  →  [0, 5]

            XOR with two hidden neurons:
              A = ReLU(x₁ + x₂)      B = ReLU(x₁ + x₂ − 1)      output = A − 2B
            parameters in a layer with n inputs, m neurons: m·n + m
            """,
            LlmCode(
                "A network that computes XOR",
                "Build the forward pass of a multilayer perceptron, count its parameters, and hand-pick weights that compute XOR. Plain Python and `math`.",
                [
                    "`relu(v)` returns max(0, v); `dense(weights, biases, inputs, activation)` returns the activated outputs of one layer, where activation is `'relu'`, `'tanh'` or `'none'`, and raises `ValueError` for any other name or a shape mismatch",
                    "`mlp_forward(layers, x)` runs `x` through a list of `(weights, biases, activation)` layers in order and returns the final outputs",
                    "`count_parameters(layer_sizes)` returns the total weights plus biases for consecutive layer sizes, such as [784, 128, 10] → 101,770",
                    "`xor_network()` returns a list of layers so that `mlp_forward` gives an output of at least 0.9 for (0, 1) and (1, 0) and at most 0.1 for (0, 0) and (1, 1)",
                ],
                """
                import math


                def relu(v):
                    ...


                def dense(weights, biases, inputs, activation):
                    ...


                def mlp_forward(layers, x):
                    ...


                def count_parameters(layer_sizes):
                    ...


                def xor_network():
                    ...
                """,
                "`dense` is: for each row and bias, compute `sum(w * x for w, x in zip(row, inputs)) + bias`, then apply the activation. For XOR, use the two hidden neurons from the lesson (weights [[1, 1], [1, 1]], biases [0, -1], ReLU) and an output layer with weights [[1, -2]], bias [0] and no activation.",
                ["relu and a dense layer", "forward through several layers", "parameter counting", "a network that computes XOR"])
        ),
        new(
            "llm-backpropagation",
            "Neural networks",
            "Backpropagation: build an autograd engine",
            "Record every arithmetic step as a graph, then walk it backwards applying the chain rule, and you get exact gradients for every number at once. This is how PyTorch works, and you'll build it.",
            "Backpropagation computes the gradient of the loss with respect to every value in a computation in one backward pass. Record each operation and its inputs as a graph during the forward pass. Then, starting from the loss with gradient 1, visit nodes in reverse order; each node multiplies the gradient it received by its local slope and adds the result into its inputs' gradients. Adding (not overwriting) is what handles a value used in more than one place.",
            [
                ("llm-derivatives-chain-rule", "backpropagation is the chain rule applied to a whole graph."),
                ("llm-neurons-layers", "the graph you differentiate is a neural network's forward pass."),
            ],
            """
            ## The idea in plain words
            Gradient descent needs the gradient of the loss with respect to **every** parameter. You could measure each one numerically, nudging one parameter at a time, but that takes two forward passes per parameter: for a billion parameters, two billion passes per step. Impossible.

            **Backpropagation** gets every gradient, exactly, for roughly the cost of one extra pass. The idea:

            1. During the **forward pass**, remember every arithmetic step: what was computed, from which inputs, and with which operation. That record is a **computational graph**.
            2. During the **backward pass**, start at the loss and walk the graph backwards. At each step, use the chain rule to pass the gradient back to that step's inputs.

            Software that does this automatically is called **automatic differentiation**, or **autograd**. It's the heart of PyTorch, JAX and every other deep learning framework. In this lesson you'll write a small one.

            ## Local slopes
            Every operation only needs to know its own **local slope**: how its output changes when each input is nudged, holding the others fixed. These are the derivative rules from the math module:

            - **Addition**, out = a + b: nudging a by some amount nudges out by the same amount, so both local slopes are **1**. Addition passes the gradient through unchanged to both inputs.
            - **Multiplication**, out = a × b: nudging a changes out at rate **b**, and nudging b changes it at rate **a**. Each input's gradient is the incoming gradient times the *other* input.
            - **tanh**, out = tanh(a): the local slope is **1 − out²**. It's near 1 when out is near 0, and near 0 when the neuron is saturated near ±1.
            - **ReLU**: the local slope is **1** if the input was positive, else **0**. Gradient flows through "on" neurons and stops at "off" ones.
            - **Power**, out = aᵏ for a fixed number k: the local slope is **k·aᵏ⁻¹**.
            - **exp**: the local slope is **out** itself.

            ## The backward pass
            Give every value in the graph a **grad** field: the rate at which the final loss changes per tiny nudge of that value. Then:

            1. Set the loss's own grad to **1**. (Nudging the loss by some amount changes the loss by that amount.)
            2. Visit every node **in reverse order of computation**, so a node is visited only after everything that used it.
            3. At each node, for each input: **input.grad += (local slope for that input) × node.grad.**

            That's the chain rule. node.grad is "how much the loss cares about this node," and the local slope is "how much this node cares about that input." Their product is "how much the loss cares about that input, through this path."

            ## Why "+=" and not "="
            A value can feed into more than one later step. In b = a × a, or in a network where one neuron's output goes to every neuron of the next layer, the loss is affected through **several paths**, and the total rate is the **sum** over the paths. So gradients must **accumulate**. If you overwrite instead, you silently lose all but one path, and training quietly goes wrong. This is the most common bug in hand-written backprop, and the exercise grader checks for it.

            It's also why frameworks make you **zero the gradients** before each training step: leftover grads from the previous step would otherwise be added to the new ones.

            ## Getting the order right
            "Reverse order of computation" has a name: reverse **topological order**. Build it with a depth-first walk: visit a node's inputs first (recursively), then add the node to a list. That list runs from inputs to output; reversed, it runs from the loss back to the parameters, which is exactly the order the backward pass needs.

            ## The design you'll build
            A `Value` object wraps one number and remembers how it was made:

            ```python
            class Value:
                def __init__(self, data, children=()):
                    self.data = data            # the number
                    self.grad = 0.0             # d(loss)/d(this), filled in by backward()
                    self._children = children   # the Values this one was computed from
                    self._backward = lambda: None   # pushes self.grad into the children

                def __mul__(self, other):
                    other = other if isinstance(other, Value) else Value(other)
                    out = Value(self.data * other.data, (self, other))
                    def _backward():
                        self.grad += other.data * out.grad
                        other.grad += self.data * out.grad
                    out._backward = _backward
                    return out
            ```

            Each operation creates a new Value and attaches a small function that knows its local slopes. `backward()` on the final Value builds the topological order and calls every node's `_backward` in reverse. Python's `__add__`, `__mul__`, `__radd__` and friends let you write `a * b + 3` and have it build the graph automatically.

            This design is borrowed from Andrej Karpathy's **micrograd**, a tiny teaching engine. PyTorch does the same thing, except its values are whole tensors (arrays of numbers) and its local slopes are written in fast GPU code.

            ## What the symbols stand for
            - **L**: the final output being differentiated, usually the loss.
            - **v.grad**: ∂L/∂v, the rate at which L changes per tiny nudge of v, with everything else in the graph recomputed.
            - **local slope**: ∂out/∂input for a single operation.
            - Accumulation rule: **input.grad += (∂out/∂input) × out.grad**, summed over every operation that used the input.

            ## Walk through one problem
            Compute L = (a × b) + a with a = 2, b = 3.

            1. Forward: c = a × b = 6, then L = c + a = 8.
            2. Start: L.grad = 1.
            3. L = c + a: addition passes the gradient straight through, so c.grad += 1, and a.grad += 1 (a.grad is now 1).
            4. c = a × b: a.grad += b × c.grad = 3 × 1, so a.grad is now 1 + 3 = **4**. And b.grad += a × c.grad = 2.
            5. Check: L = ab + a, so nudging a changes L at rate b + 1 = 4 ✓, and nudging b changes it at rate a = 2 ✓. If step 4 had overwritten a.grad instead of adding to it, you'd get 3, which is wrong.

            ## Explain it to your team
            "Training needs the gradient of the loss for every parameter. Backpropagation gets all of them in one backward sweep. During the forward pass we record every operation; then we walk the record in reverse, and each operation multiplies the incoming gradient by its own local slope and adds the result to its inputs. It's the chain rule, organized so that each step is computed once. PyTorch's autograd is exactly this, on whole arrays and on GPUs."
            """,
            """
            a = Value(2.0); b = Value(3.0)
            c = a * b            # 6.0   remembers (a, b) and its local slopes (b, a)
            L = c + a            # 8.0   remembers (c, a) and its local slopes (1, 1)
            L.backward()         # L.grad = 1, then reverse order: L → c → a, b
            a.grad  # 4.0 = 1 (through L = c + a) + 3 (through c = a × b)   ← accumulated
            b.grad  # 2.0
            """,
            LlmCode(
                "Write a tiny autograd engine",
                "Implement a `Value` class that records operations and computes gradients by backpropagation. Plain Python and `math`. The grader builds expressions with your class and compares your gradients to numerical estimates.",
                [
                    "`Value(data)` has `.data` and `.grad` (starting at 0.0) and supports `+`, `*`, `-` (both binary and unary), and `**` with an int or float exponent, mixing freely with plain numbers on either side (`2 * v`, `v + 1`, `1 - v`)",
                    "`.tanh()`, `.relu()` and `.exp()` return new Values with the right local slopes",
                    "`.backward()` sets this value's grad to 1, orders the graph topologically, and runs every node's local backward step in reverse, so each value's grad is d(this)/d(value)",
                    "Gradients accumulate with `+=`, so a value used more than once (like `a * a` or `a + a`) gets the sum over every path",
                ],
                """
                import math


                class Value:
                    def __init__(self, data, children=()):
                        self.data = float(data)
                        self.grad = 0.0
                        self._children = children
                        self._backward = lambda: None

                    def __add__(self, other):
                        ...

                    def __mul__(self, other):
                        ...

                    def __pow__(self, exponent):
                        ...

                    def __neg__(self):
                        ...

                    def __sub__(self, other):
                        ...

                    def __radd__(self, other):
                        ...

                    def __rmul__(self, other):
                        ...

                    def __rsub__(self, other):
                        ...

                    def tanh(self):
                        ...

                    def relu(self):
                        ...

                    def exp(self):
                        ...

                    def backward(self):
                        ...
                """,
                "Wrap plain numbers with `other if isinstance(other, Value) else Value(other)`. Build subtraction from addition and negation (`self + (-other)`), and negation from multiplication by -1. For `backward`, collect nodes with a recursive depth-first visit (children first, then the node, skipping already-visited nodes), set `self.grad = 1.0`, and call `node._backward()` for each node in reversed order.",
                ["arithmetic builds the right values", "values used twice accumulate", "gradients match numerical estimates", "a small neuron trains with your engine"])
        ),
        new(
            "llm-training-networks",
            "Neural networks",
            "Training networks: minibatches, Adam, schedules and clipping",
            "The practical machinery that makes gradient descent work on big networks: random starts, minibatches, the Adam optimizer, warmup and decay, and gradient clipping.",
            "Real training starts from small random weights (identical weights would stay identical forever), estimates the gradient from a random minibatch rather than the whole dataset, and updates with Adam, which keeps running averages of each parameter's gradient and squared gradient to give every parameter its own sensible step size. The learning rate warms up and then decays, and gradients whose total length is too large are scaled down so one bad batch can't wreck the model.",
            [
                ("llm-gradient-descent", "everything here is a refinement of the basic gradient step."),
                ("llm-backpropagation", "backpropagation supplies the gradients these methods use."),
                ("llm-python-toolkit", "you'll shuffle with a seeded random generator."),
            ],
            """
            ## The idea in plain words
            Plain gradient descent, as you wrote it for a line, computes the gradient on the **entire** dataset and takes one fixed-size step. That breaks down for real networks. Datasets have billions of examples, so one full gradient is too slow; one learning rate rarely suits every parameter; and a single strange batch can produce a huge gradient that throws the model off a cliff. This lesson covers the standard fixes. Every LLM training run uses all of them.

            ## Starting point: small and random
            Why not start every weight at zero, as you did for the line? Because in a network, neurons in the same layer would then compute identical outputs, receive identical gradients and take identical steps, and they'd **stay identical forever**. Ten neurons would behave like one. Starting from **small random** values breaks this symmetry, so each neuron can drift toward a different job.

            "Small" matters too: weights too large make activations explode through the layers; too small and they shrink toward nothing. Standard schemes (called Xavier/Glorot or Kaiming/He initialization) pick a random spread based on how many inputs each neuron has, so signals keep about the same size layer after layer.

            ## Minibatches and stochastic gradient descent
            Instead of the whole dataset, compute the gradient on a small random **minibatch**, say 32 or 512 examples, and step. The minibatch gradient is a noisy but unbiased estimate of the full gradient: on average, it points the same way. This is **stochastic gradient descent** (SGD; "stochastic" means random). It wins because you take many cheap steps instead of a few expensive ones, and the noise even helps escape poor flat spots.

            The standard recipe for each **epoch** (one pass over the training data): shuffle the example order with a seeded generator, cut it into consecutive batches, and take one step per batch. Every example is used exactly once per epoch. The last batch may be smaller.

            LLMs are trained mostly for about one epoch over enormous data, and measure progress in **tokens seen** rather than epochs. A frontier model's batch might hold millions of tokens.

            ## Momentum and Adam
            Plain SGD has two weaknesses. Noisy gradients make it zigzag, and one learning rate is wrong for parameters whose gradients are consistently tiny or consistently huge. **Adam**, the default optimizer for LLMs (in its **AdamW** variant), fixes both by keeping two running averages per parameter:

            1. **First moment, m:** a running average of the gradient. It smooths out the zigzag, like a heavy ball that keeps rolling in a consistent direction. That's **momentum**.
            2. **Second moment, v:** a running average of the gradient **squared**. It measures how big this parameter's gradients typically are, regardless of sign.

            The step is **m divided by the square root of v**. In words: move in the smoothed direction, scaled so that a parameter with typically large gradients takes cautious steps and one with typically small gradients takes relatively bigger steps. Every parameter gets its own effective learning rate.

            Each running average blends a little of the new value into the old one: new m = β₁·m + (1 − β₁)·gradient, with β₁ = 0.9 typically, so m is mostly memory with 10% fresh gradient. Same for v with β₂ = 0.999. Because both start at zero, they're too small for the first few steps; Adam fixes that **bias** by dividing m by (1 − β₁ᵗ) and v by (1 − β₂ᵗ), where t counts steps from 1. Those corrections approach 1 as t grows. A tiny ε (like 1e-8) is added under the division so nothing divides by zero.

            **AdamW** also shrinks every weight slightly toward zero at each step (**weight decay**), a regularizer that discourages relying on any one huge weight.

            ## Learning-rate schedules
            The best learning rate changes during training. The common LLM recipe:

            - **Warmup:** start near zero and increase linearly over the first few thousand steps. Early on, Adam's running averages are unreliable and the random weights produce wild gradients; small steps avoid early blow-ups.
            - **Decay:** after the peak, lower the rate gradually toward a small floor, often following a **cosine** curve: a smooth slope that falls slowly at first, fastest in the middle, and slowly again at the end. Smaller late steps let the model settle into a valley instead of rattling around it.

            ## Gradient clipping
            Occasionally a batch produces an enormous gradient, and one huge step can undo days of training. **Clipping by global norm** treats all the gradients as one long vector, measures its length, and, if the length is over a limit (often 1.0), scales the whole thing down to that length. The direction is preserved; only the size is capped. Batches with normal gradients are untouched.

            ## Watching a run
            Log the training loss, the validation loss, the learning rate and the gradient norm. A healthy run shows a loss that falls quickly and then slowly. Sudden **loss spikes** usually mean a learning rate that's too high or bad data in a batch. A loss of `nan` ("not a number") means something overflowed; check the learning rate and clipping first.

            ## What the symbols stand for
            - **g**: the gradient for one parameter at the current step.
            - **m, v**: Adam's running averages of g and g², one pair per parameter, starting at 0.
            - **β₁, β₂**: how much of the old average to keep, **constants** around 0.9 and 0.999.
            - **t**: the step number, counting from 1.
            - **m̂** = m / (1 − β₁ᵗ), **v̂** = v / (1 − β₂ᵗ): the bias-corrected averages.
            - **η**: the learning rate. **ε**: a tiny constant such as 1e-8.
            - Update: **parameter ← parameter − η · m̂ / (√v̂ + ε)**.

            ## Walk through one step
            One parameter with value 1.0, gradient 0.5, at the first step (t = 1), with η = 0.1.

            1. m = 0.9·0 + 0.1·0.5 = 0.05. v = 0.999·0 + 0.001·0.25 = 0.00025.
            2. Bias correction: m̂ = 0.05 / (1 − 0.9) = 0.5. v̂ = 0.00025 / (1 − 0.999) = 0.25.
            3. Step: 0.5 / (√0.25 + 1e-8) ≈ 1.0, times η = 0.1. The parameter becomes 1.0 − 0.1 = **0.9**.

            Notice that the first step's size was about η no matter how big the gradient was. That's Adam's per-parameter scaling at work.

            ## Explain it to your team
            "Real training starts from small random weights so neurons don't stay identical, and estimates the gradient from random minibatches instead of the whole dataset. The optimizer, almost always AdamW, smooths each parameter's gradient and scales its step by how big that parameter's gradients usually are. The learning rate warms up and then decays, and we clip gradients so one bad batch can't wreck the run. We watch the loss curve and gradient norm to catch problems early."
            """,
            """
            Adam, per parameter, step t = 1, 2, 3 …
              m ← β₁·m + (1 − β₁)·g          v ← β₂·v + (1 − β₂)·g²
              m̂ = m / (1 − β₁ᵗ)              v̂ = v / (1 − β₂ᵗ)
              param ← param − η · m̂ / (√v̂ + ε)

            clip by global norm: if ‖all gradients‖ > max_norm, multiply every gradient by max_norm / ‖all gradients‖
            schedule: linear warmup to the peak, then cosine decay to a floor
            """,
            LlmCode(
                "The training toolkit",
                "Write the optimizer and the batching, scheduling and clipping helpers that a real training loop uses. Plain Python and `math`.",
                [
                    "`minibatches(n, batch_size, seed)` shuffles the indexes 0 … n − 1 with `random.Random(seed)` and returns them cut into consecutive batches (the last may be smaller), so every index appears exactly once",
                    "`adam_step(params, grads, state, learning_rate, beta1=0.9, beta2=0.999, eps=1e-8)` returns new params, where `state` is a dict that starts empty and must hold `t`, `m` and `v` between calls; apply bias correction with t counting from 1",
                    "`clip_by_global_norm(grads, max_norm)` returns the gradients scaled down so their combined length is at most `max_norm`, unchanged if already within it",
                    "`warmup_cosine(step, total_steps, warmup_steps, peak_lr, min_lr)` rises linearly from 0 at step 0 to `peak_lr` at `warmup_steps`, then follows a cosine down to `min_lr` at `total_steps`",
                ],
                """
                import math
                import random


                def minibatches(n, batch_size, seed):
                    ...


                def adam_step(params, grads, state, learning_rate, beta1=0.9, beta2=0.999, eps=1e-8):
                    ...


                def clip_by_global_norm(grads, max_norm):
                    ...


                def warmup_cosine(step, total_steps, warmup_steps, peak_lr, min_lr):
                    ...
                """,
                "In `adam_step`, if `state` is empty, set `state['t'] = 0` and zero lists for `m` and `v`; then increase `t` before computing the corrections. For the cosine part, let progress = (step − warmup) / (total − warmup), then lr = min_lr + (peak_lr − min_lr) × (1 + cos(π × progress)) / 2.",
                ["minibatches cover every example once", "Adam matches a reference and minimizes a bowl", "clipping keeps direction and caps length", "warmup then cosine decay"])
        ),
        new(
            "llm-pytorch-lab",
            "Neural networks",
            "Lab: the same ideas in PyTorch",
            "Install PyTorch on your own machine and rebuild what you wrote by hand: tensors instead of lists, autograd instead of your Value class, and AdamW instead of your adam_step.",
            "PyTorch is the hand-built pieces scaled up: tensors are n-dimensional arrays that live on a CPU or GPU, autograd is your Value graph applied to whole tensors, nn.Linear is your dense layer, and torch.optim.AdamW is your adam_step. Rebuilding a small network in PyTorch and checking its gradients against your own engine proves you understand what the library does.",
            [
                ("llm-backpropagation", "PyTorch's autograd is the engine you built, on tensors."),
                ("llm-training-networks", "the training loop uses minibatches, AdamW and clipping."),
            ],
            """
            ## Why this lab runs on your machine
            The exercises so far run in Pathway's sandbox, which has no GPU and no internet. PyTorch is a large download that runs best on a GPU, so this lab, and every later lab that trains a model, runs on your own computer. You'll record evidence as you go; together, the labs become your capstone portfolio.

            You don't need a gaming GPU for this lab. Everything here runs on any laptop's CPU in seconds.

            ## Install
            1. Make a project folder and a virtual environment, which keeps this project's packages separate from everything else: `python3 -m venv .venv`, then activate it with `source .venv/bin/activate` on macOS/Linux or `.venv\Scripts\activate` on Windows.
            2. Install PyTorch using the selector on pytorch.org, which gives the exact command for your operating system and hardware. On most machines the plain `pip install torch` is right. NVIDIA GPU owners pick the CUDA build that matches their driver.
            3. Check it: `python -c "import torch; print(torch.__version__, torch.cuda.is_available(), torch.backends.mps.is_available())"`. The last two say whether an NVIDIA GPU (CUDA) or an Apple Silicon GPU (MPS) is usable.

            ## Tensors are your lists, in bulk
            A **tensor** is an n-dimensional array of numbers with a **shape** and a **dtype** (number type).

            ```python
            import torch
            x = torch.tensor([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]])   # shape (2, 3), dtype float32
            W = torch.randn(4, 3)                                    # random 4 × 3 matrix
            y = x @ W.T                                              # matrix multiply: (2, 3) @ (3, 4) → (2, 4)
            ```

            `@` is matrix multiplication, `.T` is transpose, and the shape rule from the matrices lesson applies. A dimension of rows-as-examples is called the **batch dimension**: here, two examples go through the same weights at once. Moving work to a GPU is `device = "cuda"` (or `"mps"`), then `x = x.to(device)`.

            ## Autograd is your Value class, on tensors
            Mark tensors whose gradients you want with `requires_grad=True`. Operations on them build a graph, and `.backward()` fills in `.grad`, exactly as in your engine:

            ```python
            a = torch.tensor(2.0, requires_grad=True)
            b = torch.tensor(3.0, requires_grad=True)
            L = a * b + a
            L.backward()
            print(a.grad, b.grad)    # tensor(4.) tensor(2.)  — same as your Value engine
            ```

            Gradients accumulate across `.backward()` calls, just like your `+=`. That's why training loops call `optimizer.zero_grad()` every step.

            ## Layers, losses and optimizers
            - `torch.nn.Linear(n, m)` is a dense layer: an m × n weight matrix and m biases, with Kaiming-style random initialization.
            - `torch.nn.ReLU()`, `torch.nn.Tanh()`: activations.
            - `torch.nn.functional.cross_entropy(logits, targets)`: the stable cross-entropy from logits you wrote in the loss lesson.
            - `torch.optim.AdamW(model.parameters(), lr=1e-3, weight_decay=0.01)`: your adam_step plus weight decay.
            - `torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)`: your clip_by_global_norm.

            ## The training loop
            ```python
            import torch
            from torch import nn

            torch.manual_seed(0)
            X = torch.tensor([[0., 0.], [0., 1.], [1., 0.], [1., 1.]])
            y = torch.tensor([0, 1, 1, 0])                       # XOR as two classes

            model = nn.Sequential(nn.Linear(2, 8), nn.ReLU(), nn.Linear(8, 2))
            optimizer = torch.optim.AdamW(model.parameters(), lr=0.05)
            for step in range(500):
                logits = model(X)                                 # forward pass
                loss = nn.functional.cross_entropy(logits, y)     # stable cross-entropy
                optimizer.zero_grad()                             # clear accumulated grads
                loss.backward()                                   # backpropagation
                nn.utils.clip_grad_norm_(model.parameters(), 1.0)
                optimizer.step()                                  # AdamW update
                if step % 100 == 0:
                    print(step, loss.item())
            print(model(X).argmax(dim=1))                         # tensor([0, 1, 1, 0])
            ```

            Every line maps to something you built: forward pass, loss, zeroing, backward, clipping, the optimizer step.

            ## What the words mean
            - **Tensor:** an n-dimensional array of numbers with a shape, a dtype (`float32`, `bfloat16`, `int64` …) and a device.
            - **Device:** where a tensor lives and its math runs: `cpu`, `cuda` (NVIDIA GPU) or `mps` (Apple Silicon GPU).
            - **`requires_grad`:** asks autograd to track operations on this tensor.
            - **`nn.Module`:** PyTorch's building block for layers and models; `model.parameters()` lists every learnable tensor.
            - **Batch dimension:** the leading dimension holding separate examples processed together.

            ## Lab steps
            1. Install PyTorch and record the version and which devices are available.
            2. Run the `L = a * b + a` example and your own `Value` engine side by side. Confirm the gradients match.
            3. Build a slightly larger expression (a tanh neuron with three inputs) in both and compare all the gradients to four decimal places.
            4. Train the XOR network above. Then change the hidden size to 1 and note what happens, and why, based on the neurons lesson.
            5. Write three sentences on what PyTorch did that your engine didn't: tensors instead of single numbers, GPU kernels, and a library of tested layers and optimizers.

            ## Explain it to your team
            "PyTorch is the pieces we built by hand, scaled up. Tensors are arrays that can live on a GPU, autograd records operations and backpropagates exactly like our Value engine, and the layers, losses and optimizers are the same formulas, tested and fast. Knowing what's underneath means that when training misbehaves, we know where to look."
            """,
            """
            your engine                     PyTorch
            Value(2.0)                      torch.tensor(2.0, requires_grad=True)
            dense(W, b, x, 'relu')          nn.ReLU()(nn.Linear(n, m)(x))
            cross_entropy_from_logits       nn.functional.cross_entropy
            adam_step                       torch.optim.AdamW(...).step()
            clip_by_global_norm             nn.utils.clip_grad_norm_
            L.backward()                    loss.backward()
            """,
            LlmLab(
                "PyTorch on your machine",
                "Install PyTorch locally, confirm its gradients match your own engine, and train a small network. Keep the evidence below in a notes file in your capstone repository.",
                [
                    "The output of the version check: PyTorch version and whether CUDA or MPS is available",
                    "The same expression's gradients from your Value engine and from PyTorch, matching to four decimal places",
                    "The XOR training log (loss every 100 steps) and the final predictions",
                    "What happened with a hidden size of 1, and your explanation",
                    "Three sentences on what PyTorch adds beyond your engine",
                ],
                "If `pip install torch` is slow or fails, use the exact command from the pytorch.org selector for your system. A CPU-only install is fine for this lab."),
            LlmLabStamp("PyTorch 2.14", "https://docs.pytorch.org/docs/stable/index.html")),
    ];
}
