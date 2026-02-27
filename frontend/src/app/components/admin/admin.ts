import { Component, signal } from '@angular/core';

import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HealthStatusComponent } from '../health-status/health-status';

export interface FlaggedItem {
  field: string;
  agentResult: string;
  confidence: number;
  humanCorrection: string | null;
}

export interface HitlReviewItem {
  documentId: string;
  fileName: string;
  flaggedItems: FlaggedItem[];
  overallConfidence: number;
  isResolved: boolean;
}

export interface AuditEntry {
  id: string;
  documentId: string;
  action: string;
  actor: string;
  details: string | null;
  timestamp: string;
}

@Component({
  selector: 'app-admin',
  imports: [CommonModule, FormsModule, HealthStatusComponent],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class AdminComponent {
  activeTab = signal<'dashboard' | 'hitl' | 'audit' | 'qna'>('dashboard');

  // ── HITL Review ──────────────────────────────────────────────────────
  pendingReviews = signal<HitlReviewItem[]>([
    {
      documentId: 'demo-001',
      fileName: 'research-paper-sample.pdf',
      overallConfidence: 0.18,
      isResolved: false,
      flaggedItems: [
        {
          field: 'Extraction',
          agentResult: 'Extraction confidence 18% is below HITL threshold',
          confidence: 0.18,
          humanCorrection: null,
        },
        {
          field: 'Plagiarism',
          agentResult: 'Similarity: 28.4% – exceeds 25% threshold',
          confidence: 0.72,
          humanCorrection: null,
        },
      ],
    },
  ]);
  correctionInputs = signal<Record<string, string>>({});
  hitlMessage = signal<string | null>(null);

  // ── Audit Log ────────────────────────────────────────────────────────
  auditLog = signal<AuditEntry[]>([
    {
      id: '1',
      documentId: 'demo-001',
      action: 'Pipeline Started',
      actor: 'system',
      details: 'Full 11-agent pipeline initiated',
      timestamp: new Date().toISOString(),
    },
    {
      id: '2',
      documentId: 'demo-001',
      action: 'HITL Flagged',
      actor: 'system',
      details: 'Confidence below 25% — pending admin review',
      timestamp: new Date().toISOString(),
    },
  ]);

  // ── Q&A Chat ─────────────────────────────────────────────────────────
  qnaDocumentId = signal('');
  qnaQuestion = signal('');
  qnaHistory = signal<{ role: 'user' | 'agent'; text: string }[]>([]);
  qnaLoading = signal(false);

  constructor(
    public auth: AuthService,
    private apiService: ApiService,
  ) {}

  setTab(tab: 'dashboard' | 'hitl' | 'audit' | 'qna'): void {
    this.activeTab.set(tab);
  }

  getCorrectionInput(documentId: string, field: string): string {
    return this.correctionInputs()[`${documentId}::${field}`] ?? '';
  }

  setCorrectionInput(documentId: string, field: string, value: string): void {
    this.correctionInputs.update((m) => ({ ...m, [`${documentId}::${field}`]: value }));
  }

  submitCorrection(review: HitlReviewItem, item: FlaggedItem): void {
    const correction = this.getCorrectionInput(review.documentId, item.field);
    if (!correction.trim()) return;

    // TODO: call this.apiService.submitHitlCorrection(review.documentId, item.field, correction)
    this.auditLog.update((log) => [
      ...log,
      {
        id: Date.now().toString(),
        documentId: review.documentId,
        action: 'Admin Correction',
        actor: this.auth.currentUser() ?? 'admin',
        details: `Field: ${item.field} → "${correction}"`,
        timestamp: new Date().toISOString(),
      },
    ]);

    this.pendingReviews.update((reviews) =>
      reviews.map((r) =>
        r.documentId === review.documentId
          ? {
              ...r,
              flaggedItems: r.flaggedItems.map((f) =>
                f.field === item.field ? { ...f, humanCorrection: correction } : f,
              ),
              isResolved: r.flaggedItems.every((f) => f.humanCorrection || f.field === item.field),
            }
          : r,
      ),
    );
    this.hitlMessage.set(`Correction saved for field "${item.field}".`);
    setTimeout(() => this.hitlMessage.set(null), 3000);
  }

  askQuestion(): void {
    const docId = this.qnaDocumentId().trim();
    const question = this.qnaQuestion().trim();
    if (!docId || !question) return;

    this.qnaHistory.update((h) => [...h, { role: 'user', text: question }]);
    this.qnaQuestion.set('');
    this.qnaLoading.set(true);

    // TODO: call this.apiService.askQuestion(docId, question) once Q&A Agent is implemented
    setTimeout(() => {
      this.qnaHistory.update((h) => [
        ...h,
        {
          role: 'agent',
          text: `[Stub] Answer to "${question}" will be retrieved via RAG from the vector store for document ${docId}.`,
        },
      ]);
      this.qnaLoading.set(false);
    }, 800);
  }
}
