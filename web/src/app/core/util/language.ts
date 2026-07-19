/** Common ISO 639-1 code → display name. Falls back to the uppercased code. */
const NAMES: Record<string, string> = {
  en: 'English',
  ja: 'Japanese',
  es: 'Spanish',
  de: 'German',
  fr: 'French',
  it: 'Italian',
  ko: 'Korean',
  zh: 'Chinese',
  pt: 'Portuguese',
  ru: 'Russian',
  ar: 'Arabic',
  nl: 'Dutch',
  sv: 'Swedish',
  pl: 'Polish',
  tr: 'Turkish',
  hi: 'Hindi',
  vi: 'Vietnamese',
};

export function languageName(code: string | null | undefined): string {
  if (!code) return '';
  return NAMES[code.toLowerCase()] ?? code.toUpperCase();
}
