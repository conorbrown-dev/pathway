# T-SQL front-end and grading harness for the SQL Server course (docs/sqlserver-curriculum.md).
#
# Learner SQL is written in T-SQL, checked against a documented subset of the language, translated
# and run on a locked-down in-memory SQLite database. SQL Server itself cannot run inside the
# sandbox (1 vCPU, 12 s, no network), so this file's job is to be honest about the gap:
#
#   * anything outside the subset is refused with a plain message, never run "close enough";
#   * SQLite-only syntax (LIMIT, ||, TRUE, GLOB, double-quoted strings...) is refused, because it
#     would fail on SQL Server;
#   * SQL Server rules that SQLite does not enforce are enforced here (strict GROUP BY, select
#     aliases in WHERE, ORDER BY in subqueries, string length, integer ranges, case-insensitive
#     text, semicolon-terminated statements, ...).
#
# Everything above the "Grader helpers" banner is the swappable engine: `Session` is the whole
# interface graders use. evaluator.mjs prepends this file to each grader, so the combined script
# runs standalone under `python3 -I` with only the standard library.
import datetime as _dt
import math
import os
import re
import sqlite3
import sys


class TsqlError(Exception):
    """A message in plain words, shown to the learner as feedback."""


def _error(message):
    raise TsqlError(message)


# ---------------------------------------------------------------------------------------------
# Tokens
# ---------------------------------------------------------------------------------------------

class Tok:
    __slots__ = ("kind", "text", "up")

    def __init__(self, kind, text):
        self.kind = kind  # word | br | dq | str | num | var | op | punct
        self.text = text
        self.up = text.upper() if kind == "word" else text

    def __repr__(self):
        return f"Tok({self.kind},{self.text!r})"


_TOKEN_RE = re.compile(
    r"""
    (?P<ws>\s+)
  | (?P<lc>--[^\n]*)
  | (?P<bc>/\*)
  | (?P<str>[Nn]?'(?:[^']|'')*')
  | (?P<br>\[(?:[^\]]|\]\])*\])
  | (?P<dq>"(?:[^"]|"")*")
  | (?P<num>\d+\.\d*(?:[eE][+-]?\d+)?|\.\d+(?:[eE][+-]?\d+)?|\d+(?:[eE][+-]?\d+)?)
  | (?P<var>@@?[A-Za-z_][A-Za-z0-9_]*)
  | (?P<word>\#{0,2}[A-Za-z_][A-Za-z0-9_$\#]*)
  | (?P<op><>|!=|<=|>=|!<|!>|\+=|-=|\*=|/=|%=|\|\||[-+*/%=<>&|^~])
  | (?P<punct>[(),;.])
    """,
    re.X,
)


def tokenize(source):
    tokens = []
    i = 0
    n = len(source)
    while i < n:
        m = _TOKEN_RE.match(source, i)
        if not m:
            _error(f"Incorrect syntax near '{source[i]}'.")
        kind = m.lastgroup
        if kind == "bc":
            depth = 1
            j = m.end()
            while j < n and depth:
                if source.startswith("/*", j):
                    depth += 1
                    j += 2
                elif source.startswith("*/", j):
                    depth -= 1
                    j += 2
                else:
                    j += 1
            if depth:
                _error("Missing end comment mark '*/'.")
            i = j
            continue
        i = m.end()
        if kind in ("ws", "lc"):
            continue
        tokens.append(Tok(kind, m.group()))
    return tokens


def _name(tok):
    if tok.kind == "br":
        return tok.text[1:-1].replace("]]", "]")
    if tok.kind == "dq":
        return tok.text[1:-1].replace('""', '"')
    return tok.text


def _is(tok, *words):
    return tok is not None and tok.kind == "word" and tok.up in words


def _is_ident(tok):
    return tok is not None and (tok.kind in ("br", "dq") or (tok.kind == "word" and tok.up not in KEYWORDS))


def _near(tok):
    return f"Incorrect syntax near '{tok.text if tok is not None else 'end of statement'}'."


def _bracket(name):
    if "]" in name:
        _error("Identifiers containing ']' are not available in the sandbox.")
    return f"[{name}]"


def _close(toks, i):
    """Index of the ')' matching the '(' at toks[i]."""
    depth = 0
    for j in range(i, len(toks)):
        if toks[j].kind == "punct":
            if toks[j].text == "(":
                depth += 1
            elif toks[j].text == ")":
                depth -= 1
                if depth == 0:
                    return j
    _error("Incorrect syntax near '('. A parenthesis is not closed.")


def _split_commas(toks):
    parts, depth, current = [], 0, []
    for t in toks:
        if t.kind == "punct" and t.text == "(":
            depth += 1
        elif t.kind == "punct" and t.text == ")":
            depth -= 1
        if depth == 0 and t.kind == "punct" and t.text == ",":
            parts.append(current)
            current = []
        else:
            current.append(t)
    parts.append(current)
    return parts


def _top_level(toks, word):
    """Indexes of depth-0 keyword `word` in toks."""
    depth, found = 0, []
    for i, t in enumerate(toks):
        if t.kind == "punct" and t.text == "(":
            depth += 1
        elif t.kind == "punct" and t.text == ")":
            depth -= 1
        elif depth == 0 and t.kind == "word" and t.up == word:
            found.append(i)
    return found


# Reserved words that are never identifiers. Contextual words (ROWS, FIRST, ACTION, ...) are
# deliberately absent so they stay usable as column names; the parsers that need them look for them.
KEYWORDS = frozenset(
    """ADD ALL ALTER AND ANY AS ASC BEGIN BETWEEN BY CASE CHECK COMMIT CONSTRAINT CREATE CROSS
    CURRENT_TIMESTAMP DEFAULT DELETE DESC DISTINCT DROP ELSE END EXCEPT EXISTS FOREIGN FROM FULL
    GROUP HAVING IN INDEX INNER INSERT INTERSECT INTO IS JOIN KEY LEFT LIKE NOT NULL ON OR ORDER
    OUTER OVER PRIMARY REFERENCES RIGHT ROLLBACK SELECT SET SOME TABLE THEN TOP TRAN TRANSACTION
    UNION UNIQUE UPDATE VALUES VIEW WHEN WHERE WITH""".split()
)
_FRAME_WORDS = frozenset("ROWS ROW RANGE UNBOUNDED PRECEDING FOLLOWING CURRENT PARTITION".split())
_SQLITE_ONLY = {
    "GLOB": "GLOB", "REGEXP": "REGEXP", "PRAGMA": "PRAGMA", "ATTACH": "ATTACH", "AUTOINCREMENT": "AUTOINCREMENT",
    "ILIKE": "ILIKE", "RETURNING": "RETURNING", "TRUE": "TRUE", "FALSE": "FALSE",
}
_SELECT_CLAUSE_STOPS = ("FROM", "WHERE", "GROUP", "HAVING", "ORDER", "OFFSET")

# ---------------------------------------------------------------------------------------------
# Functions
# ---------------------------------------------------------------------------------------------

# T-SQL name -> (SQLite name, min args, max args). These are built into SQLite with the same meaning.
_NATIVE = {
    "LTRIM": ("ltrim", 1, 1), "RTRIM": ("rtrim", 1, 1),
    "TRIM": ("trim", 1, 1), "ABS": ("abs", 1, 1), "ROUND": ("round", 2, 2),
    "COALESCE": ("coalesce", 2, 99), "NULLIF": ("nullif", 2, 2), "ISNULL": ("ifnull", 2, 2), "IIF": ("iif", 3, 3),
    "COUNT": ("count", 1, 1), "COUNT_BIG": ("count", 1, 1), "SUM": ("sum", 1, 1), "AVG": ("avg", 1, 1),
    "MIN": ("min", 1, 1), "MAX": ("max", 1, 1), "ROW_NUMBER": ("row_number", 0, 0), "RANK": ("rank", 0, 0),
    "DENSE_RANK": ("dense_rank", 0, 0), "NTILE": ("ntile", 1, 1), "LAG": ("lag", 1, 3), "LEAD": ("lead", 1, 3),
    "FIRST_VALUE": ("first_value", 1, 1), "LAST_VALUE": ("last_value", 1, 1),
}
# T-SQL name -> (python-registered SQLite function, min args, max args).
_UDF = {
    "UPPER": ("tsql_upper", 1, 1), "LOWER": ("tsql_lower", 1, 1), "REPLACE": ("tsql_replace", 3, 3), "LEN": ("tsql_len", 1, 1), "LEFT": ("tsql_left", 2, 2), "RIGHT": ("tsql_right", 2, 2),
    "SUBSTRING": ("tsql_substring", 3, 3), "CHARINDEX": ("tsql_charindex", 2, 3), "CONCAT": ("tsql_concat", 2, 99),
    "CONCAT_WS": ("tsql_concat_ws", 3, 99), "REPLICATE": ("tsql_replicate", 2, 2), "REVERSE": ("tsql_reverse", 1, 1),
    "CEILING": ("tsql_ceiling", 1, 1), "FLOOR": ("tsql_floor", 1, 1), "SQRT": ("tsql_sqrt", 1, 1),
    "POWER": ("tsql_power", 2, 2), "SIGN": ("tsql_sign", 1, 1), "GETDATE": ("tsql_getdate", 0, 0),
    "SYSDATETIME": ("tsql_getdate", 0, 0), "GETUTCDATE": ("tsql_getdate", 0, 0), "SYSUTCDATETIME": ("tsql_getdate", 0, 0),
    "DATEADD": ("tsql_dateadd", 3, 3), "DATEDIFF": ("tsql_datediff", 3, 3), "DATEPART": ("tsql_datepart", 2, 2),
    "YEAR": ("tsql_year", 1, 1), "MONTH": ("tsql_month", 1, 1), "DAY": ("tsql_day", 1, 1),
    "EOMONTH": ("tsql_eomonth", 1, 2), "STRING_AGG": ("tsql_string_agg", 2, 2),
}
_STRINGY_FUNCTIONS = frozenset("UPPER LOWER LTRIM RTRIM TRIM SUBSTRING LEFT RIGHT REPLACE CONCAT CONCAT_WS REPLICATE REVERSE STRING_AGG".split())
_AGGREGATES = frozenset("COUNT COUNT_BIG SUM AVG MIN MAX STRING_AGG".split())
_DATEPARTS = {
    "year": "year", "yy": "year", "yyyy": "year", "quarter": "quarter", "qq": "quarter", "q": "quarter",
    "month": "month", "mm": "month", "m": "month", "day": "day", "dd": "day", "d": "day", "week": "week",
    "wk": "week", "ww": "week", "hour": "hour", "hh": "hour", "minute": "minute", "mi": "minute", "n": "minute",
    "second": "second", "ss": "second", "s": "second",
}
_ALLOWED_SQLITE_FUNCTIONS = frozenset(
    {v[0] for v in _NATIVE.values()} | {v[0] for v in _UDF.values()}
    | {"tsql_cast", "tsql_string_agg_ord"}
    # SQLite functions the translator itself emits (CHECK constraints) or SQLite calls for operators.
    | {"length", "like"}
)

# ---------------------------------------------------------------------------------------------
# Value helpers (the Python behind the registered SQLite functions)
# ---------------------------------------------------------------------------------------------

_LAST_UDF_ERROR = [None]


class _RuntimeProblem(Exception):
    pass


def _problem(message):
    raise _RuntimeProblem(message)


def _guard(function):
    def wrapper(*args):
        try:
            return function(*args)
        except _RuntimeProblem as error:
            _LAST_UDF_ERROR[0] = str(error)
            raise
    return wrapper


def _text(value):
    if value is None:
        return None
    if isinstance(value, float):
        return str(int(value)) if value.is_integer() and abs(value) < 1e15 else repr(value)
    return str(value)


_DATE_RE = re.compile(r"^\s*(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2})(?:\.\d+)?)?)?\s*$")


def _parse_date(value):
    if value is None:
        return None, False
    m = _DATE_RE.match(str(value))
    if not m:
        _problem(f"Conversion failed when converting date and/or time from character string: '{value}'.")
    year, month, day, hour, minute, second = m.groups()
    try:
        stamp = _dt.datetime(int(year), int(month), int(day), int(hour or 0), int(minute or 0), int(second or 0))
    except ValueError:
        _problem(f"Conversion failed when converting date and/or time from character string: '{value}'.")
    return stamp, hour is not None


def _format_date(stamp, with_time):
    return stamp.strftime("%Y-%m-%d %H:%M:%S") if with_time else stamp.strftime("%Y-%m-%d")


def _datepart_name(part):
    name = _DATEPARTS.get(str(part).lower())
    if name is None:
        _problem(f"'{part}' is not a recognized datepart option.")
    return name


@_guard
def tsql_dateadd(part, amount, value):
    if value is None or amount is None:
        return None
    stamp, has_time = _parse_date(value)
    unit = _datepart_name(part)
    amount = int(amount)
    if unit in ("year", "quarter", "month"):
        months = amount * {"year": 12, "quarter": 3, "month": 1}[unit]
        index = stamp.year * 12 + (stamp.month - 1) + months
        year, month = divmod(index, 12)
        month += 1
        if not 1 <= year <= 9999:
            _problem("Adding a value to a 'datetime' column caused an overflow.")
        last_day = (_dt.date(year + (month == 12), month % 12 + 1, 1) - _dt.timedelta(days=1)).day
        stamp = stamp.replace(year=year, month=month, day=min(stamp.day, last_day))
    else:
        delta = {"week": _dt.timedelta(weeks=amount), "day": _dt.timedelta(days=amount), "hour": _dt.timedelta(hours=amount),
                 "minute": _dt.timedelta(minutes=amount), "second": _dt.timedelta(seconds=amount)}[unit]
        stamp = stamp + delta
    return _format_date(stamp, has_time or unit in ("hour", "minute", "second"))


@_guard
def tsql_datediff(part, start, end):
    if start is None or end is None:
        return None
    a, _ = _parse_date(start)
    b, _ = _parse_date(end)
    unit = _datepart_name(part)
    if unit == "year":
        return b.year - a.year
    if unit == "quarter":
        return (b.year * 4 + (b.month - 1) // 3) - (a.year * 4 + (a.month - 1) // 3)
    if unit == "month":
        return (b.year - a.year) * 12 + b.month - a.month
    if unit == "week":
        # Weeks are counted as Sunday-to-Saturday boundaries, as SQL Server's default DATEFIRST does.
        sunday_a = a.date() - _dt.timedelta(days=(a.weekday() + 1) % 7)
        sunday_b = b.date() - _dt.timedelta(days=(b.weekday() + 1) % 7)
        return (sunday_b - sunday_a).days // 7
    if unit == "day":
        return (b.date() - a.date()).days
    size = {"hour": 3600, "minute": 60, "second": 1}[unit]
    epoch = _dt.datetime(1970, 1, 1)
    return int((b - epoch).total_seconds() // size) - int((a - epoch).total_seconds() // size)


@_guard
def tsql_datepart(part, value):
    if value is None:
        return None
    stamp, _ = _parse_date(value)
    unit = _datepart_name(part)
    if unit == "quarter":
        return (stamp.month - 1) // 3 + 1
    if unit == "week":
        return int(stamp.strftime("%U")) + 1
    return {"year": stamp.year, "month": stamp.month, "day": stamp.day, "hour": stamp.hour, "minute": stamp.minute, "second": stamp.second}[unit]


@_guard
def tsql_year(value):
    return None if value is None else _parse_date(value)[0].year


@_guard
def tsql_month(value):
    return None if value is None else _parse_date(value)[0].month


@_guard
def tsql_day(value):
    return None if value is None else _parse_date(value)[0].day


@_guard
def tsql_eomonth(value, months=0):
    if value is None:
        return None
    stamp, _ = _parse_date(value)
    index = stamp.year * 12 + (stamp.month - 1) + int(months or 0)
    year, month = divmod(index, 12)
    month += 1
    return (_dt.date(year + (month == 12), month % 12 + 1, 1) - _dt.timedelta(days=1)).strftime("%Y-%m-%d")


def tsql_getdate():
    return _dt.datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")


def tsql_upper(value):
    return None if value is None else str(value).upper()


def tsql_lower(value):
    return None if value is None else str(value).lower()


def tsql_replace(value, old, new):
    """REPLACE matches case-insensitively, as the default SQL Server collation does."""
    if value is None or old is None or new is None:
        return None
    if str(old) == "":
        return str(value)
    return re.sub(re.escape(str(old)), lambda _m: str(new), str(value), flags=re.IGNORECASE)


def tsql_len(value):
    return None if value is None else len(str(value).rstrip(" "))


def tsql_left(value, count):
    if value is None or count is None:
        return None
    if count < 0:
        _problem("Invalid length parameter passed to the LEFT function.")
    return str(value)[: int(count)]


def tsql_right(value, count):
    if value is None or count is None:
        return None
    if count < 0:
        _problem("Invalid length parameter passed to the RIGHT function.")
    text = str(value)
    return text[len(text) - int(count):] if int(count) < len(text) else text


def tsql_substring(value, start, length):
    if value is None or start is None or length is None:
        return None
    if length < 0:
        _problem("Invalid length parameter passed to the substring function.")
    text, start, length = str(value), int(start), int(length)
    begin = start - 1
    if begin < 0:  # positions before the first character use up part of the length, as in T-SQL
        length += begin
        begin = 0
    return text[begin: begin + max(length, 0)]


def tsql_charindex(needle, haystack, start=1):
    if needle is None or haystack is None:
        return None
    start = 1 if start is None or start < 1 else int(start)
    return str(haystack).lower().find(str(needle).lower(), start - 1) + 1


def tsql_concat(*values):
    return "".join("" if v is None else _text(v) for v in values)


def tsql_concat_ws(separator, *values):
    if separator is None:
        return None
    return str(separator).join(_text(v) for v in values if v is not None)


def tsql_replicate(value, count):
    if value is None or count is None:
        return None
    if count < 0:
        return None
    return str(value) * int(count)


def tsql_reverse(value):
    return None if value is None else str(value)[::-1]


def tsql_ceiling(value):
    return None if value is None else (value if isinstance(value, int) else float(math.ceil(value)))


def tsql_floor(value):
    return None if value is None else (value if isinstance(value, int) else float(math.floor(value)))


@_guard
def tsql_sqrt(value):
    if value is None:
        return None
    if value < 0:
        _problem("An invalid floating point operation occurred.")
    return math.sqrt(value)


def tsql_power(base, exponent):
    return None if base is None or exponent is None else float(base) ** exponent


def tsql_sign(value):
    return None if value is None else (value > 0) - (value < 0)


_INT_RANGES = {"tinyint": (0, 255), "smallint": (-32768, 32767), "int": (-2147483648, 2147483647), "bigint": (-(2 ** 63), 2 ** 63 - 1)}


def _round_half_away(value, digits):
    factor = 10 ** digits
    scaled = abs(value) * factor
    rounded = math.floor(scaled + 0.5 + 1e-9 * (1 if scaled < 1e9 else 0))
    return math.copysign(rounded / factor, value)


@_guard
def tsql_cast(value, kind, precision, scale, strict):
    """CAST / CONVERT / TRY_CAST. `kind` is the normalized target type; strict=0 returns NULL on failure."""
    if value is None:
        return None
    try:
        if kind in _INT_RANGES:
            if isinstance(value, str):
                if not re.fullmatch(r"\s*[+-]?\d+\s*", value):
                    _problem(f"Conversion failed when converting the varchar value '{value}' to data type {kind}.")
                number = int(value)
            else:
                number = int(value)  # truncates toward zero, like SQL Server
            low, high = _INT_RANGES[kind]
            if not low <= number <= high:
                _problem(f"Arithmetic overflow error converting expression to data type {kind}.")
            return number
        if kind == "bit":
            if isinstance(value, str):
                if value.strip().lower() in ("true", "1"):
                    return 1
                if value.strip().lower() in ("false", "0"):
                    return 0
                _problem(f"Conversion failed when converting the varchar value '{value}' to data type bit.")
            return 0 if value == 0 else 1
        if kind == "decimal":
            try:
                number = float(value)
            except ValueError:
                _problem(f"Error converting data type varchar to numeric.")
            number = _round_half_away(number, scale)
            if abs(number) >= 10 ** (precision - scale):
                _problem("Arithmetic overflow error converting numeric to data type numeric.")
            return number
        if kind == "float":
            try:
                return float(value)
            except ValueError:
                _problem(f"Error converting data type varchar to float.")
        if kind == "varchar":
            text = _text(value)
            return text if precision <= 0 else text[:precision]
        if kind in ("date", "datetime"):
            stamp, has_time = _parse_date(value)
            return _format_date(stamp, kind == "datetime")
    except _RuntimeProblem:
        if strict:
            raise
        return None
    except (ValueError, OverflowError):
        if strict:
            _problem(f"Conversion failed when converting the value to data type {kind}.")
        return None
    _problem(f"Cannot convert to data type {kind}.")


class _StringAggPlain:
    """STRING_AGG(value, separator): NULLs are skipped; the order is whatever the scan produces."""

    def __init__(self):
        self.items = []

    def step(self, value, separator):
        if value is not None:
            self.items.append((value, separator))

    def finalize(self):
        if not self.items:
            return None
        return str(self.items[0][1]).join(_text(item[0]) for item in self.items)


class _StringAggOrdered:
    """STRING_AGG(...) WITHIN GROUP (ORDER BY key [DESC]): sorted by key, NULL keys first when ascending."""

    def __init__(self):
        self.items = []

    def step(self, value, separator, key, descending):
        if value is not None:
            self.items.append((value, separator, key, descending))

    def finalize(self):
        if not self.items:
            return None
        descending = bool(self.items[0][3])
        ordered = sorted(self.items, key=lambda item: (item[2] is not None, item[2] if item[2] is not None else 0), reverse=descending)
        return str(ordered[0][1]).join(_text(item[0]) for item in ordered)


# ---------------------------------------------------------------------------------------------
# Types
# ---------------------------------------------------------------------------------------------

_CHAR = {"char", "varchar", "nchar", "nvarchar", "text", "ntext"}
_INT = {"tinyint", "smallint", "int", "integer", "bigint", "bit"}
_DEC = {"decimal", "numeric", "money", "smallmoney"}
_FLOAT = {"float", "real"}
_DATE = {"date": "date", "datetime": "datetime", "datetime2": "datetime", "smalldatetime": "datetime", "time": "time", "datetimeoffset": "datetime"}
_BINARY = {"binary", "varbinary"}


def _parse_type(toks, i):
    """Parse `name [(n[,m] | MAX)]` at toks[i]; returns (name, precision, scale, next index)."""
    if i >= len(toks) or toks[i].kind not in ("word", "br"):
        _error(_near(toks[i] if i < len(toks) else None))
    name = _name(toks[i]).lower()
    precision, scale = None, 0
    i += 1
    if i < len(toks) and toks[i].kind == "punct" and toks[i].text == "(":
        end = _close(toks, i)
        inner = [t for t in toks[i + 1:end] if not (t.kind == "punct" and t.text == ",")]
        if len(inner) >= 1:
            if inner[0].kind == "word" and inner[0].up == "MAX":
                precision = 0
            elif inner[0].kind == "num":
                precision = int(float(inner[0].text))
            else:
                _error(_near(inner[0]))
        if len(inner) >= 2 and inner[1].kind == "num":
            scale = int(float(inner[1].text))
        i = end + 1
    return name, precision, scale, i


def _cast_kind(name, precision, scale):
    """The tsql_cast arguments for a target type name."""
    if name in ("int", "integer"):
        return "int", 0, 0
    if name in ("bigint", "smallint", "tinyint", "bit"):
        return name, 0, 0
    if name in _DEC:
        if name in ("money", "smallmoney"):
            return "decimal", 19, 4
        return "decimal", precision or 18, scale
    if name in _FLOAT:
        return "float", 0, 0
    if name in _CHAR:
        return "varchar", (30 if precision is None else precision), 0
    if name in ("date",):
        return "date", 0, 0
    if name in ("datetime", "datetime2", "smalldatetime"):
        return "datetime", 0, 0
    _error(f"Data type '{name}' is not available in the sandbox.")


# ---------------------------------------------------------------------------------------------
# Translator and session
# ---------------------------------------------------------------------------------------------

_STATEMENT_HELP = (
    "The sandbox runs set-based T-SQL: SELECT, INSERT, UPDATE, DELETE, CREATE/ALTER/DROP TABLE, CREATE INDEX, "
    "CREATE VIEW, transactions, DECLARE/SET variables and PRINT."
)
_LOCAL_LAB = "runs on a real SQL Server in the local lab, not in the sandbox."


class ResultSet(list):
    """Rows of one SELECT (each a list); `columns` holds the result column names."""

    columns = ()


class Session:
    """One disposable database. Everything a grader needs is run(), rows(), errors and table metadata."""

    def __init__(self):
        self.db = sqlite3.connect(":memory:", isolation_level=None)
        self.db.execute("PRAGMA foreign_keys = ON")
        self._register()
        self.catalog = {}  # lower table name -> list of {name, kind, nullable, pk, identity}
        self.indexes = {}  # lower index name -> {table, columns, includes, unique, clustered}
        self.vars = {}  # lower name -> [type tuple, value]
        self.errors = []  # statement-level runtime errors (the batch carries on, as in SQL Server)
        self.messages = []  # PRINT output
        self.trancount = 0
        self.xact_abort = False
        self._savepoints = []
        self._in_ddl = False
        self._ticks = [0]
        self.db.set_progress_handler(self._progress, 20000)
        self.db.set_authorizer(self._authorize)

    # -- setup ------------------------------------------------------------------------------

    def _register(self):
        for name, function, arity in [
            ("tsql_upper", tsql_upper, 1), ("tsql_lower", tsql_lower, 1), ("tsql_replace", tsql_replace, 3), ("tsql_len", tsql_len, 1), ("tsql_left", tsql_left, 2), ("tsql_right", tsql_right, 2),
            ("tsql_substring", tsql_substring, 3), ("tsql_concat_ws", tsql_concat_ws, -1), ("tsql_concat", tsql_concat, -1),
            ("tsql_replicate", tsql_replicate, 2), ("tsql_reverse", tsql_reverse, 1), ("tsql_ceiling", tsql_ceiling, 1),
            ("tsql_floor", tsql_floor, 1), ("tsql_sqrt", tsql_sqrt, 1), ("tsql_power", tsql_power, 2), ("tsql_sign", tsql_sign, 1),
            ("tsql_dateadd", tsql_dateadd, 3), ("tsql_datediff", tsql_datediff, 3), ("tsql_datepart", tsql_datepart, 2),
            ("tsql_year", tsql_year, 1), ("tsql_month", tsql_month, 1), ("tsql_day", tsql_day, 1),
            ("tsql_eomonth", tsql_eomonth, -1), ("tsql_cast", tsql_cast, 5), ("tsql_charindex", tsql_charindex, -1),
        ]:
            self.db.create_function(name, arity, function, deterministic=True)
        self.db.create_function("tsql_getdate", 0, tsql_getdate)
        self.db.create_aggregate("tsql_string_agg", 2, _StringAggPlain)
        self.db.create_aggregate("tsql_string_agg_ord", 4, _StringAggOrdered)

    def _progress(self):
        self._ticks[0] += 1
        return 1 if self._ticks[0] > 1500 else 0  # about 30 million VM steps per statement batch

    _DENIED = {24, 25, 7, 5, 29, 30, 28}  # ATTACH DETACH CREATE_TRIGGER CREATE_TEMP_TRIGGER VTABLE ANALYZE

    def _authorize(self, action, arg1, arg2, database, source):
        if action in self._DENIED:
            return sqlite3.SQLITE_DENY
        if action == 19:  # PRAGMA: only SQLite's own row checks while it adds a constrained column
            return sqlite3.SQLITE_OK if self._in_ddl and arg1 in ("quick_check", "foreign_key_check") else sqlite3.SQLITE_DENY
        if action == 31:  # FUNCTION
            # DDL text is generated by the translator, which already checked every function name a
            # learner wrote; SQLite's own ALTER TABLE machinery calls internal functions (glob, substr...).
            return sqlite3.SQLITE_OK if self._in_ddl or arg2 in _ALLOWED_SQLITE_FUNCTIONS else sqlite3.SQLITE_DENY
        return sqlite3.SQLITE_OK

    # -- running ----------------------------------------------------------------------------

    def run(self, script):
        """Run T-SQL text. Returns a list of result sets (one per SELECT).

        Like SQL Server, each batch (the text between GO lines) is compiled completely before any of
        it runs, so a syntax error anywhere stops the whole batch. Compile-time problems raise
        TsqlError. Runtime errors (constraint violations, bad conversions) are recorded in
        self.errors and the batch carries on, unless SET XACT_ABORT ON is in effect."""
        results = []
        for batch in self._batches(script):
            self.vars = {}
            plan = []
            for statement in self._statements(tokenize(batch)):
                plan.extend(self._translate(statement))
            for action in plan:
                if self._execute(action, results):
                    break
        self._ticks[0] = 0
        return results

    def rows(self, script):
        """Rows of the last SELECT in `script`, as lists of values."""
        before = len(self.errors)
        results = self.run(script)
        if len(self.errors) > before:
            _error(self.errors[-1])
        return results[-1] if results else []

    @staticmethod
    def _batches(script):
        batches, current = [], []
        for line in script.splitlines():
            m = re.fullmatch(r"\s*GO\s*(\d*)\s*;?\s*", line, re.I)
            if m:
                if m.group(1):
                    _error("GO with a repeat count is not available in the sandbox.")
                batches.append("\n".join(current))
                current = []
            else:
                current.append(line)
        batches.append("\n".join(current))
        return [b for b in batches if b.strip()]

    @staticmethod
    def _statements(tokens):
        statements, current = [], []
        for t in tokens:
            if t.kind == "punct" and t.text == ";":
                if current:
                    statements.append(current)
                current = []
            else:
                current.append(t)
        if current:
            statements.append(current)
        return statements

    def _bind(self, used):
        return {"v_" + key: self.vars[key][1] for key in used}

    def _execute(self, action, results):
        """Run one planned action. Returns True when the rest of the batch must be skipped."""
        kind = action[0]
        if kind == "none":
            return False
        if kind == "tran":
            return self._transaction(action[1], action[2])
        try:
            if kind in ("select", "dml", "ddl"):
                self._in_ddl = kind == "ddl"
                cursor = self.db.execute(action[1], self._bind(action[2]))
                if kind == "select":
                    result = ResultSet(list(row) for row in cursor.fetchall())
                    result.columns = [d[0] for d in (cursor.description or ())]
                    results.append(result)
            else:  # declare / setvar / print evaluate one expression
                value = None
                if action[2] is not None:
                    value = self.db.execute(action[2], self._bind(action[3])).fetchone()[0]
                if kind == "print":
                    self.messages.append("" if value is None else _text(value))
                else:
                    self.vars[action[1]][1] = value
            self._in_ddl = False
        except sqlite3.Error as error:
            self._in_ddl = False
            message = self._friendly(error)
            if self._is_compile_error(error):
                raise TsqlError(message)
            self.errors.append(message)
            if self.xact_abort:
                self._transaction("rollback", None)
                return True
        return False

    @staticmethod
    def _is_compile_error(error):
        text = str(error)
        if text.startswith("user-defined function raised") or "interrupted" in text:
            return False
        return not isinstance(error, (sqlite3.IntegrityError, sqlite3.DataError))

    def _friendly(self, error):
        text = str(error)
        if text.startswith("user-defined function raised") and _LAST_UDF_ERROR[0]:
            message, _LAST_UDF_ERROR[0] = _LAST_UDF_ERROR[0], None
            return message
        m = re.match(r"no such table: (?:main\.)?(.+)", text)
        if m:
            return f"Invalid object name '{m.group(1)}'."
        m = re.match(r"no such column: (.+)", text)
        if m:
            return f"Invalid column name '{m.group(1).split('.')[-1]}'."
        m = re.match(r"ambiguous column name: (.+)", text)
        if m:
            return f"Ambiguous column name '{m.group(1)}'."
        m = re.match(r"UNIQUE constraint failed: (.+)", text)
        if m:
            return f"Violation of PRIMARY KEY or UNIQUE KEY constraint on {m.group(1)}. Cannot insert duplicate key."
        m = re.match(r"NOT NULL constraint failed: (.+)\.(.+)", text)
        if m:
            return f"Cannot insert the value NULL into column '{m.group(2)}', table '{m.group(1)}'; column does not allow nulls."
        m = re.match(r"CHECK constraint failed: (.+)", text)
        if m:
            name = m.group(1)
            if name.startswith("__len_"):
                return "String or binary data would be truncated."
            if name.startswith("__range_"):
                return "Arithmetic overflow error converting expression to the column's data type."
            if name.startswith("__check_"):
                return "The statement conflicted with a CHECK constraint."
            return f"The statement conflicted with the CHECK constraint '{name}'."
        if "FOREIGN KEY constraint failed" in text:
            return "The statement conflicted with a FOREIGN KEY constraint."
        if text.startswith("cannot store"):
            return "Conversion failed: a value does not fit the column's data type."
        if "interrupted" in text:
            return "The statement ran for too long and was stopped. Check for a query that never ends."
        if "not authorized" in text:
            return "That statement or function is not available in the sandbox."
        if "syntax error" in text:
            near = re.search(r'near "(.*?)"', text)
            hint = " Did the previous statement end with a semicolon?" if near and near.group(1).upper() in ("SELECT", "INSERT", "UPDATE", "DELETE", "WITH", "CREATE") else ""
            return f"Incorrect syntax near '{near.group(1)}'.{hint}" if near else "Incorrect syntax."
        if "may not be used in a transaction" in text or "within a transaction" in text:
            return "This statement is not allowed inside a transaction in the sandbox."
        return f"The statement failed: {text}"

    def _transaction(self, action, name):
        if action == "begin":
            if self.trancount == 0:
                self.db.execute("BEGIN")
            self.trancount += 1
        elif action == "commit":
            if self.trancount == 0:
                self.errors.append("The COMMIT TRANSACTION request has no corresponding BEGIN TRANSACTION.")
                return False
            self.trancount -= 1
            if self.trancount == 0:
                self.db.execute("COMMIT")
        elif action == "rollback":
            if name and name.lower() in self._savepoints:
                self.db.execute(f"ROLLBACK TO [{name}]")
            elif self.trancount == 0:
                self.errors.append("The ROLLBACK TRANSACTION request has no corresponding BEGIN TRANSACTION.")
            else:
                self.db.execute("ROLLBACK")
                self.trancount = 0
                self._savepoints = []
        elif action == "save":
            self._savepoints.append(name.lower())
            self.db.execute(f"SAVEPOINT [{name}]")
        return False

    @property
    def open_transactions(self):
        return self.trancount

    # -- metadata for graders ---------------------------------------------------------------

    def tables(self):
        return sorted(self.catalog)

    def columns(self, table):
        return [c["name"] for c in self.catalog.get(table.lower(), [])]

    def has_table(self, table):
        return table.lower() in self.catalog

    def has_view(self, name):
        return bool(self.db.execute("SELECT 1 FROM sqlite_master WHERE type = 'view' AND name = ? COLLATE NOCASE", (name,)).fetchone())

    def index_info(self, name):
        return self.indexes.get(name.lower())

    def table_indexes(self, table):
        return [dict(name=n, **i) for n, i in self.indexes.items() if i["table"].lower() == table.lower()]

    # -- translation ------------------------------------------------------------------------

    def _translate(self, toks):
        if not toks:
            return []
        first = toks[0]
        word = first.up if first.kind == "word" else None
        translator = _Translator(self)
        if word in ("SELECT", "WITH") or (first.kind == "punct" and first.text == "("):
            return [("select", translator.query(toks), translator.params())]
        handler = {
            "INSERT": translator.insert, "UPDATE": translator.update, "DELETE": translator.delete,
            "CREATE": translator.create, "ALTER": translator.alter, "DROP": translator.drop,
            "BEGIN": translator.begin, "COMMIT": translator.commit, "ROLLBACK": translator.rollback,
            "SAVE": translator.save, "DECLARE": translator.declare, "SET": translator.set_statement,
            "PRINT": translator.print_statement,
        }.get(word)
        if handler is None:
            if word in ("IF", "WHILE", "END", "TRY", "EXEC", "EXECUTE", "RETURN", "THROW", "RAISERROR", "BREAK", "CONTINUE"):
                _error(f"'{first.text}' belongs to procedural T-SQL (control flow, procedures, error handling), which {_LOCAL_LAB}")
            if word in ("USE", "GRANT", "DENY", "REVOKE", "BACKUP", "RESTORE", "TRUNCATE", "MERGE", "EXPLAIN"):
                _error(f"'{first.text}' {_LOCAL_LAB}")
            _error(f"Incorrect syntax near '{first.text}'. {_STATEMENT_HELP}")
        return handler(toks)


class _Translator:
    """Translates one statement. Holds the variable parameters it uses."""

    def __init__(self, session):
        self.s = session
        self.used = set()
        self._checks = 0

    def _check_name(self, pending):
        """Every learner CHECK gets a name: SQLite would otherwise reuse the previous constraint's name."""
        if pending:
            return pending
        self._checks += 1
        return f"CONSTRAINT [__check_{self._checks}] "

    def params(self):
        return set(self.used)

    # -- expressions -------------------------------------------------------------------------

    def rw(self, toks, nested=False, in_over=False):
        """Rewrite a token list (one expression, clause list or query body) into SQLite SQL."""
        if not in_over and any(_is(t, "SELECT") for t in toks):
            self._check_block(toks, nested)
        out = []
        limit = None
        i, n = 0, len(toks)
        while i < n:
            t = toks[i]
            nxt = toks[i + 1] if i + 1 < n else None
            prev = toks[i - 1] if i > 0 else None
            if t.kind == "word":
                up = t.up
                if up in _SQLITE_ONLY and not (nxt is not None and nxt.kind == "punct" and nxt.text == "("):
                    _error(f"'{t.text}' is SQLite syntax, not T-SQL, and would fail on SQL Server.")
                if up == "LIMIT" and prev is not None and not (prev.kind == "punct" and prev.text == ".") and nxt is not None and nxt.kind in ("num", "var"):
                    _error("LIMIT is not T-SQL. Use SELECT TOP (n) or ORDER BY ... OFFSET ... ROWS FETCH NEXT ... ROWS ONLY.")
                if up == "SELECT":
                    out.append("SELECT")
                    j = i + 1
                    while j < n and _is(toks[j], "DISTINCT", "ALL"):
                        out.append(toks[j].up)
                        j += 1
                    if j < n and _is(toks[j], "TOP"):
                        j, limit = self._top(toks, j)
                    i = j
                    continue
                if up == "OFFSET" and (nxt is not None) and not (prev is not None and prev.kind == "punct" and prev.text == "."):
                    if limit is not None:
                        _error("A query cannot use both TOP and OFFSET.")
                    i, limit = self._offset(toks, i)
                    continue
                if up == "INTO":
                    _error("SELECT ... INTO is not available in the sandbox. Use CREATE TABLE and INSERT ... SELECT instead.")
                if nxt is not None and nxt.kind == "punct" and nxt.text == "(" and up not in KEYWORDS and self._is_column_list(toks, i):
                    # `name (a, b)` after AS, or a CTE header `name (a, b) AS (...)`: a column list, not a call.
                    end = _close(toks, i + 1)
                    columns = ", ".join(_bracket(_name(c)) for c in toks[i + 2:end] if not (c.kind == "punct" and c.text == ","))
                    out.append(f"{_bracket(t.text)} ({columns})")
                    i = end + 1
                    continue
                if nxt is not None and nxt.kind == "punct" and nxt.text == "(" and up not in KEYWORDS - {"LEFT", "RIGHT"}:
                    end = _close(toks, i + 1)
                    text, consumed = self._call(toks, i, end, in_over)
                    out.append(text)
                    i = consumed
                    continue
                if up == "OVER" and nxt is not None and nxt.kind == "punct" and nxt.text == "(":
                    end = _close(toks, i + 1)
                    out.append("OVER (" + self.rw(toks[i + 2:end], nested, in_over=True) + ")")
                    i = end + 1
                    continue
                if up in KEYWORDS or (in_over and up in _FRAME_WORDS):
                    if up == "WITH" and nxt is not None and nxt.kind == "punct" and nxt.text == "(":
                        _error("Table hints (WITH (NOLOCK) and friends) are not available in the sandbox.")
                    out.append(up)
                    i += 1
                    continue
            if t.kind in ("word", "br", "dq"):
                i = self._qualified(toks, i, out)
                continue
            if t.kind == "str":
                out.append(self._string(toks, i))
                i += 1
                continue
            if t.kind == "num":
                out.append(t.text)
                i += 1
                continue
            if t.kind == "var":
                out.append(self._variable(t))
                i += 1
                continue
            if t.kind == "op":
                i = self._operator(toks, i, out)
                continue
            if t.kind == "punct":
                if t.text == "(":
                    end = _close(toks, i)
                    out.append("(" + self.rw(toks[i + 1:end], True) + ")")
                    i = end + 1
                    continue
                out.append(t.text)
                i += 1
                continue
            _error(_near(t))
        if limit is not None:
            if any(_top_level(toks, word) for word in ("UNION", "INTERSECT", "EXCEPT")):
                _error("TOP combined with UNION, INTERSECT or EXCEPT is not available in the sandbox; put each TOP query in parentheses.")
            out.append(limit)
        return " ".join(out)

    def query(self, toks):
        return self.rw(toks)

    @staticmethod
    def _is_column_list(toks, i):
        """toks[i] is a name followed by '(': a column list (`AS alias (a, b)` or a CTE header
        `name (a, b) AS (...)`) rather than a function call."""
        if i > 0 and _is(toks[i - 1], "AS"):
            return True
        end = _close(toks, i + 1)
        return end + 2 < len(toks) and _is(toks[end + 1], "AS") and toks[end + 2].kind == "punct" and toks[end + 2].text == "("

    def _string(self, toks, i):
        text = toks[i].text
        if text[0] in "Nn":
            text = text[1:]
        prev = toks[i - 1] if i > 0 else None
        if prev is not None and _is(prev, "LIKE") and "[" in text:
            _error("LIKE patterns with [character classes] are not available in the sandbox; use % and _ only.")
        return text

    def _variable(self, tok):
        if tok.text.startswith("@@"):
            _error(f"{tok.text} is not available in the sandbox.")
        key = tok.text[1:].lower()
        if key not in self.s.vars:
            _error(f'Must declare the scalar variable "{tok.text}".')
        self.used.add(key)
        return ":v_" + key

    def _operator(self, toks, i, out):
        t = toks[i]
        text = t.text
        if text == "||":
            _error("'||' is not T-SQL. Join strings with + or CONCAT().")
        if text == "^":
            _error("The ^ operator is not available in the sandbox.")
        if text in ("+=", "-=", "*=", "/=", "%="):
            _error(f"Incorrect syntax near '{text}'. Compound assignment is only valid in SET.")
        if text in ("!<", "!>"):
            out.append(">=" if text == "!<" else "<=")
        elif text == "+" and (self._stringy(toks, i - 1, True) or self._stringy(toks, i + 1, False)):
            out.append("||")
        else:
            out.append(text)
        return i + 1

    def _stringy(self, toks, index, left):
        """Is the operand ending (left) or starting (right) at toks[index] a string?"""
        if index < 0 or index >= len(toks):
            return False
        t = toks[index]
        if t.kind == "str":
            return True
        if t.kind == "var":
            entry = self.s.vars.get(t.text[1:].lower())
            return bool(entry and entry[0] in _CHAR)
        if t.kind in ("word", "br", "dq") and not (t.kind == "word" and t.up in KEYWORDS):
            nxt = toks[index + 1] if index + 1 < len(toks) else None
            if not left and nxt is not None and nxt.kind == "punct" and nxt.text == "(":
                return t.up in _STRINGY_FUNCTIONS or self._call_is_char_cast(toks, index)
            return self.s_is_char_column(_name(t))
        if left and t.kind == "punct" and t.text == ")":
            depth, j = 0, index
            while j >= 0:
                if toks[j].kind == "punct" and toks[j].text == ")":
                    depth += 1
                elif toks[j].kind == "punct" and toks[j].text == "(":
                    depth -= 1
                    if depth == 0:
                        break
                j -= 1
            if j > 0 and toks[j - 1].kind == "word":
                return toks[j - 1].up in _STRINGY_FUNCTIONS or self._call_is_char_cast(toks, j - 1)
        return False

    def _call_is_char_cast(self, toks, index):
        if toks[index].up not in ("CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT"):
            return False
        end = _close(toks, index + 1)
        inner = toks[index + 2:end]
        if toks[index].up in ("CAST", "TRY_CAST"):
            for k in _top_level(inner, "AS"):
                return k + 1 < len(inner) and inner[k + 1].kind in ("word", "br") and _name(inner[k + 1]).lower() in _CHAR
            return False
        return bool(inner) and inner[0].kind in ("word", "br") and _name(inner[0]).lower() in _CHAR

    def s_is_char_column(self, name):
        kinds = {c["kind"] for cols in self.s.catalog.values() for c in cols if c["name"].lower() == name.lower()}
        return kinds == {"char"}

    def _qualified(self, toks, i, out):
        parts = [toks[i]]
        j = i
        while j + 2 < len(toks) and toks[j + 1].kind == "punct" and toks[j + 1].text == "." and toks[j + 2].kind in ("word", "br", "dq") \
                and not (toks[j + 2].kind == "word" and toks[j + 2].up in KEYWORDS):
            parts.append(toks[j + 2])
            j += 2
        if j + 2 < len(toks) and toks[j + 1].kind == "punct" and toks[j + 1].text == "." and toks[j + 2].kind == "op" and toks[j + 2].text == "*":
            parts.append(toks[j + 2])
            j += 2
        if parts[-1].kind == "op":
            names = [_bracket(_name(p)) for p in parts[:-1]]
            if parts[0].kind != "op" and len(parts) == 3 and _name(parts[0]).lower() == "dbo":
                names = names[1:]
            out.append(".".join(names) + ".*")
            return j + 1
        names = [_name(p) for p in parts]
        lowered = [x.lower() for x in names]
        if lowered[-1].startswith("sqlite_"):
            _error(f"Invalid object name '{names[-1]}'.")
        if lowered[0] in ("sys", "information_schema"):
            _error("System catalog views (sys.*, INFORMATION_SCHEMA.*) are not available in the sandbox; query them in the local lab.")
        if len(names) > 3 or (len(names) == 3 and lowered[0] != "dbo" and False):
            _error("Three- and four-part names are not available in the sandbox.")
        if lowered[0] == "dbo" and len(names) >= 2:
            names = names[1:]
        elif len(names) == 3:
            _error("Three- and four-part names are not available in the sandbox.")
        elif len(names) == 2 and lowered[0] in ("main", "temp"):
            names = names[1:]
        out.append(".".join(_bracket(x) for x in names))
        return j + 1

    # -- function calls ----------------------------------------------------------------------

    def _call(self, toks, i, end, in_over):
        name_tok = toks[i]
        name = name_tok.up
        args = [] if end == i + 2 else _split_commas(toks[i + 2:end])
        consumed = end + 1

        if name in ("CAST", "TRY_CAST"):
            text = self._cast(args, name == "TRY_CAST", name_tok)
        elif name in ("CONVERT", "TRY_CONVERT"):
            text = self._convert(args, name == "TRY_CONVERT", name_tok)
        elif name in ("DATEADD", "DATEDIFF", "DATEPART"):
            self._arity(name, args, *(_UDF[name][1:]))
            first = args[0]
            if len(first) != 1 or first[0].kind != "word" or first[0].text.lower() not in _DATEPARTS:
                _error(f"'{' '.join(t.text for t in first)}' is not a recognized {name.lower()} option.")
            rest = [self.rw(a, True) for a in args[1:]]
            text = f"{_UDF[name][0]}('{_DATEPARTS[first[0].text.lower()]}', {', '.join(rest)})"
        elif name == "STRING_AGG":
            self._arity(name, args, 2, 2)
            value, sep = (self.rw(a, True) for a in args)
            ordered = None
            if consumed + 1 < len(toks) and _is(toks[consumed], "WITHIN") and _is(toks[consumed + 1], "GROUP"):
                if consumed + 2 >= len(toks) or not (toks[consumed + 2].kind == "punct" and toks[consumed + 2].text == "("):
                    _error(_near(toks[consumed + 2] if consumed + 2 < len(toks) else None))
                group_end = _close(toks, consumed + 2)
                body = toks[consumed + 3:group_end]
                if not body or not _is(body[0], "ORDER") or len(body) < 3 or not _is(body[1], "BY"):
                    _error("WITHIN GROUP needs an ORDER BY clause.")
                key_tokens = body[2:]
                descending = bool(key_tokens) and _is(key_tokens[-1], "DESC")
                if key_tokens and _is(key_tokens[-1], "ASC", "DESC"):
                    key_tokens = key_tokens[:-1]
                if any(t.kind == "punct" and t.text == "," for t in key_tokens):
                    _error("WITHIN GROUP (ORDER BY ...) with several columns is not available in the sandbox.")
                ordered = (self.rw(key_tokens, True), 1 if descending else 0)
                consumed = group_end + 1
            text = f"tsql_string_agg_ord({value}, {sep}, {ordered[0]}, {ordered[1]})" if ordered else f"tsql_string_agg({value}, {sep})"
        elif name in _NATIVE:
            target, low, high = _NATIVE[name]
            self._arity(name, args, low, high)
            if name in _AGGREGATES and args and args[0] and args[0][0].kind == "op" and args[0][0].text == "*":
                if name not in ("COUNT", "COUNT_BIG"):
                    _error(f"{name}(*) is not valid. Only COUNT(*) is.")
                text = f"{target}(*)"
            else:
                text = f"{target}({', '.join(self.rw(a, True) for a in args)})"
        elif name in _UDF:
            target, low, high = _UDF[name]
            self._arity(name, args, low, high)
            text = f"{target}({', '.join(self.rw(a, True) for a in args)})"
        else:
            _error(f"'{name_tok.text}' is not a recognized built-in function name.")
        return text, consumed

    @staticmethod
    def _arity(name, args, low, high):
        if len(args) < low or len(args) > high:
            _error(f"The {name.lower()} function requires {low if low == high else f'{low} to {high}'} argument(s).")
        if any(len(a) == 0 for a in args):
            _error(f"Incorrect syntax near ',' in the {name.lower()} function.")

    def _cast(self, args, safe, name_tok):
        if len(args) != 1:
            _error(f"Incorrect syntax near the keyword '{name_tok.text}'.")
        parts = args[0]
        cut = _top_level(parts, "AS")
        if not cut:
            _error(f"Incorrect syntax near the keyword '{name_tok.text}'. Expected AS.")
        name, precision, scale, end = _parse_type(parts, cut[-1] + 1)
        if end != len(parts):
            _error(_near(parts[end]))
        kind, p, s = _cast_kind(name, precision, scale)
        return f"tsql_cast({self.rw(parts[:cut[-1]], True)}, '{kind}', {p}, {s}, {0 if safe else 1})"

    def _convert(self, args, safe, name_tok):
        if len(args) != 2:
            _error("CONVERT with a style argument is not available in the sandbox. Use CAST, or FORMAT in the local lab.")
        name, precision, scale, end = _parse_type(args[0], 0)
        if end != len(args[0]):
            _error(_near(args[0][end]))
        kind, p, s = _cast_kind(name, precision, scale)
        return f"tsql_cast({self.rw(args[1], True)}, '{kind}', {p}, {s}, {0 if safe else 1})"

    # -- TOP / OFFSET ------------------------------------------------------------------------

    def _top(self, toks, j):
        j += 1
        if j >= len(toks):
            _error(_near(None))
        if toks[j].kind == "punct" and toks[j].text == "(":
            end = _close(toks, j)
            count = self.rw(toks[j + 1:end], True)
            j = end + 1
        elif toks[j].kind in ("num", "var"):
            count = self.rw([toks[j]], True)
            j += 1
        else:
            _error(_near(toks[j]))
        if j < len(toks) and _is(toks[j], "PERCENT"):
            _error("TOP ... PERCENT is not available in the sandbox.")
        if j < len(toks) and _is(toks[j], "WITH") and j + 1 < len(toks) and _is(toks[j + 1], "TIES"):
            _error("TOP ... WITH TIES is not available in the sandbox.")
        return j, f"LIMIT {count}"

    def _offset(self, toks, i):
        j = i + 1
        k = j
        depth = 0
        while k < len(toks):
            t = toks[k]
            if t.kind == "punct" and t.text == "(":
                depth += 1
            elif t.kind == "punct" and t.text == ")":
                depth -= 1
            elif depth == 0 and _is(t, "ROWS", "ROW"):
                break
            k += 1
        if k >= len(toks):
            _error("Incorrect syntax near OFFSET. T-SQL requires OFFSET <n> ROWS.")
        offset = self.rw(toks[j:k], True)
        k += 1
        if k < len(toks) and _is(toks[k], "FETCH"):
            if k + 1 >= len(toks) or not _is(toks[k + 1], "NEXT", "FIRST"):
                _error(_near(toks[k + 1] if k + 1 < len(toks) else None))
            m = k + 2
            n = m
            depth = 0
            while n < len(toks):
                t = toks[n]
                if t.kind == "punct" and t.text == "(":
                    depth += 1
                elif t.kind == "punct" and t.text == ")":
                    depth -= 1
                elif depth == 0 and _is(t, "ROWS", "ROW"):
                    break
                n += 1
            if n + 1 >= len(toks) or not _is(toks[n + 1], "ONLY"):
                _error("Incorrect syntax near FETCH. T-SQL requires FETCH NEXT <n> ROWS ONLY.")
            return n + 2, f"LIMIT {self.rw(toks[m:n], True)} OFFSET {offset}"
        return k, f"LIMIT -1 OFFSET {offset}"

    # -- SELECT-block rules that SQLite does not enforce -------------------------------------

    def _blocks(self, toks):
        """Split a depth-0 token list into SELECT blocks at set operators."""
        blocks, current, depth = [], [], 0
        for t in toks:
            if t.kind == "punct" and t.text == "(":
                depth += 1
            elif t.kind == "punct" and t.text == ")":
                depth -= 1
            if depth == 0 and t.kind == "word" and t.up in ("UNION", "INTERSECT", "EXCEPT"):
                blocks.append(current)
                current = []
            else:
                current.append(t)
        blocks.append(current)
        return blocks

    def _check_block(self, toks, nested):
        # Each block is checked on its own; parenthesized subqueries are checked when rw() recurses.
        for block in self._blocks(toks):
            selects = _top_level(block, "SELECT")
            if not selects:
                continue
            if nested:
                self._check_nested_order(block)
            self._check_group_by(block[selects[0]:])
            self._check_alias_use(block[selects[0]:])

    def _check_nested_order(self, block):
        orders = _top_level(block, "ORDER")
        if orders and not _top_level(block, "TOP") and not _top_level(block, "OFFSET") and not any(_is(t, "TOP") for t in block[:6]):
            # Window ORDER BYs sit inside parentheses, so a depth-0 ORDER BY is the query's own.
            _error("The ORDER BY clause is invalid in views, subqueries, derived tables and common table expressions unless TOP or OFFSET is also specified.")

    def _clause_spans(self, block):
        """Map clause keyword -> (start, end) token indexes at depth 0 of a block starting with SELECT."""
        marks = []
        depth = 0
        for i, t in enumerate(block):
            if t.kind == "punct" and t.text == "(":
                depth += 1
            elif t.kind == "punct" and t.text == ")":
                depth -= 1
            elif depth == 0 and t.kind == "word":
                if t.up in ("FROM", "WHERE", "HAVING", "OFFSET"):
                    marks.append((t.up, i))
                elif t.up in ("GROUP", "ORDER") and i + 1 < len(block) and _is(block[i + 1], "BY"):
                    marks.append((t.up, i))
        marks.sort(key=lambda m: m[1])
        spans = {"SELECT": (1, marks[0][1] if marks else len(block))}
        for index, (word, start) in enumerate(marks):
            spans.setdefault(word, (start + (2 if word in ("GROUP", "ORDER") else 1), marks[index + 1][1] if index + 1 < len(marks) else len(block)))
        return spans

    def _select_items(self, block, spans):
        start, end = spans["SELECT"]
        body = block[start:end]
        k = 0
        while k < len(body) and _is(body[k], "DISTINCT", "ALL"):
            k += 1
        if k < len(body) and _is(body[k], "TOP"):
            if k + 1 < len(body) and body[k + 1].kind == "punct" and body[k + 1].text == "(":
                k = _close(body, k + 1) + 1
            else:
                k += 2
        return _split_commas(body[k:])

    @staticmethod
    def _alias_of(item):
        if len(item) >= 3 and _is(item[-2], "AS") and item[-1].kind in ("word", "br", "dq", "str"):
            return _name(item[-1]) if item[-1].kind != "str" else item[-1].text.strip("'"), item[:-2]
        if len(item) >= 2 and _is_ident(item[-1]) and (item[-2].kind in ("str", "num", "br", "dq") or (item[-2].kind == "punct" and item[-2].text == ")")
                                                       or _is_ident(item[-2])) and not (item[-2].kind == "punct" and item[-2].text == "."):
            return _name(item[-1]), item[:-1]
        if len(item) >= 3 and item[1].kind == "op" and item[1].text == "=" and _is_ident(item[0]):
            return _name(item[0]), item[2:]
        return None, item

    def _column_refs(self, toks):
        """Bare column names used in toks outside aggregate calls, window calls and subqueries."""
        refs, i, n = [], 0, len(toks)
        while i < n:
            t = toks[i]
            nxt = toks[i + 1] if i + 1 < n else None
            if t.kind == "punct" and t.text == "(":
                end = _close(toks, i)
                inner = toks[i + 1:end]
                if not (inner and _is(inner[0], "SELECT")):
                    refs.extend(self._column_refs(inner))
                i = end + 1
                continue
            if t.kind == "word" and nxt is not None and nxt.kind == "punct" and nxt.text == "(" and t.up not in KEYWORDS - {"LEFT", "RIGHT"}:
                end = _close(toks, i + 1)
                follows_over = end + 1 < n and _is(toks[end + 1], "OVER")
                if t.up in _AGGREGATES or follows_over:
                    i = end + 1
                    if follows_over and i + 1 < n and toks[i + 1].kind == "punct":
                        i = _close(toks, i + 1) + 1
                    elif i + 2 < n and _is(toks[i], "WITHIN") and _is(toks[i + 1], "GROUP") and toks[i + 2].kind == "punct":
                        i = _close(toks, i + 2) + 1
                    continue
                inner = toks[i + 2:end]
                if t.up in ("DATEADD", "DATEDIFF", "DATEPART"):
                    inner = inner[inner.index(next(x for x in inner if x.kind == "punct" and x.text == ",")) + 1:] if any(x.kind == "punct" and x.text == "," for x in inner) else []
                if t.up in ("CAST", "TRY_CAST"):
                    cut = _top_level(inner, "AS")
                    inner = inner[:cut[-1]] if cut else inner
                if t.up in ("CONVERT", "TRY_CONVERT"):
                    parts = _split_commas(inner)
                    inner = parts[1] if len(parts) > 1 else []
                refs.extend(self._column_refs(inner))
                i = end + 1
                continue
            if t.kind in ("word", "br", "dq") and not (t.kind == "word" and t.up in KEYWORDS):
                j = i
                while j + 2 < n and toks[j + 1].kind == "punct" and toks[j + 1].text == "." and toks[j + 2].kind in ("word", "br", "dq"):
                    j += 2
                refs.append(_name(toks[j]))
                i = j + 1
                continue
            i += 1
        return refs

    @staticmethod
    def _norm(toks):
        out = []
        for t in toks:
            if t.kind in ("word", "br", "dq"):
                out.append(_name(t).lower())
            elif t.kind == "punct" and t.text == ".":
                if out:
                    out.pop()  # drop the qualifier so t.x and x compare equal
            else:
                out.append(t.text.lower())
        return " ".join(out)

    def _check_group_by(self, block):
        spans = self._clause_spans(block)
        items = [self._alias_of(item)[1] for item in self._select_items(block, spans)]
        has_aggregate = any(
            t.kind == "word" and t.up in _AGGREGATES and i + 1 < len(item) and item[i + 1].kind == "punct" and item[i + 1].text == "("
            and not (_close(item, i + 1) + 1 < len(item) and _is(item[_close(item, i + 1) + 1], "OVER"))
            for item in items for i, t in enumerate(item)
        )
        if "GROUP" not in spans and not has_aggregate:
            return
        grouped_exprs, grouped_cols, grouped_any = set(), set(), set()
        if "GROUP" in spans:
            for expr in _split_commas(block[spans["GROUP"][0]:spans["GROUP"][1]]):
                grouped_exprs.add(self._norm(expr))
                refs = self._column_refs(expr)
                grouped_any.update(r.lower() for r in refs)
                if len(expr) and len(refs) == 1 and (len(expr) == 1 or (len(expr) == 3 and expr[1].kind == "punct")):
                    grouped_cols.add(refs[0].lower())
        for item in items:
            if not item or self._norm(item) in grouped_exprs or (item[0].kind == "op" and item[0].text == "*" and len(item) == 1):
                if item and item[0].kind == "op" and item[0].text == "*" and ("GROUP" in spans or has_aggregate):
                    _error("Column '*' is invalid in the select list because it is not contained in either an aggregate function or the GROUP BY clause.")
                continue
            for ref in self._column_refs(item):
                if ref.lower() in grouped_cols or ref.lower() in grouped_any:
                    continue
                _error(f"Column '{ref}' is invalid in the select list because it is not contained in either an aggregate function or the GROUP BY clause.")

    def _check_alias_use(self, block):
        spans = self._clause_spans(block)
        aliases = {}
        for item in self._select_items(block, spans):
            alias, body = self._alias_of(item)
            if alias and body:
                aliases[alias.lower()] = body
        if not aliases:
            return
        known = {c["name"].lower() for cols in self.s.catalog.values() for c in cols}
        for clause in ("WHERE", "GROUP", "HAVING"):
            if clause not in spans:
                continue
            toks = block[spans[clause][0]:spans[clause][1]]
            for ref in self._bare_refs(toks):
                key = ref.lower()
                if key in aliases and key not in known and key not in self._column_names_in_from(block, spans):
                    _error(f"Invalid column name '{ref}'. A column alias defined in the SELECT list cannot be used in {clause if clause != 'GROUP' else 'GROUP BY'}; repeat the expression instead.")

    def _bare_refs(self, toks):
        refs, depth = [], 0
        for i, t in enumerate(toks):
            if t.kind == "punct" and t.text == "(":
                depth += 1
            elif t.kind == "punct" and t.text == ")":
                depth -= 1
            elif depth == 0 and t.kind in ("word", "br", "dq") and not (t.kind == "word" and t.up in KEYWORDS):
                prev = toks[i - 1] if i else None
                nxt = toks[i + 1] if i + 1 < len(toks) else None
                if (prev is not None and prev.kind == "punct" and prev.text == ".") or (nxt is not None and nxt.kind == "punct" and nxt.text in (".", "(")):
                    continue
                refs.append(_name(t))
        return refs

    def _column_names_in_from(self, block, spans):
        """Names defined by derived tables / CTE column lists in this statement (alias checks stay lenient)."""
        names = set()
        for i, t in enumerate(block):
            if t.kind == "word" and t.up == "AS" and i + 1 < len(block) and block[i + 1].kind == "punct" and block[i + 1].text == "(":
                names.update(a.lower() for a in self._inner_aliases(block[i + 2:_close(block, i + 1)]))
        return names

    def _inner_aliases(self, toks):
        names = []
        for select in _top_level(toks, "SELECT")[:1]:
            spans = self._clause_spans(toks[select:])
            for item in self._select_items(toks[select:], spans):
                alias, _ = self._alias_of(item)
                if alias:
                    names.append(alias)
        return names

    # -- statements --------------------------------------------------------------------------

    def _table_name(self, toks, i):
        """Parse a (possibly dbo-qualified) table name at toks[i]; returns (name, next index, temp)."""
        parts = []
        while i < len(toks) and toks[i].kind in ("word", "br", "dq") and not (toks[i].kind == "word" and toks[i].up in KEYWORDS):
            parts.append(_name(toks[i]))
            i += 1
            if i < len(toks) and toks[i].kind == "punct" and toks[i].text == ".":
                i += 1
            else:
                break
        if not parts:
            _error(_near(toks[i] if i < len(toks) else None))
        if len(parts) >= 2 and parts[0].lower() == "dbo" and len(parts) == 2:
            parts = parts[1:]
        elif len(parts) > 1:
            _error("Only the dbo schema is available in the sandbox.")
        return parts[0], i

    def _reject_clauses(self, toks, what, words):
        for word in words:
            if _top_level(toks, word):
                if word == "OUTPUT":
                    _error(f"The OUTPUT clause on {what} {_LOCAL_LAB}")
                _error(f"{what} ... {word} {_LOCAL_LAB}")

    def insert(self, toks):
        i = 1
        if i < len(toks) and _is(toks[i], "INTO"):
            i += 1
        if i < len(toks) and _is(toks[i], "TOP"):
            _error("INSERT TOP is not available in the sandbox.")
        name, i = self._table_name(toks, i)
        self._reject_clauses(toks, "INSERT", ["OUTPUT"])
        columns = ""
        if i < len(toks) and toks[i].kind == "punct" and toks[i].text == "(":
            end = _close(toks, i)
            columns = " (" + ", ".join(_bracket(_name(t)) for t in toks[i + 1:end] if not (t.kind == "punct" and t.text == ",")) + ")"
            i = end + 1
        rest = toks[i:]
        if not rest:
            _error(_near(None))
        if _is(rest[0], "DEFAULT") and len(rest) == 2 and _is(rest[1], "VALUES"):
            body = "DEFAULT VALUES"
        elif _is(rest[0], "VALUES"):
            rows = rest[1:]
            parts = _split_commas(rows)
            rendered = []
            for part in parts:
                if len(part) < 2 or not (part[0].kind == "punct" and part[0].text == "(" and _close(part, 0) == len(part) - 1):
                    _error(_near(part[0] if part else None))
                rendered.append("(" + self.rw(part[1:-1], True) + ")")
            body = "VALUES " + ", ".join(rendered)
        elif _is(rest[0], "SELECT", "WITH"):
            body = self.rw(rest)
        else:
            _error(_near(rest[0]))
        return [("dml", f"INSERT INTO {_bracket(name)}{columns} {body}", self.params())]

    def _assignments(self, toks, table):
        out = []
        for part in _split_commas(toks):
            cut = None
            for k, t in enumerate(part):
                if t.kind == "op" and t.text in ("=", "+=", "-=", "*=", "/=", "%="):
                    cut = k
                    break
            if cut is None or cut == 0:
                _error(_near(part[0] if part else None))
            target = part[:cut]
            if len(target) == 3 and target[1].kind == "punct" and target[1].text == ".":
                target = target[2:]
            if len(target) != 1 or target[0].kind not in ("word", "br", "dq") or (target[0].kind == "word" and target[0].up in KEYWORDS):
                _error(_near(part[0]))
            operator = part[cut].text
            expr = part[cut + 1:]
            if not expr:
                _error(_near(None))
            column = _bracket(_name(target[0]))
            value = self.rw(expr, True)
            if operator != "=":
                value = f"{column} {operator[0]} ({value})"
            out.append(f"{column} = {value}")
        return ", ".join(out)

    def update(self, toks):
        i = 1
        if i < len(toks) and _is(toks[i], "TOP"):
            _error("UPDATE TOP is not available in the sandbox.")
        name, i = self._table_name(toks, i)
        if i >= len(toks) or not _is(toks[i], "SET"):
            _error(_near(toks[i] if i < len(toks) else None))
        rest = toks[i + 1:]
        self._reject_clauses(rest, "UPDATE", ["OUTPUT"])
        if _top_level(rest, "FROM"):
            _error(f"UPDATE ... FROM (updating from a join) {_LOCAL_LAB}")
        where = _top_level(rest, "WHERE")
        sets = rest[: where[0]] if where else rest
        sql = f"UPDATE {_bracket(name)} SET {self._assignments(sets, name)}"
        if where:
            sql += " WHERE " + self.rw(rest[where[0] + 1:], True)
        return [("dml", sql, self.params())]

    def delete(self, toks):
        i = 1
        if i < len(toks) and _is(toks[i], "TOP"):
            _error("DELETE TOP is not available in the sandbox.")
        if i < len(toks) and _is(toks[i], "FROM"):
            i += 1
        name, i = self._table_name(toks, i)
        rest = toks[i:]
        self._reject_clauses(rest, "DELETE", ["OUTPUT"])
        if _top_level(rest, "FROM"):
            _error(f"DELETE ... FROM (deleting by joining) {_LOCAL_LAB}")
        sql = f"DELETE FROM {_bracket(name)}"
        if rest:
            if not _is(rest[0], "WHERE"):
                _error(_near(rest[0]))
            sql += " WHERE " + self.rw(rest[1:], True)
        return [("dml", sql, self.params())]

    # -- DDL ---------------------------------------------------------------------------------

    def create(self, toks):
        i = 1
        unique = clustered = False
        while i < len(toks) and _is(toks[i], "UNIQUE", "CLUSTERED", "NONCLUSTERED"):
            unique = unique or toks[i].up == "UNIQUE"
            clustered = clustered or toks[i].up == "CLUSTERED"
            i += 1
        if i >= len(toks):
            _error(_near(None))
        kind = toks[i].up if toks[i].kind == "word" else None
        if kind == "TABLE":
            return self._create_table(toks, i + 1)
        if kind == "INDEX":
            return self._create_index(toks, i + 1, unique, clustered)
        if kind == "VIEW":
            name, j = self._table_name(toks, i + 1)
            cols = ""
            if j < len(toks) and toks[j].kind == "punct" and toks[j].text == "(":
                end = _close(toks, j)
                cols = " (" + ", ".join(_bracket(_name(t)) for t in toks[j + 1:end] if not (t.kind == "punct" and t.text == ",")) + ")"
                j = end + 1
            if j >= len(toks) or not _is(toks[j], "AS"):
                _error(_near(toks[j] if j < len(toks) else None))
            return [("ddl", f"CREATE VIEW {_bracket(name)}{cols} AS {self.rw(toks[j + 1:], True)}", self.params())]
        _error(f"CREATE {toks[i].text} {_LOCAL_LAB}")

    def _create_table(self, toks, i):
        temp = i < len(toks) and toks[i].kind == "word" and toks[i].text.startswith("#")
        name, i = self._table_name(toks, i)
        if i >= len(toks) or not (toks[i].kind == "punct" and toks[i].text == "("):
            _error(_near(toks[i] if i < len(toks) else None))
        end = _close(toks, i)
        if end != len(toks) - 1:
            _error(_near(toks[end + 1]))
        definitions = _split_commas(toks[i + 1:end])
        table_pk = None
        for d in definitions:
            if self._is_table_constraint(d):
                if _is(d[0], "CONSTRAINT"):
                    d = d[2:]
                if d and _is(d[0], "PRIMARY"):
                    k = next(idx for idx, t in enumerate(d) if t.kind == "punct" and t.text == "(")
                    cols = [_name(t) for t in d[k + 1:_close(d, k)] if not (t.kind == "punct" and t.text == ",")]
                    table_pk = cols
        pieces, columns, drop_table_pk = [], [], False
        for d in definitions:
            if not d:
                _error("Incorrect syntax near ','.")
            if self._is_table_constraint(d):
                sql, skip = self._table_constraint(d, table_pk, columns)
                if sql:
                    pieces.append(sql)
                continue
            sql, info = self._column(d, table_pk)
            pieces.append(sql)
            columns.append(info)
        if not columns:
            _error("A table must have at least one column.")
        pieces = [p for p in pieces if p]
        sql = f"CREATE {'TEMP ' if temp else ''}TABLE {_bracket(name)} (" + ", ".join(pieces) + ") STRICT"
        self.s.catalog[name.lower()] = columns
        return [("ddl", sql, set())]

    @staticmethod
    def _is_table_constraint(d):
        word = d[0].up if d[0].kind == "word" else ""
        if word == "CONSTRAINT":
            return len(d) >= 3 and _is(d[2], "PRIMARY", "UNIQUE", "FOREIGN", "CHECK")
        return word in ("PRIMARY", "UNIQUE", "FOREIGN", "CHECK")

    def _table_constraint(self, d, table_pk, columns):
        name = ""
        if _is(d[0], "CONSTRAINT"):
            name = f"CONSTRAINT {_bracket(_name(d[1]))} "
            d = d[2:]
        kind = d[0].up
        k = 1
        while k < len(d) and _is(d[k], "CLUSTERED", "NONCLUSTERED"):
            k += 1
        if kind == "CHECK":
            return f"{self._check_name(name)}CHECK ({self.rw(d[k + 1:_close(d, k)], True)})", False
        if kind in ("PRIMARY", "UNIQUE"):
            if kind == "PRIMARY":
                if k >= len(d) or not _is(d[k], "KEY"):
                    _error(_near(d[k] if k < len(d) else None))
                k += 1
                while k < len(d) and _is(d[k], "CLUSTERED", "NONCLUSTERED"):
                    k += 1
            if k >= len(d) or not (d[k].kind == "punct" and d[k].text == "("):
                _error(_near(d[k] if k < len(d) else None))
            cols = [t for t in d[k + 1:_close(d, k)] if not (t.kind == "punct" and t.text == ",")]
            names = [_name(t) for t in cols if not _is(t, "ASC", "DESC")]
            if kind == "PRIMARY" and len(names) == 1:
                return "", True  # emitted on the column (needed for IDENTITY)
            if kind == "PRIMARY":
                return f"{name}PRIMARY KEY ({', '.join(_bracket(x) for x in names)})", False
            return f"{name}UNIQUE ({', '.join(_bracket(x) for x in names)})", False
        # FOREIGN KEY (cols) REFERENCES t (cols) [ON DELETE x] [ON UPDATE y]
        if k >= len(d) or not _is(d[k], "KEY"):
            _error(_near(d[k] if k < len(d) else None))
        k += 1
        close = _close(d, k)
        local = [_bracket(_name(t)) for t in d[k + 1:close] if not (t.kind == "punct" and t.text == ",")]
        return f"{name}FOREIGN KEY ({', '.join(local)}) " + self._references(d, close + 1), False

    def _references(self, d, k):
        if k >= len(d) or not _is(d[k], "REFERENCES"):
            _error(_near(d[k] if k < len(d) else None))
        table, k = self._table_name(d, k + 1)
        out = f"REFERENCES {_bracket(table)}"
        if k < len(d) and d[k].kind == "punct" and d[k].text == "(":
            end = _close(d, k)
            out += " (" + ", ".join(_bracket(_name(t)) for t in d[k + 1:end] if not (t.kind == "punct" and t.text == ",")) + ")"
            k = end + 1
        while k < len(d):
            if _is(d[k], "ON") and k + 1 < len(d) and _is(d[k + 1], "DELETE", "UPDATE"):
                event = d[k + 1].up
                action = []
                k += 2
                while k < len(d) and not _is(d[k], "ON") and not _is(d[k], "NOT", "NULL", "CONSTRAINT", "CHECK", "UNIQUE", "PRIMARY", "DEFAULT"):
                    action.append(d[k].up)
                    k += 1
                phrase = " ".join(action)
                if phrase not in ("CASCADE", "SET NULL", "NO ACTION", "SET DEFAULT"):
                    _error(f"ON {event} {phrase} is not a valid referential action.")
                out += f" ON {event} {phrase}"
            else:
                break
        return out

    def _column(self, d, table_pk):
        if d[0].kind not in ("word", "br", "dq") or (d[0].kind == "word" and d[0].up in KEYWORDS):
            _error(_near(d[0]))
        name = _name(d[0])
        type_name, precision, scale, k = _parse_type(d, 1)
        sqltype, checks, kind = self._sql_type(name, type_name, precision, scale)
        identity = notnull = pk = False
        parts = []
        pending = ""
        default = None
        unique = ""
        refs = ""
        extra_checks = []
        while k < len(d):
            t = d[k]
            if _is(t, "NOT") and k + 1 < len(d) and _is(d[k + 1], "NULL"):
                notnull = True
                k += 2
            elif _is(t, "NULL"):
                k += 1
            elif t.kind == "word" and t.up == "IDENTITY":
                identity = True
                k += 1
                if k < len(d) and d[k].kind == "punct" and d[k].text == "(":
                    end = _close(d, k)
                    seeds = [x.text for x in d[k + 1:end] if x.kind == "num"]
                    if seeds not in ([], ["1", "1"]):
                        _error("IDENTITY seeds other than (1, 1) are not available in the sandbox.")
                    k = end + 1
            elif _is(t, "DEFAULT"):
                k += 1
                start = k
                while k < len(d) and not _is(d[k], "NOT", "NULL", "CONSTRAINT", "PRIMARY", "UNIQUE", "CHECK", "REFERENCES") and not (d[k].kind == "word" and d[k].up == "IDENTITY"):
                    if d[k].kind == "punct" and d[k].text == "(":
                        k = _close(d, k)
                    k += 1
                expr = d[start:k]
                rendered = self.rw(expr, True)
                default = "CURRENT_TIMESTAMP" if rendered == "tsql_getdate()" else (rendered if len(expr) == 1 and expr[0].kind in ("num", "str") else f"({rendered})")
            elif _is(t, "CONSTRAINT"):
                pending = f"CONSTRAINT {_bracket(_name(d[k + 1]))} "
                k += 2
            elif _is(t, "PRIMARY"):
                k += 1
                if k >= len(d) or not _is(d[k], "KEY"):
                    _error(_near(d[k] if k < len(d) else None))
                k += 1
                while k < len(d) and _is(d[k], "CLUSTERED", "NONCLUSTERED"):
                    k += 1
                pk = True
                parts.append(pending + "PRIMARY KEY")
                pending = ""
            elif _is(t, "UNIQUE"):
                k += 1
                while k < len(d) and _is(d[k], "CLUSTERED", "NONCLUSTERED"):
                    k += 1
                unique += " " + pending + "UNIQUE"
                pending = ""
            elif _is(t, "CHECK"):
                end = _close(d, k + 1)
                extra_checks.append(f"{self._check_name(pending)}CHECK ({self.rw(d[k + 2:end], True)})")
                pending = ""
                k = end + 1
            elif _is(t, "REFERENCES"):
                refs += " " + pending + self._references(d, k)
                pending = ""
                while k < len(d) and not _is(d[k], "NOT", "NULL", "CONSTRAINT", "CHECK", "UNIQUE", "PRIMARY", "DEFAULT", "IDENTITY"):
                    k += 1
            elif _is(t, "COLLATE"):
                collation = _name(d[k + 1]).upper() if k + 1 < len(d) else ""
                if "_CI_" not in collation:
                    _error(f"COLLATE {collation} is not available in the sandbox; the sandbox always compares text case-insensitively.")
                k += 2
            else:
                _error(_near(t))
        if table_pk and len(table_pk) == 1 and table_pk[0].lower() == name.lower():
            pk = True
        elif table_pk and name.lower() in [x.lower() for x in table_pk]:
            notnull = True
        if identity and not pk:
            _error("In the sandbox an IDENTITY column must also be the table's primary key.")
        if identity and kind != "int":
            _error("IDENTITY needs an integer column.")
        sql = _bracket(name) + " " + sqltype
        if pk:
            constraint = parts[0] if parts else "PRIMARY KEY"
            sql += f" {constraint}" + (" AUTOINCREMENT" if identity else "")
            notnull = True
        if notnull:
            sql += " NOT NULL"
        if kind == "char":
            sql += " COLLATE NOCASE"
        if default is not None:
            sql += f" DEFAULT {default}"
        sql += unique + refs
        for c in checks + extra_checks:
            sql += f" {c}"
        return sql, {"name": name, "kind": kind, "type": type_name, "nullable": not notnull, "pk": pk, "identity": identity}

    def _sql_type(self, column, name, precision, scale):
        safe = column.replace("]", "")
        if name in _CHAR:
            checks = [] if not precision else [f"CONSTRAINT [__len_{safe}] CHECK (length([{safe}]) <= {precision})"]
            return "TEXT", checks, "char"
        if name in _INT:
            if name == "bit":
                return "INTEGER", [f"CONSTRAINT [__range_{safe}] CHECK ([{safe}] IN (0, 1))"], "int"
            if name == "bigint" or name == "integer":
                return "INTEGER", [], "int"
            low, high = _INT_RANGES[name]
            return "INTEGER", [f"CONSTRAINT [__range_{safe}] CHECK ([{safe}] BETWEEN {low} AND {high})"], "int"
        if name in _DEC or name in _FLOAT:
            return "REAL", [], "num"
        if name in _DATE:
            return "TEXT", [], "date"
        if name in _BINARY:
            return "BLOB", [], "other"
        if name == "uniqueidentifier":
            return "TEXT", [], "char"
        _error(f"Data type '{name}' is not available in the sandbox.")

    def _create_index(self, toks, i, unique, clustered):
        if i >= len(toks):
            _error(_near(None))
        index_name = _name(toks[i])
        i += 1
        if i >= len(toks) or not _is(toks[i], "ON"):
            _error(_near(toks[i] if i < len(toks) else None))
        table, i = self._table_name(toks, i + 1)
        if i >= len(toks) or not (toks[i].kind == "punct" and toks[i].text == "("):
            _error(_near(toks[i] if i < len(toks) else None))
        end = _close(toks, i)
        keys = []
        for part in _split_commas(toks[i + 1:end]):
            if not part or part[0].kind not in ("word", "br", "dq"):
                _error(_near(part[0] if part else None))
            keys.append(_bracket(_name(part[0])) + (" DESC" if _is(part[-1], "DESC") else ""))
        i = end + 1
        includes, where = [], ""
        while i < len(toks):
            if toks[i].kind == "word" and toks[i].text.upper() == "INCLUDE":
                close = _close(toks, i + 1)
                includes = [_name(t) for t in toks[i + 2:close] if not (t.kind == "punct" and t.text == ",")]
                i = close + 1
            elif _is(toks[i], "WHERE"):
                where = " WHERE " + self.rw(toks[i + 1:], True)
                break
            else:
                _error(f"Index option '{toks[i].text}' {_LOCAL_LAB}")
        self.s.indexes[index_name.lower()] = {
            "table": table, "columns": [k.replace("[", "").replace("]", "").replace(" DESC", "") for k in keys],
            "includes": includes, "unique": unique, "clustered": clustered,
        }
        return [("ddl", f"CREATE {'UNIQUE ' if unique else ''}INDEX {_bracket(index_name)} ON {_bracket(table)} ({', '.join(keys)}){where}", set())]

    def alter(self, toks):
        if len(toks) < 4 or not _is(toks[1], "TABLE"):
            _error(f"ALTER {toks[1].text if len(toks) > 1 else ''} {_LOCAL_LAB}")
        table, i = self._table_name(toks, 2)
        if i >= len(toks):
            _error(_near(None))
        action = toks[i].up if toks[i].kind == "word" else ""
        if action == "ADD":
            body = toks[i + 1:]
            if body and _is(body[0], "CONSTRAINT") or (body and _is(body[0], "PRIMARY", "UNIQUE", "FOREIGN", "CHECK")):
                _error(f"ALTER TABLE ... ADD CONSTRAINT {_LOCAL_LAB} In the sandbox, declare constraints when you create the table or add a column with them.")
            if not body:
                _error(_near(None))
            sql, info = self._column(body, None)
            if info["pk"] or info["identity"]:
                _error("A column added with ALTER TABLE cannot be a PRIMARY KEY or IDENTITY in the sandbox.")
            if not info["nullable"] and " DEFAULT " not in sql:
                _error(f"Cannot add a NOT NULL column '{info['name']}' without a DEFAULT to a table in the sandbox.")
            self.s.catalog.setdefault(table.lower(), []).append(info)
            return [("ddl", f"ALTER TABLE {_bracket(table)} ADD COLUMN {sql}", set())]
        if action == "DROP":
            if i + 2 < len(toks) + 1 and i + 1 < len(toks) and toks[i + 1].kind == "word" and toks[i + 1].up == "COLUMN" and i + 2 < len(toks):
                column = _name(toks[i + 2])
                self.s.catalog[table.lower()] = [c for c in self.s.catalog.get(table.lower(), []) if c["name"].lower() != column.lower()]
                return [("ddl", f"ALTER TABLE {_bracket(table)} DROP COLUMN {_bracket(column)}", set())]
            _error(f"ALTER TABLE ... DROP {_LOCAL_LAB}")
        _error(f"ALTER TABLE ... {toks[i].text} {_LOCAL_LAB}")

    def drop(self, toks):
        if len(toks) < 2 or toks[1].kind != "word" or toks[1].up not in ("TABLE", "VIEW", "INDEX"):
            _error(f"DROP {toks[1].text if len(toks) > 1 else ''} {_LOCAL_LAB}")
        kind = toks[1].up
        i = 2
        exists = ""
        if i + 1 < len(toks) and _is(toks[i], "IF") and _is(toks[i + 1], "EXISTS"):
            exists = "IF EXISTS "
            i += 2
        statements = []
        if kind == "INDEX":
            name = _name(toks[i])
            self.s.indexes.pop(name.lower(), None)
            return [("ddl", f"DROP INDEX {exists}{_bracket(name)}", set())]
        for part in _split_commas(toks[i:]):
            name, _ = self._table_name(part, 0)
            if kind == "TABLE":
                self.s.catalog.pop(name.lower(), None)
                for key in [k for k, v in self.s.indexes.items() if v["table"].lower() == name.lower()]:
                    self.s.indexes.pop(key)
            statements.append(("ddl", f"DROP {kind} {exists}{_bracket(name)}", set()))
        return statements

    # -- transactions, variables, printing ---------------------------------------------------

    def begin(self, toks):
        if len(toks) >= 2 and _is(toks[1], "TRAN", "TRANSACTION"):
            return [("tran", "begin", None)]
        _error("BEGIN ... END blocks belong to procedural T-SQL, which " + _LOCAL_LAB)

    def commit(self, toks):
        return [("tran", "commit", None)]

    def rollback(self, toks):
        name = _name(toks[2]) if len(toks) >= 3 and toks[2].kind in ("word", "br") else None
        return [("tran", "rollback", name)]

    def save(self, toks):
        if len(toks) != 3 or not _is(toks[1], "TRAN", "TRANSACTION"):
            _error(_near(toks[1] if len(toks) > 1 else None))
        return [("tran", "save", _name(toks[2]))]

    def declare(self, toks):
        items = _split_commas(toks[1:])
        out = []
        for item in items:
            if not item or item[0].kind != "var" or item[0].text.startswith("@@"):
                _error(_near(item[0] if item else None))
            key = item[0].text[1:].lower()
            k = 1
            if k < len(item) and _is(item[k], "AS"):
                k += 1
            if k < len(item) and _is(item[k], "TABLE"):
                _error(f"Table variables {_LOCAL_LAB}")
            name, precision, scale, k = _parse_type(item, k)
            if name not in _CHAR | _INT | _DEC | _FLOAT | set(_DATE):
                _error(f"Data type '{name}' is not available in the sandbox.")
            if key in self.s.vars:
                _error(f"The variable name '@{item[0].text[1:]}' has already been declared.")
            family = "char" if name in _CHAR else "int" if name in _INT else "num" if name in _DEC | _FLOAT else "date"
            sql, used = None, set()
            if k < len(item):
                if not (item[k].kind == "op" and item[k].text == "="):
                    _error(_near(item[k]))
                sql, used = self._evaluate(item[k + 1:], name, precision, scale)
            self.s.vars[key] = [family, None, name, precision, scale]
            out.append(("declare", key, sql, used))
        return out

    def _evaluate(self, expr, type_name, precision, scale):
        """SQL (and the variables it reads) that computes `expr` converted to the declared type."""
        if not expr:
            _error(_near(None))
        translator = _Translator(self.s)
        kind, p, s = _cast_kind(type_name, precision, scale)
        sql = f"SELECT tsql_cast({translator.rw(expr, True)}, '{kind}', {p}, {s}, 1)"
        return sql, translator.params()

    def set_statement(self, toks):
        if len(toks) >= 3 and toks[1].kind == "word" and toks[1].up in ("NOCOUNT", "ANSI_NULLS", "QUOTED_IDENTIFIER", "ANSI_WARNINGS"):
            return [("none",)]
        if len(toks) == 3 and _is(toks[1], "XACT_ABORT"):
            self.s.xact_abort = _is(toks[2], "ON")
            return [("none",)]
        if len(toks) >= 4 and toks[1].kind == "var" and toks[2].kind == "op" and toks[2].text in ("=", "+=", "-=", "*=", "/=", "%="):
            key = toks[1].text[1:].lower()
            if key not in self.s.vars:
                _error(f'Must declare the scalar variable "{toks[1].text}".')
            _family, _value, type_name, precision, scale = self.s.vars[key]
            expr = toks[3:]
            if toks[2].text != "=":
                expr = [toks[1], Tok("op", toks[2].text[0]), Tok("punct", "(")] + expr + [Tok("punct", ")")]
            sql, used = self._evaluate(expr, type_name, precision, scale)
            return [("setvar", key, sql, used)]
        _error(f"SET {toks[1].text if len(toks) > 1 else ''} {_LOCAL_LAB}")

    def print_statement(self, toks):
        translator = _Translator(self.s)
        return [("print", None, f"SELECT {translator.rw(toks[1:], True)}", translator.params())]


# =============================================================================================
# Grader helpers
# =============================================================================================

WORKSPACE = os.path.dirname(os.path.abspath(__file__))
SUBMISSION = os.path.join(WORKSPACE, "submission.sql")


class GraderFailure(Exception):
    pass


def fail(message):
    raise GraderFailure(message)


def check(condition, message):
    if not condition:
        fail(message)


def show(value, limit=200):
    text = repr(value)
    return text if len(text) <= limit else text[:limit] + "..."


def load_submission():
    with open(SUBMISSION, encoding="utf-8") as handle:
        text = handle.read()
    check(text.strip(), "Your answer is empty. Write the T-SQL the exercise asks for.")
    return text


def new_db(setup=""):
    """A fresh database, with the fixture's setup script (T-SQL) already run."""
    session = Session()
    try:
        session.run(setup)
    except TsqlError as error:
        fail(f"The exercise's own setup failed ({error}). This is a bug in the exercise, not your answer.")
    check(not session.errors, f"The exercise's own setup failed ({session.errors[:1]}). This is a bug in the exercise.")
    return session


def run_learner(session, text=None, allow_errors=False):
    """Run the learner's script; plain-words failure if it does not compile or hits a runtime error."""
    text = load_submission() if text is None else text
    try:
        results = session.run(text)
    except TsqlError as error:
        fail(str(error))
    if session.errors and not allow_errors:
        fail(session.errors[0])
    return results


def last_result(results, what="a result set"):
    check(results, f"Your script did not return {what}. End it with a SELECT.")
    return results[-1]


def rows(session, sql):
    """Run a grader check (T-SQL) and return its rows."""
    try:
        return session.rows(sql)
    except TsqlError as error:
        fail(f"A grader check failed to run ({error}). This is a bug in the exercise.")


def scalar(session, sql):
    result = rows(session, sql)
    return result[0][0] if result and result[0] else None


def _same_value(a, b, tolerance=1e-6):
    if isinstance(a, (int, float)) and isinstance(b, (int, float)) and not isinstance(a, bool) and not isinstance(b, bool):
        return abs(a - b) <= tolerance * max(1.0, abs(b))
    if isinstance(a, str) and isinstance(b, str):
        return a == b
    return a == b


def same_rows(actual, expected, ordered=True):
    if len(actual) != len(expected):
        return False
    if not ordered:
        key = lambda row: tuple((v is None, str(v) if not isinstance(v, (int, float)) else f"{float(v):020.6f}") for v in row)
        actual, expected = sorted(actual, key=key), sorted(expected, key=key)
    return all(len(a) == len(e) and all(_same_value(x, y) for x, y in zip(a, e)) for a, e in zip(actual, expected))


def expect_rows(actual, expected, what, ordered=True):
    check(
        same_rows(actual, [list(r) for r in expected], ordered),
        f"{what}: expected {show([list(r) for r in expected])}"
        f"{' in this order' if ordered else ''} but got {show(actual)}.",
    )


def expect_columns(session_or_columns, expected, what):
    """The learner's result columns (or a table's) match `expected` names, ignoring case."""
    check([c.lower() for c in session_or_columns] == [e.lower() for e in expected], f"{what}: expected columns {expected} but got {list(session_or_columns)}.")


_passed_tests = []


def test(name):
    _passed_tests.append(name)
    print(f"ok - {name}")


def run(main):
    try:
        main()
    except GraderFailure as failure:
        print(f"Not yet: {failure}")
        sys.exit(1)
    print("PATHWAY_TEST_PASS")
