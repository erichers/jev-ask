import { Injectable } from '@angular/core';
import { AskResponse, HistorySummary, TapeRow } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  examples(): Promise<string[]> {
    return this.get<string[]>('/api/examples');
  }

  tape(): Promise<TapeRow[]> {
    return this.get<TapeRow[]>('/api/tape');
  }

  history(): Promise<HistorySummary[]> {
    return this.get<HistorySummary[]>('/api/history');
  }

  historyById(id: string): Promise<AskResponse> {
    return this.get<AskResponse>('/api/history/' + id);
  }

  ask(question: string): Promise<AskResponse> {
    return this.post<AskResponse>('/api/ask', { question });
  }

  recompute(body: {
    ticker: string;
    condition: string;
    style: string;
    levelMode: string;
    level: number;
    expiry: string;
  }): Promise<AskResponse> {
    return this.post<AskResponse>('/api/recompute', body);
  }

  async pdf(result: AskResponse): Promise<Blob> {
    const response = await fetch('/api/brief.pdf', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(result)
    });
    if (!response.ok) {
      throw new Error(await this.errorText(response));
    }
    return response.blob();
  }

  private async get<T>(url: string): Promise<T> {
    let response: Response;
    try {
      response = await fetch(url);
    } catch {
      throw new Error('The API did not respond. Start it with dotnet run, or use docker compose.');
    }
    if (!response.ok) throw new Error(await this.errorText(response));
    return response.json() as Promise<T>;
  }

  private async post<T>(url: string, body: unknown): Promise<T> {
    let response: Response;
    try {
      response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
    } catch {
      throw new Error('The API did not respond. Start it with dotnet run, or use docker compose.');
    }
    if (!response.ok) throw new Error(await this.errorText(response));
    return response.json() as Promise<T>;
  }

  private async errorText(response: Response): Promise<string> {
    try {
      const body = await response.json() as { error?: string };
      if (body.error) return body.error;
    } catch {
      // Fall through to a status line.
    }
    return 'Request failed (' + response.status + ').';
  }
}
