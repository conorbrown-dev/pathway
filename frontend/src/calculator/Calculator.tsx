import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { Calculator as CalculatorIcon, CornerDownLeft, Trash2, X } from "lucide-react";
import { calculate, engineering, fmt, plain, type AngleMode } from "./engine";
import "./calculator.css";

/** Courses that get the calculator, keyed by course languageId. */
export const calculatorProfiles: Record<string, { siPrefixes: boolean }> = {
  math: { siPrefixes: false },
  "electrical-engineering": { siPrefixes: true },
};

export const calculatorProfileFor = (languageId: string | undefined) =>
  languageId ? calculatorProfiles[languageId] ?? null : null;

type Entry = { input: string; interpretation: string; value: number; assigned?: string; steps: string[] };

type Saved = { angleMode: AngleMode; history: Entry[]; variables: Record<string, number> };

const storageKey = "pathway-calculator";
const historyLimit = 30;

function load(): Saved {
  try {
    const raw = localStorage.getItem(storageKey);
    if (raw) {
      const saved = JSON.parse(raw) as Partial<Saved>;
      return {
        angleMode: saved.angleMode === "rad" ? "rad" : "deg",
        history: Array.isArray(saved.history) ? saved.history.slice(-historyLimit) : [],
        variables: saved.variables && typeof saved.variables === "object" ? saved.variables : {},
      };
    }
  } catch {
    // Storage can be unavailable (private windows); the calculator still works without it.
  }
  return { angleMode: "deg", history: [], variables: {} };
}

function save(state: Saved) {
  try {
    localStorage.setItem(storageKey, JSON.stringify(state));
  } catch {
    // Ignore: persistence is a convenience.
  }
}

type Key = { label: string; insert: string; title?: string; wide?: boolean; tone?: "op" | "fn" };

const numberPad: Key[] = [
  { label: "(", insert: "(" },
  { label: ")", insert: ")" },
  { label: "^", insert: "^", title: "Power", tone: "op" },
  { label: "÷", insert: "÷", tone: "op" },
  { label: "7", insert: "7" },
  { label: "8", insert: "8" },
  { label: "9", insert: "9" },
  { label: "×", insert: "×", tone: "op" },
  { label: "4", insert: "4" },
  { label: "5", insert: "5" },
  { label: "6", insert: "6" },
  { label: "−", insert: "-", tone: "op" },
  { label: "1", insert: "1" },
  { label: "2", insert: "2" },
  { label: "3", insert: "3" },
  { label: "+", insert: "+", tone: "op" },
  { label: "0", insert: "0" },
  { label: ".", insert: "." },
  { label: "ans", insert: "ans", title: "The previous answer", tone: "fn" },
];

const functionKeys: Key[] = [
  { label: "x²", insert: "²", title: "Square" },
  { label: "√", insert: "√(", title: "Square root" },
  { label: "π", insert: "π" },
  { label: "e", insert: "e", title: "Euler's number, 2.718…" },
  { label: "sin", insert: "sin(" },
  { label: "cos", insert: "cos(" },
  { label: "tan", insert: "tan(" },
  { label: "°", insert: "°", title: "Degrees, whatever the mode" },
  { label: "sin⁻¹", insert: "asin(", title: "Inverse sine: which angle has this sine?" },
  { label: "cos⁻¹", insert: "acos(", title: "Inverse cosine" },
  { label: "tan⁻¹", insert: "atan(", title: "Inverse tangent" },
  { label: "|x|", insert: "abs(", title: "Absolute value" },
  { label: "ln", insert: "ln(", title: "Natural log (base e)" },
  { label: "log", insert: "log(", title: "Log base 10, or log(x, base)" },
  { label: "log₂", insert: "log2(", title: "Log base 2" },
  { label: "n!", insert: "!", title: "Factorial" },
];

const engineeringKeys: Key[] = [
  { label: "k", insert: "k", title: "kilo, × 1,000" },
  { label: "M", insert: "M", title: "mega, × 1,000,000" },
  { label: "m", insert: "m", title: "milli, ÷ 1,000" },
  { label: "µ", insert: "µ", title: "micro, ÷ 1,000,000" },
  { label: "n", insert: "n", title: "nano, ÷ 10⁹" },
  { label: "p", insert: "p", title: "pico, ÷ 10¹²" },
  { label: "∥", insert: " || ", title: "In parallel: 1 ÷ (1/a + 1/b)" },
  { label: "hypot", insert: "hypot(", title: "√(a² + b²), e.g. impedance magnitude" },
];

export function CalculatorButton({ open, onToggle }: { open: boolean; onToggle: () => void }) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-expanded={open}
      aria-controls="pathway-calculator"
      className={`calc-toggle ${open ? "calc-toggle-open" : ""}`}
    >
      <CalculatorIcon size={15} aria-hidden />
      <span>Calculator</span>
    </button>
  );
}

export function CalculatorPanel({
  open,
  onClose,
  siPrefixes,
  onUseValue,
}: {
  open: boolean;
  onClose: () => void;
  siPrefixes: boolean;
  /** When the current exercise takes a number, results can be sent straight to the answer box. */
  onUseValue?: (value: string) => void;
}) {
  const initial = useMemo(load, []);
  const [angleMode, setAngleMode] = useState<AngleMode>(initial.angleMode);
  const [history, setHistory] = useState<Entry[]>(initial.history);
  const [variables, setVariables] = useState<Record<string, number>>(initial.variables);
  const [input, setInput] = useState("");
  const [recall, setRecall] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showHelp, setShowHelp] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const tapeRef = useRef<HTMLOListElement>(null);

  const ans = history.length ? history[history.length - 1].value : undefined;
  const options = { angleMode, siPrefixes, ans, variables };

  useEffect(() => save({ angleMode, history, variables }), [angleMode, history, variables]);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    tapeRef.current?.lastElementChild?.scrollIntoView({ block: "nearest" });
  }, [history.length]);

  // Live preview while typing: the result, or nothing until the expression is complete.
  const preview = useMemo(() => {
    if (!input.trim()) return null;
    const result = calculate(input, options);
    return result.ok ? result : null;
  }, [input, angleMode, siPrefixes, ans, variables]);

  if (!open) return null;

  const evaluate = () => {
    if (!input.trim()) return;
    const result = calculate(input, options);
    if (!result.ok) {
      setError(result.error);
      if (result.position !== undefined && inputRef.current) {
        inputRef.current.focus();
        inputRef.current.setSelectionRange(result.position, result.position + 1);
      }
      return;
    }
    if (result.assigned) setVariables((current) => ({ ...current, [result.assigned!]: result.value }));
    setHistory((current) => [
      ...current.slice(-(historyLimit - 1)),
      { input: input.trim(), interpretation: result.interpretation, value: result.value, assigned: result.assigned, steps: result.steps },
    ]);
    setInput("");
    setError(null);
    setRecall(null);
  };

  const insert = (text: string) => {
    const field = inputRef.current;
    const start = field?.selectionStart ?? input.length;
    const end = field?.selectionEnd ?? input.length;
    // Starting a fresh line with an operator continues from the previous answer, like a desk calculator.
    // Minus is left out so a line can start with a negative number.
    const continues = !input.trim() && ans !== undefined && /^[+×÷^²!]|^ \|\| /.test(text);
    const next = continues ? `ans${text}` : input.slice(0, start) + text + input.slice(end);
    const caret = continues ? next.length : start + text.length;
    setInput(next);
    setError(null);
    requestAnimationFrame(() => {
      field?.focus();
      field?.setSelectionRange(caret, caret);
    });
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      event.preventDefault();
      evaluate();
    } else if (event.key === "Escape") {
      event.preventDefault();
      if (input) setInput("");
      else onClose();
    } else if (event.key === "ArrowUp" && history.length) {
      event.preventDefault();
      const index = recall === null ? history.length - 1 : Math.max(0, recall - 1);
      setRecall(index);
      setInput(history[index].input);
    } else if (event.key === "ArrowDown" && recall !== null) {
      event.preventDefault();
      const index = recall + 1;
      if (index >= history.length) {
        setRecall(null);
        setInput("");
      } else {
        setRecall(index);
        setInput(history[index].input);
      }
    }
  };

  const clearAll = () => {
    setHistory([]);
    setVariables({});
    setInput("");
    setError(null);
  };

  const variableNames = Object.keys(variables);

  return (
    <aside id="pathway-calculator" className="calc-panel" aria-label="Calculator">
      <header className="calc-header">
        <strong>
          <CalculatorIcon size={15} aria-hidden /> Calculator
        </strong>
        <div className="calc-mode" role="radiogroup" aria-label="Angle unit">
          {(["deg", "rad"] as const).map((mode) => (
            <button
              key={mode}
              type="button"
              role="radio"
              aria-checked={angleMode === mode}
              className={angleMode === mode ? "calc-mode-on" : ""}
              onClick={() => setAngleMode(mode)}
              title={mode === "deg" ? "Angles in degrees (a full turn is 360)" : "Angles in radians (a full turn is 2π)"}
            >
              {mode.toUpperCase()}
            </button>
          ))}
        </div>
        <button type="button" className="calc-icon" onClick={() => setShowHelp((v) => !v)} aria-expanded={showHelp} title="How to type expressions">
          ?
        </button>
        <button type="button" className="calc-icon" onClick={onClose} aria-label="Close calculator">
          <X size={16} />
        </button>
      </header>

      {showHelp && (
        <div className="calc-help">
          <p>Type the way the lesson writes it. A few things to know:</p>
          <ul>
            <li><code>2 + 3 × 4</code> multiplies first. Each answer shows how it was read, and <em>Steps</em> lists the order.</li>
            <li><code>2π</code> and <code>3(4 + 1)</code> multiply without a sign. <code>1/2πf</code> means <code>1 ÷ (2πf)</code>.</li>
            <li><code>−3^2</code> is −9 (the power happens before the minus). Write <code>(−3)^2</code> for 9.</li>
            <li>Name a value with <code>t = 10</code>, then use <code>t</code> in a formula. <code>ans</code> is the last answer; ↑ recalls earlier lines.</li>
            <li>Trig follows DEG/RAD. Add <code>°</code> to force degrees: <code>sin(30°)</code>.</li>
            <li><code>log(x)</code> is base 10, <code>log2(x)</code> base 2, <code>log(x, b)</code> any base, <code>ln(x)</code> base e.</li>
            {siPrefixes && (
              <li>SI prefixes after a number: <code>4.7k</code>, <code>10µ</code> (or <code>10u</code>), <code>2.2nF</code>, <code>20mA</code>. <code>m</code> is milli and <code>M</code> is mega. <code>a || b</code> combines in parallel, and binds tighter than +.</li>
            )}
          </ul>
        </div>
      )}

      <ol className="calc-tape" ref={tapeRef} aria-live="polite">
        {history.length === 0 && <li className="calc-empty">Results appear here. Press Enter to calculate.</li>}
        {history.map((entry, index) => (
          <li key={`${index}-${entry.input}`} className="calc-entry">
            <button type="button" className="calc-entry-input" onClick={() => insert(entry.input)} title="Reuse this expression">
              {entry.input}
            </button>
            {entry.interpretation.replace(/\s/g, "") !== entry.input.replace(/\s/g, "") && (
              <p className="calc-read-as">read as {entry.interpretation}</p>
            )}
            <p className="calc-entry-value">
              {entry.assigned ? `${entry.assigned} = ` : "= "}
              {fmt(entry.value)}
              {siPrefixes && engineering(entry.value) && <span className="calc-eng"> ({engineering(entry.value)})</span>}
            </p>
            <div className="calc-entry-actions">
              {entry.steps.length > 1 && (
                <details>
                  <summary>Steps</summary>
                  <ol>
                    {entry.steps.map((step, stepIndex) => (
                      <li key={stepIndex}>{step}</li>
                    ))}
                  </ol>
                </details>
              )}
              {onUseValue && (
                <button type="button" className="calc-use" onClick={() => onUseValue(plain(entry.value))}>
                  Use in answer
                </button>
              )}
            </div>
          </li>
        ))}
      </ol>

      {variableNames.length > 0 && (
        <div className="calc-vars" aria-label="Variables">
          {variableNames.map((name) => (
            <button key={name} type="button" onClick={() => insert(name)} title={`Insert ${name}`}>
              {name} = {fmt(variables[name])}
            </button>
          ))}
        </div>
      )}

      <div className="calc-entry-line">
        <label className="sr-only" htmlFor="calculator-input">Expression</label>
        <input
          id="calculator-input"
          ref={inputRef}
          value={input}
          onChange={(event) => {
            setInput(event.target.value);
            setError(null);
            setRecall(null);
          }}
          onKeyDown={onKeyDown}
          placeholder={siPrefixes ? "e.g. 5V / 4.7k" : "e.g. 2000(1.06)^10"}
          autoComplete="off"
          autoCapitalize="off"
          spellCheck={false}
          aria-invalid={error ? true : undefined}
          aria-describedby="calculator-status"
        />
        <button type="button" className="calc-equals" onClick={evaluate} aria-label="Calculate">
          <CornerDownLeft size={16} />
        </button>
      </div>
      <p id="calculator-status" className={`calc-status ${error ? "calc-status-error" : ""}`} role={error ? "alert" : undefined}>
        {error ?? (preview ? `= ${fmt(preview.value)}` : " ")}
      </p>

      <div className="calc-keys calc-keys-fn">
        {[...functionKeys, ...(siPrefixes ? engineeringKeys : [])].map((key) => (
          <button key={key.label} type="button" title={key.title} onClick={() => insert(key.insert)}>
            {key.label}
          </button>
        ))}
      </div>
      <div className="calc-keys">
        {numberPad.map((key) => (
          <button key={key.label} type="button" title={key.title} className={key.tone ? `calc-key-${key.tone}` : ""} onClick={() => insert(key.insert)}>
            {key.label}
          </button>
        ))}
        <button type="button" className="calc-key-equals" onClick={evaluate}>
          =
        </button>
      </div>
      <div className="calc-footer">
        <button type="button" onClick={() => setInput((v) => v.slice(0, -1))}>⌫ Back</button>
        <button type="button" onClick={() => setInput("")}>Clear line</button>
        <button type="button" onClick={clearAll} title="Clear history and variables">
          <Trash2 size={13} aria-hidden /> Clear all
        </button>
      </div>
    </aside>
  );
}
