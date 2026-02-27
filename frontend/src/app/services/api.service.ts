import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PipelineStepMeta {
  id: number;
  name: string;
  icon: string;
  description: string;
}

export interface FlaggedItemSummary {
  field: string;
  agentResult: string;
  confidence: number;
  humanCorrection: string | null;
}

export interface ReviewDecision {
  documentId: string;
  approved: boolean;
  rejectionReason: string | null;
  reviewedBy: string;
  decidedAt: string;
}

export interface DocumentSummary {
  documentId: string;
  fileName: string;
  overallSuccess: boolean;
  totalElapsedMs: number;
  requiresReview: boolean;
  isResolved: boolean;
  overallConfidence: number;
  flaggedItems: FlaggedItemSummary[];
  processedAt: string;
  reviewDecision?: ReviewDecision;
}

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
  // 11 agent steps in pipeline order
  ingestion: StepResult; // 1. Ingestion Agent
  preProcess: StepResult; // 2. Pre-process Agent
  translation: StepResult; // 3. Translation Agent
  extraction: StepResult; // 4. Extraction Agent
  validation: StepResult; // 5. Validation Agent
  contentSafety: StepResult; // 6. Content Safety Agent
  plagiarism: StepResult; // 7. Plagiarism Detection Agent
  ragIndex: StepResult; // 8. RAG Agent
  summarization: StepResult; // 9. Summary Agent
  qnA: StepResult; // 10. Q&A Agent
  humanFeedback: StepResult; // 11. Human Feedback Agent
  totalElapsedMs: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.baseUrl}/api/health`);
  }

  getPipelineSteps(): Observable<PipelineStepMeta[]> {
    return this.http.get<PipelineStepMeta[]>(`${this.baseUrl}/api/documents/pipeline-steps`);
  }

  getDocuments(): Observable<DocumentSummary[]> {
    return this.http.get<DocumentSummary[]>(`${this.baseUrl}/api/documents`);
  }

  submitReview(
    documentId: string,
    approved: boolean,
    rejectionReason?: string,
  ): Observable<ReviewDecision> {
    return this.http.post<ReviewDecision>(`${this.baseUrl}/api/documents/${documentId}/review`, {
      approved,
      rejectionReason: rejectionReason ?? null,
    });
  }

  uploadDocument(file: File): Observable<PipelineResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<PipelineResult>(`${this.baseUrl}/api/documents/process`, formData);
  }

  askQuestion(documentId: string, question: string, sessionId?: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/documents/${documentId}/ask`, {
      documentId,
      question,
      sessionId,
    });
  }

  submitHitlCorrection(documentId: string, field: string, correction: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/documents/${documentId}/correct`, {
      field,
      correction,
    });
  }
}
