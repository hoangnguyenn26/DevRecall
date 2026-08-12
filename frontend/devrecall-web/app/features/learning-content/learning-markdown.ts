export type MarkdownBlock =
  | { type: 'paragraph' | 'blockquote'; text: string }
  | { type: 'heading'; level: number; text: string }
  | { type: 'code'; language: string; code: string }
  | { type: 'list'; ordered: boolean; items: string[] }

export type InlineToken =
  | { type: 'text' | 'strong' | 'emphasis' | 'code'; text: string }
  | { type: 'link'; text: string; url: string; safe: boolean }

export function parseInlineMarkdown(value: string): InlineToken[] {
  const tokens: InlineToken[] = []
  const pattern = /(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*|\[[^\]]+\]\([^)]+\))/g
  let cursor = 0
  for (const match of value.matchAll(pattern)) {
    if (match.index > cursor) tokens.push({ type: 'text', text: value.slice(cursor, match.index) })
    const raw = match[0]
    if (raw.startsWith('`')) tokens.push({ type: 'code', text: raw.slice(1, -1) })
    else if (raw.startsWith('**')) tokens.push({ type: 'strong', text: raw.slice(2, -2) })
    else if (raw.startsWith('*')) tokens.push({ type: 'emphasis', text: raw.slice(1, -1) })
    else {
      const link = /^\[([^\]]+)\]\(([^)]+)\)$/.exec(raw)!
      tokens.push({ type: 'link', text: link[1]!, url: link[2]!, safe: isSafeMarkdownUrl(link[2]!) })
    }
    cursor = match.index + raw.length
  }
  if (cursor < value.length) tokens.push({ type: 'text', text: value.slice(cursor) })
  return tokens
}

export function isSafeMarkdownUrl(value: string): boolean {
  if (value.startsWith('/') || value.startsWith('#')) return true
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:' || url.protocol === 'mailto:'
  } catch { return false }
}

export function parseMarkdown(markdown: string): MarkdownBlock[] {
  const lines = markdown.replaceAll('\r\n', '\n').split('\n')
  const blocks: MarkdownBlock[] = []
  let paragraph: string[] = []
  const flush = () => {
    if (paragraph.length) blocks.push({ type: 'paragraph', text: paragraph.join(' ') })
    paragraph = []
  }
  for (let index = 0; index < lines.length;) {
    const line = lines[index]!
    if (line.startsWith('```')) {
      flush()
      const language = line.slice(3).trim()
      const code: string[] = []
      index++
      while (index < lines.length && !lines[index]!.startsWith('```')) code.push(lines[index++]!)
      if (index < lines.length) index++
      blocks.push({ type: 'code', language, code: code.join('\n') })
      continue
    }
    const heading = /^(#{1,6})\s+(.+)$/.exec(line)
    if (heading) {
      flush(); blocks.push({ type: 'heading', level: Math.min(6, heading[1]!.length + 2), text: heading[2]! }); index++; continue
    }
    const list = /^(\s*)([-*+] |\d+[.)] )(.+)$/.exec(line)
    if (list) {
      flush()
      const ordered = /^\d/.test(list[2]!)
      const items: string[] = []
      while (index < lines.length) {
        const item = /^(\s*)([-*+] |\d+[.)] )(.+)$/.exec(lines[index]!)
        if (!item || /^\d/.test(item[2]!) !== ordered) break
        items.push(item[3]!); index++
      }
      blocks.push({ type: 'list', ordered, items }); continue
    }
    if (line.startsWith('> ')) { flush(); blocks.push({ type: 'blockquote', text: line.slice(2) }); index++; continue }
    if (!line.trim()) flush(); else paragraph.push(line.trim())
    index++
  }
  flush()
  return blocks
}
