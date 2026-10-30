import { useId, useState } from "react";
import { waveAt, waveformPath } from "./waveMath";

type Props = { phasors?: boolean };

/** Self-contained laboratory: moving sliders redraws the waveform immediately. */
export function WavePlayground({ phasors = false }: Props) {
  const [amplitude, setAmplitude] = useState(2);
  const [period, setPeriod] = useState(2);
  const [phase, setPhase] = useState(0);
  const id = useId();
  const t = 0;
  const y0 = waveAt(t, amplitude, period, phase);
  const phaseRad = (phase * Math.PI) / 180;
  const dialX = 60 + 40 * Math.cos(phaseRad);
  const dialY = 60 - 40 * Math.sin(phaseRad);

  return (
    <section className="my-8 rounded-xl border border-[#c9d9d0] bg-white p-4 text-[#243d31]" aria-label="Interactive wave graph">
      <h2 className="font-display text-xl font-semibold">Interactive wave laboratory</h2>
      <p className="mt-2 text-sm">Move the sliders and observe how a sine wave changes. The graph uses time on its horizontal axis and signal value on its vertical axis. Angle is measured in degrees.</p>
      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        {([
          ["Amplitude", amplitude, setAmplitude, 0.5, 5, 0.5, "signal units"],
          ["Period", period, setPeriod, 0.5, 4, 0.5, "s per cycle"],
          ["Phase", phase, setPhase, -180, 180, 15, "degrees"],
        ] as const).map(([label, value, setter, min, max, step, unit]) => (
          <label key={label} className="block text-xs font-semibold">
            <span className="flex justify-between gap-2"><span>{label}</span><output>{value} {unit}</output></span>
            <input aria-label={label} className="mt-2 w-full accent-[#28836d]" type="range" min={min} max={max} step={step} value={value} onChange={(event) => setter(Number(event.target.value))} />
          </label>
        ))}
      </div>
      <svg className="mt-4 w-full rounded-lg bg-[#f4f8f5]" viewBox="0 0 690 288" role="img" aria-label={`Sine graph: amplitude ${amplitude}, period ${period} seconds, phase ${phase} degrees`}>
        <title>Wave shape changes with amplitude, period, and phase</title>
        {Array.from({ length: 9 }, (_, i) => <line key={`vx${i}`} x1={25 + i * 80} x2={25 + i * 80} y1="10" y2="250" stroke="#d8e5dd" />)}
        {Array.from({ length: 5 }, (_, i) => <line key={`hy${i}`} x1="25" x2="665" y1={10 + i * 60} y2={10 + i * 60} stroke="#d8e5dd" />)}
        <line x1="25" x2="665" y1="130" y2="130" stroke="#658d7b" strokeWidth="1.5" />
        <g transform="translate(25,10) scale(1,1)">
          {phasors && <path d={waveformPath(amplitude, period, 0, 640, 240, 8)} stroke="#84939e" strokeWidth="2" strokeDasharray="7 5" fill="none" />}
          <path d={waveformPath(amplitude, period, phase, 640, 240, 8)} stroke="#15765e" strokeWidth="3" fill="none" />
        </g>
        <text x="25" y="270" fontSize="12" fill="#47695b">0 s</text>
        <text x="626" y="270" fontSize="12" fill="#47695b">8 s</text>
        <text x="330" y="284" fontSize="12" fill="#47695b">time →</text>
        <text x="3" y="16" fontSize="12" fill="#47695b">+5</text>
        <text x="8" y="134" fontSize="12" fill="#47695b">0</text>
        <text x="3" y="249" fontSize="12" fill="#47695b">−5</text>
      </svg>
      <p className="mt-3 text-sm"><strong>At time 0:</strong> value = {y0.toFixed(2)} signal units. Frequency = {(1 / period).toFixed(2)} Hz; a shorter period produces more cycles in the same eight seconds.</p>
      {phasors && <div className="mt-4 flex flex-wrap items-center gap-4 rounded-md bg-[#eef4f0] p-3">
        <svg width="120" height="120" viewBox="0 0 120 120" role="img" aria-label={`Phasor direction ${phase} degrees`}>
          <circle cx="60" cy="60" r="40" fill="none" stroke="#94b4a1" />
          <line x1="12" y1="60" x2="108" y2="60" stroke="#94b4a1" />
          <line x1="60" y1="12" x2="60" y2="108" stroke="#94b4a1" />
          <line x1="60" y1="60" x2={dialX} y2={dialY} stroke="#15765e" strokeWidth="3" />
          <circle cx={dialX} cy={dialY} r="4" fill="#15765e" />
          <title>Phasor angle relative to the horizontal reference</title>
        </svg>
        <p className="max-w-md flex-1 text-sm">A <strong>phasor</strong> represents the amplitude and phase of a sinusoid at one shared frequency. The dashed line is the zero-phase reference; the green line is phase-shifted. The arrow turns to show the selected phase at time zero. For circuit comparison, voltage and current can have different phases.</p>
      </div>}
      <p className="mt-2 text-xs text-[#527563]" id={id}>Experiment: double the amplitude, halve the period, then shift the phase by 90°. Describe what changes and what stays the same.</p>
    </section>
  );
}
