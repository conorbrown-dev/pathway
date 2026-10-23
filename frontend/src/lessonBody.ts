// A deliberately small Markdown subset for lesson bodies. Lesson copy is authored in the API, so
// this never produces HTML: it returns plain data that React renders as elements, which keeps
// lesson text from ever being interpreted as markup.
//
// Supported: `#`–`###` headings, fenced code blocks (```lang), `-`/`*` bullet lists, `1.`/`1)`
// numbered lists, paragraphs separated by blank lines, and inline `code` and **bold**.

export type LessonInline =
  | { kind: "text"; text: string }
  | { kind: "code"; text: string }
  | { kind: "strong"; text: string };

export type LessonBlock =
  | { kind: "heading"; level: 2 | 3 | 4; text: string }
  | { kind: "code"; language: string | null; code: string }
  | { kind: "list"; ordered: boolean; items: string[] }
  | { kind: "paragraph"; text: string };

const fencePattern = /^\s*```\s*([\w#+-]*)\s*$/;
const headingPattern = /^(#{1,3})\s+(.+?)\s*#*\s*$/;
const bulletPattern = /^\s*[-*]\s+(.*)$/;
const numberedPattern = /^\s*\d+[.)]\s+(.*)$/;

export function parseLessonBody(body: string): LessonBlock[] {
  const blocks: LessonBlock[] = [];
  const lines = body.replace(/\r\n?/g, "\n").split("\n");
  let paragraph: string[] = [];
  let list: { ordered: boolean; items: string[] } | null = null;

  const flushParagraph = () => {
    const text = paragraph.join("\n").trim();
    if (text) blocks.push({ kind: "paragraph", text });
    paragraph = [];
  };
  const flushList = () => {
    if (list && list.items.length > 0) blocks.push({ kind: "list", ...list });
    list = null;
  };
  const flush = () => {
    flushParagraph();
    flushList();
  };

  for (let index = 0; index < lines.length; index++) {
    const line = lines[index];

    const fence = fencePattern.exec(line);
    if (fence) {
      flush();
      const code: string[] = [];
      index++;
      while (index < lines.length && !fencePattern.test(lines[index])) {
        code.push(lines[index]);
        index++;
      }
      // An unterminated fence still renders everything after it as code rather than vanishing.
      blocks.push({ kind: "code", language: fence[1] ? fence[1].toLowerCase() : null, code: code.join("\n") });
      continue;
    }

    if (!line.trim()) {
      flush();
      continue;
    }

    const heading = headingPattern.exec(line);
    if (heading) {
      flush();
      // A lesson's own title is the page's h1, so body headings start one level below it.
      blocks.push({ kind: "heading", level: (heading[1].length + 1) as 2 | 3 | 4, text: heading[2] });
      continue;
    }

    const bullet = bulletPattern.exec(line);
    const numbered = bullet ? null : numberedPattern.exec(line);
    if (bullet || numbered) {
      const ordered = numbered !== null;
      flushParagraph();
      if (list && list.ordered !== ordered) flushList();
      list ??= { ordered, items: [] };
      list.items.push((bullet ?? numbered)![1].trim());
      continue;
    }

    // An indented line directly under a list item continues that item.
    if (list && /^\s{2,}\S/.test(line)) {
      list.items[list.items.length - 1] += ` ${line.trim()}`;
      continue;
    }

    flushList();
    paragraph.push(line);
  }

  flush();
  return blocks;
}

export function parseLessonInline(text: string): LessonInline[] {
  const parts: LessonInline[] = [];
  const pattern = /`([^`]+)`|\*\*([^*]+)\*\*/g;
  let last = 0;
  for (let match = pattern.exec(text); match; match = pattern.exec(text)) {
    if (match.index > last) parts.push({ kind: "text", text: text.slice(last, match.index) });
    parts.push(match[1] !== undefined ? { kind: "code", text: match[1] } : { kind: "strong", text: match[2] });
    last = match.index + match[0].length;
  }
  if (last < text.length) parts.push({ kind: "text", text: text.slice(last) });
  return parts;
}
