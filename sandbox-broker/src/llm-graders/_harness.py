# Shared helpers for the AI & LLM course graders. evaluator.mjs prepends this file to each
# lesson's grader, so the combined script runs standalone under `python3 -I` (which does not put
# the script's directory on sys.path). Learner code is standard-library Python only.
#
# A grader calls load() to run the submission, then uses need()/check()/close() and finally
# passed(). Any failed check prints one plain-words message and exits 1.
import contextlib
import io
import math
import os
import random
import sys

WORKSPACE = os.path.dirname(os.path.abspath(__file__))
SUBMISSION = os.path.join(WORKSPACE, "submission.py")


class GraderFailure(Exception):
    pass


def fail(message):
    raise GraderFailure(message)


def check(condition, message):
    if not condition:
        fail(message)


def close(actual, expected, tolerance=1e-6):
    """True when two numbers agree to within an absolute-or-relative tolerance."""
    try:
        actual = float(actual)
    except (TypeError, ValueError):
        return False
    if math.isnan(actual) or math.isinf(actual):
        return False
    return abs(actual - expected) <= tolerance * max(1.0, abs(expected))


def close_list(actual, expected, tolerance=1e-6):
    try:
        actual = list(actual)
    except TypeError:
        return False
    return len(actual) == len(expected) and all(close(a, e, tolerance) for a, e in zip(actual, expected))


def close_matrix(actual, expected, tolerance=1e-6):
    try:
        rows = [list(row) for row in actual]
    except TypeError:
        return False
    return len(rows) == len(expected) and all(close_list(r, e, tolerance) for r, e in zip(rows, expected))


def show(value, limit=160):
    text = repr(value)
    return text if len(text) <= limit else text[:limit] + "..."


def load():
    """Run the learner's file and return its top-level names. Its prints are captured, not shown."""
    with open(SUBMISSION, encoding="utf-8") as handle:
        source = handle.read()
    namespace = {"__name__": "submission", "__file__": SUBMISSION}
    captured = io.StringIO()
    try:
        code = compile(source, "submission.py", "exec")
    except SyntaxError as error:
        fail(f"Python could not read your code: {error.msg} on line {error.lineno}.")
    try:
        with contextlib.redirect_stdout(captured):
            exec(code, namespace)
    except SystemExit:
        fail("Your code called exit() while loading. Define functions only; the grader calls them.")
    except GraderFailure:
        raise
    except Exception as error:  # noqa: BLE001 - report any learner error plainly
        fail(f"Your code raised {type(error).__name__} while loading: {error}")
    return namespace


def need(namespace, name):
    value = namespace.get(name)
    check(value is not None, f"Define `{name}`. The grader calls it by that exact name.")
    return value


def call(function, *args, **kwargs):
    """Call learner code, turning an unexpected exception into a readable failure."""
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            return function(*args, **kwargs)
    except GraderFailure:
        raise
    except Exception as error:  # noqa: BLE001
        arguments = ", ".join([show(a, 60) for a in args] + [f"{k}={show(v, 60)}" for k, v in kwargs.items()])
        fail(f"{function.__name__}({arguments}) raised {type(error).__name__}: {error}")


def raises(function, exception_types, *args):
    """True when calling the learner function raises one of the expected exception types."""
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            function(*args)
    except exception_types:
        return True
    except Exception:  # noqa: BLE001
        return False
    return False


_passed_tests = []


def test(name):
    """Mark a named group of checks as passed (shown in the output)."""
    _passed_tests.append(name)
    print(f"ok - {name}")


def run(main):
    try:
        main()
    except GraderFailure as failure:
        print(f"Not yet: {failure}")
        sys.exit(1)
    print("PATHWAY_TEST_PASS")
