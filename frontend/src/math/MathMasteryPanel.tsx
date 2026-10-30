import { useState } from "react";

type Question = { id: string; skill: string; kind: "Numeric" | "Choice"; prompt: string; unit: string | null; choices: string[] };
type StartResponse = { attemptId: string; requiredCorrect: number; questions: Question[] };
type Result = { passed: boolean; correctAnswers: number; totalQuestions: number; requiredCorrect: number; missedSkills: string[]; feedback: string };

export function MathMasteryPanel({
  slug, api, headers, guidedPassed, mastered, onMastered, onNext,
}: {
  slug: string;
  api: string;
  headers: () => Record<string, string>;
  guidedPassed: boolean;
  mastered: boolean;
  onMastered: () => void;
  onNext: () => void;
}) {
  const [attempt, setAttempt] = useState<StartResponse | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [outcome, setOutcome] = useState<Result | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function start() {
    setPending(true);
    setError("");
    try {
      const response = await fetch(api + "/api/math/mastery/" + encodeURIComponent(slug) + "/start", { method: "POST", headers: headers() });
      if (!response.ok) throw Error("Couldn't start a new assessment. Try again.");
      setAttempt(await response.json() as StartResponse);
      setAnswers({});
      setOutcome(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Couldn't start the mastery check.");
    } finally { setPending(false); }
  }

  async function submit() {
    if (!attempt || Object.keys(answers).filter(id => answers[id]?.trim()).length !== attempt.questions.length) {
      setError("Answer every question before submitting your mastery check.");
      return;
    }
    setPending(true);
    setError("");
    try {
      const response = await fetch(api + "/api/math/mastery/" + encodeURIComponent(slug) + "/" + attempt.attemptId + "/submit", {
        method: "POST", headers: headers(), body: JSON.stringify({ answers }),
      });
      if (!response.ok) throw Error("Couldn't grade your assessment. Check the connection.");
      const next = await response.json() as Result;
      setOutcome(next);
      if (next.passed) onMastered();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Couldn't grade your answers.");
    } finally { setPending(false); }
  }

  return <section className="border-t border-[#dce5dd] bg-[#f2f8f2] p-6 sm:p-8" aria-label="Lesson mastery assessment">
    <p className="text-[10px] font-bold tracking-[1.2px] text-[#258160]">DEMONSTRATE MASTERY</p>
    <h2 className="mt-2 font-display text-2xl font-semibold text-[#254438]">Mastery check</h2>
    <p className="mt-2 text-sm leading-relaxed text-[#526157]">
      One correct practice answer is a starting point. Show that you can solve varied problems before unlocking the next math lesson.
      You need <strong>7 out of 8 correct</strong> (87.5%). Each new attempt uses different numbers and examples.
    </p>
    {mastered && !attempt && <div className="mt-4 rounded-lg border border-[#80b89b] bg-[#e5f2eb] p-4 text-sm text-[#216147]">
      <strong>Mastery demonstrated</strong>
      <p className="mt-1">You've earned progress for this lesson. You can continue, or take another practice assessment.</p>
      <button onClick={onNext} className="mt-3 rounded-md bg-[#258160] px-4 py-2 text-xs font-bold text-white">Next lesson →</button>
    </div>}
    {!attempt && <button onClick={() => void start()} disabled={pending || (!guidedPassed && !mastered)}
      className="mt-4 rounded-md bg-[#258160] px-4 py-3 text-xs font-bold text-white disabled:cursor-not-allowed disabled:opacity-50">
      {pending ? "Preparing…" : mastered ? "Practice another set" : "Begin mastery check"}
    </button>}
    {!guidedPassed && !mastered && <p className="mt-2 text-xs text-[#567265]">First work through the lesson's guided problem above. Correct practice unlocks the mastery check, not the next lesson.</p>}
    {attempt && !outcome && <div className="mt-5 space-y-4">
      <p className="text-xs font-semibold text-[#476455]">{Object.values(answers).filter(x => x?.trim()).length} / {attempt.questions.length} answered</p>
      {attempt.questions.map((q, i) => <fieldset key={q.id} className="min-w-0 rounded-lg border border-[#c5d9ca] bg-white p-4">
        <legend className="px-1 text-xs font-bold text-[#38765b]">Question {i + 1} · {q.skill}</legend>
        <p className="mb-3 text-sm leading-relaxed text-[#26342d]">{q.prompt}</p>
        {q.kind === "Choice" ? <div className="grid gap-2">
          {q.choices.map(choice => <label key={choice} className="flex cursor-pointer items-start gap-2 text-sm text-[#26342d]">
            <input type="radio" name={q.id} checked={answers[q.id] === choice} onChange={() => setAnswers(a => ({ ...a, [q.id]: choice }))} />
            <span>{choice}</span>
          </label>)}
        </div> : <div className="flex items-center gap-2">
          <label htmlFor={"mastery-" + q.id} className="sr-only">Answer question {i + 1}</label>
          <input id={"mastery-" + q.id} inputMode="decimal" value={answers[q.id] ?? ""} onChange={e => setAnswers(a => ({ ...a, [q.id]: e.target.value }))}
            className="min-w-0 flex-1 rounded-md border border-[#b4cbbc] px-3 py-2 font-mono text-sm text-[#26342d]" placeholder="Numeric answer" />
          <button type="button" aria-label={"Toggle sign for question " + (i + 1)} onClick={() => setAnswers(a => { const v=(a[q.id]??"").trim(); return { ...a, [q.id]: v.startsWith("-") || v.startsWith("−") ? v.slice(1) : "-" + v }; })}
            className="rounded-md border border-[#b4cbbc] bg-white px-3 py-2 font-mono text-sm text-[#26342d]">±</button>
          {q.unit && <span className="shrink-0 text-xs text-[#526157]">{q.unit}</span>}
        </div>}
      </fieldset>)}
      <button disabled={pending} onClick={() => void submit()} className="rounded-md bg-[#258160] px-4 py-3 text-xs font-bold text-white disabled:opacity-50">{pending ? "Grading…" : "Submit all 8 answers"}</button>
    </div>}
    {outcome && <div className={"mt-5 rounded-lg border p-4 " + (outcome.passed ? "border-[#80b89b] bg-[#e5f2eb] text-[#216147]" : "border-[#ecc7ac] bg-[#fff7e8] text-[#7f4a2b]")} role="status">
      <strong className="text-sm">{outcome.passed ? "Mastered!" : "Almost there — keep practicing"}</strong>
      <p className="mt-2 text-sm">Score: {outcome.correctAnswers}/{outcome.totalQuestions} correct (minimum {outcome.requiredCorrect}). {outcome.feedback}</p>
      {outcome.missedSkills.length > 0 && <p className="mt-2 text-xs">Review: {outcome.missedSkills.join(", ")}. Return to the worked examples and try fresh questions.</p>}
      <div className="mt-3 flex flex-wrap gap-2">
        {outcome.passed && <button onClick={onNext} className="rounded-md bg-[#258160] px-4 py-2 text-xs font-bold text-white">Next lesson →</button>}
        <button onClick={() => void start()} disabled={pending} className="rounded-md border border-current px-4 py-2 text-xs font-bold disabled:opacity-50">Try a new set</button>
      </div>
    </div>}
    {error && <p role="alert" className="mt-3 text-xs text-[#ad452b]">{error}</p>}
  </section>;
}
