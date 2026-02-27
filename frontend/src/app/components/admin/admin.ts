import { ApiService, DocumentSummary } from '../../services/api.service';
import { Component, OnInit, computed, signal } from '@angular/core';

import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HealthStatusComponent } from '../health-status/health-status';

@Component({
  selector: 'app-admin',
  imports: [CommonModule, FormsModule, HealthStatusComponent],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class AdminComponent implements OnInit {
  documents = signal<DocumentSummary[]>([]);
  loading = signal(false);
  actionMessage = signal<{ text: string; type: 'success' | 'error' } | null>(null);
  rejectionReasons = signal<Record<string, string>>({});
  processingIds = signal<Set<string>>(new Set());

  // Documents that require a human decision and haven't had one yet
  awaitingReview = computed(() =>
    this.documents().filter((d) => d.requiresReview && !d.reviewDecision),
  );

  // Documents that either passed the pipeline clean, or have been actioned by an admin
  processed = computed(() =>
    this.documents().filter((d) => !d.requiresReview || !!d.reviewDecision),
  );

  constructor(
    public auth: AuthService,
    private apiService: ApiService,
  ) {}

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading.set(true);
    this.apiService.getDocuments().subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => {
        this.showMessage('Failed to load documents. Is the backend running?', 'error');
        this.loading.set(false);
      },
    });
  }

  getRejectionReason(documentId: string): string {
    return this.rejectionReasons()[documentId] ?? '';
  }

  setRejectionReason(documentId: string, reason: string): void {
    this.rejectionReasons.update((r) => ({ ...r, [documentId]: reason }));
  }

  approve(doc: DocumentSummary): void {
    this.setProcessing(doc.documentId, true);
    this.apiService.submitReview(doc.documentId, true).subscribe({
      next: () => {
        this.showMessage(`"${doc.fileName}" approved successfully.`, 'success');
        this.loadDocuments();
        this.setProcessing(doc.documentId, false);
      },
      error: () => {
        this.showMessage('Failed to submit approval.', 'error');
        this.setProcessing(doc.documentId, false);
      },
    });
  }

  reject(doc: DocumentSummary): void {
    const reason = this.getRejectionReason(doc.documentId).trim();
    if (!reason) {
      this.showMessage('Please enter a rejection reason before rejecting.', 'error');
      return;
    }
    this.setProcessing(doc.documentId, true);
    this.apiService.submitReview(doc.documentId, false, reason).subscribe({
      next: () => {
        this.showMessage(`"${doc.fileName}" rejected.`, 'success');
        this.rejectionReasons.update((r) => {
          const next = { ...r };
          delete next[doc.documentId];
          return next;
        });
        this.loadDocuments();
        this.setProcessing(doc.documentId, false);
      },
      error: () => {
        this.showMessage('Failed to submit rejection.', 'error');
        this.setProcessing(doc.documentId, false);
      },
    });
  }

  isProcessing(documentId: string): boolean {
    return this.processingIds().has(documentId);
  }

  private setProcessing(documentId: string, state: boolean): void {
    this.processingIds.update((set) => {
      const next = new Set(set);
      state ? next.add(documentId) : next.delete(documentId);
      return next;
    });
  }

  private showMessage(text: string, type: 'success' | 'error'): void {
    this.actionMessage.set({ text, type });
    setTimeout(() => this.actionMessage.set(null), 4000);
  }
}
