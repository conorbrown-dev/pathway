# expect: Use += so both paths add up
import math


class Value:
    def __init__(self, data, children=()):
        self.data = float(data)
        self.grad = 0.0
        self._children = children
        self._backward = lambda: None

    @staticmethod
    def _wrap(other):
        return other if isinstance(other, Value) else Value(other)

    def __add__(self, other):
        other = Value._wrap(other)
        out = Value(self.data + other.data, (self, other))

        def _backward():
            self.grad = out.grad
            other.grad = out.grad
        out._backward = _backward
        return out

    def __mul__(self, other):
        other = Value._wrap(other)
        out = Value(self.data * other.data, (self, other))

        def _backward():
            self.grad = other.data * out.grad
            other.grad = self.data * out.grad
        out._backward = _backward
        return out

    def __pow__(self, exponent):
        out = Value(self.data ** exponent, (self,))

        def _backward():
            self.grad += exponent * self.data ** (exponent - 1) * out.grad
        out._backward = _backward
        return out

    def __neg__(self):
        return self * -1

    def __sub__(self, other):
        return self + (-Value._wrap(other))

    def __radd__(self, other):
        return self + other

    def __rmul__(self, other):
        return self * other

    def __rsub__(self, other):
        return Value._wrap(other) + (-self)

    def tanh(self):
        t = math.tanh(self.data)
        out = Value(t, (self,))

        def _backward():
            self.grad += (1 - t * t) * out.grad
        out._backward = _backward
        return out

    def relu(self):
        out = Value(self.data if self.data > 0 else 0.0, (self,))

        def _backward():
            self.grad += (1.0 if self.data > 0 else 0.0) * out.grad
        out._backward = _backward
        return out

    def exp(self):
        e = math.exp(self.data)
        out = Value(e, (self,))

        def _backward():
            self.grad += e * out.grad
        out._backward = _backward
        return out

    def backward(self):
        order, seen = [], set()

        def visit(node):
            if id(node) in seen:
                return
            seen.add(id(node))
            for child in node._children:
                visit(child)
            order.append(node)
        visit(self)
        self.grad = 1.0
        for node in reversed(order):
            node._backward()
