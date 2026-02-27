import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface HealthResponse {
  version: string;
  status: string;
  timestamp: string;
}

export interface StepResult {
  success: boolean;
  data: unknown;
  error: string | null;
  elapsedMs: number;
}

export interface PipelineResult {
  documentId: string;
  fileName: string;
  overallSuccess: boolean;
  metadata: StepResult;
  language: StepResult;
  contentSafety: StepResult;
  plagiarism: StepResult;
  ragIndex: StepResult;
  summarization: StepResult;
  qnA: StepResult;
  totalElapsedMs: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.baseUrl}/api/health`);
  }

  uploadDocument(file: File): Observable<PipelineResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<PipelineResult>(`${this.baseUrl}/api/documents/process`, formData);
  }
}
