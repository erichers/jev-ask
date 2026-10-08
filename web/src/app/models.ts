export interface ParsedIntent {
  ticker: string;
  direction: 'above' | 'below' | null;
  style: 'close' | 'touch';
  levelMode: 'absolute' | 'percent';
  level: number;
  expiry: string;
  expiryAssumed: boolean;
  rawText: string;
}

export interface AskStep {
  n: number;
  title: string;
  detail: string;
}

export interface ChartPoint {
  date: string;
  close: number;
}

export interface AskResponse {
  id: number;
  question: string;
  intent: ParsedIntent;
  parser: string;
  parserFellBack: boolean;
  attemptedParser: string | null;
  manualEdit: boolean;
  dataFreshness: string;
  dataOrigin: string;
  spot: number;
  spotDate: string;
  probability: number;
  bandLow: number;
  bandHigh: number;
  monteCarlo: number;
  empirical: number | null;
  empiricalSamples: number;
  vol20: number | null;
  vol60: number | null;
  vol252: number | null;
  volWindow: string;
  tradingDays: number;
  targetPrice: number;
  reasoning: string;
  steps: AskStep[];
  chart: ChartPoint[];
  disclaimer: string;
}

export interface TapeRow {
  ticker: string;
  close: number;
  date: string;
  change: number;
  freshness: string;
  origin: string;
}

export interface HistoryItem {
  id?: number;
  question: string;
  probability: number;
  ticker: string;
  at: string;
}

export interface HistorySummary {
  id: number;
  question: string;
  ticker: string;
  probability: number;
  createdAtUtc: string;
}

export function parserLabel(result: AskResponse): string {
  if (result.manualEdit) return 'Manual edit';
  if (result.parserFellBack && result.attemptedParser) {
    return 'Rule-based parser. ' + result.attemptedParser + ' was unusable';
  }
  if (result.parser === 'rule-based') return 'Rule-based parser';
  return result.parser;
}

export function dataLabel(result: AskResponse): string {
  const fresh = result.dataFreshness === 'live' ? 'Live data' : 'Cached data';
  return fresh + ' · ' + result.dataOrigin;
}

export function reducedMotion(): boolean {
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
