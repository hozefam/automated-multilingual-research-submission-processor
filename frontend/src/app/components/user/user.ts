import {
  ApiService,
  DocumentSummary,
  PipelineResult,
  PipelineStepMeta,
  StepResult,
} from '../../services/api.service';
import { Component, OnInit, computed, signal } from '@angular/core';

import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { HealthStatusComponent } from '../health-status/health-status';

export type StepState = 'pending' | 'active' | 'done' | 'error';
export type UploadStatus = 'idle' | 'processing' | 'done' | 'error';

export interface PipelineStep {
  id: number;
  icon: string;
  title: string;
  description: string;
  state: StepState;
  detail?: string;
}

@Component({
  selector: 'app-user',
  imports: [CommonModule, HealthStatusComponent],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserComponent implements OnInit {
  selectedFile = signal<File | null>(null);
  uploadStatus = signal<UploadStatus>('idle');
  isDragOver = signal(false);
  currentStepIndex = signal(-1);
  apiError = signal<string | null>(null);
  totalElapsedMs = signal<number | null>(null);

  steps = signal<PipelineStep[]>([]);
  mySubmissions = signal<DocumentSummary[]>([]);
  submissionsLoading = signal(false);
  activeTab = signal<'upload' | 'submissions'>('upload');

  completedSteps = computed(() => this.steps().filter((s) => s.state === 'done').length);
  progressPercent = computed(() => Math.round((this.completedSteps() / this.steps().length) * 100));

  constructor(
    public auth: AuthService,
    private apiService: ApiService,
  ) {}

  ngOnInit(): void {
    this.apiService.getPipelineSteps().subscribe({
      next: (stepsFromApi: PipelineStepMeta[]) => {
        this.steps.set(
          stepsFromApi.map((s) => ({
            id: s.id,
            icon: s.icon,
            title: s.name,
            description: s.description,
            state: 'pending' as StepState,
          })),
        );
      },
    });
    this.loadMySubmissions();
  }

  loadMySubmissions(): void {
    this.submissionsLoading.set(true);
    this.apiService.getDocuments().subscribe({
      next: (docs) => {
        this.mySubmissions.set(docs);
        this.submissionsLoading.set(false);
      },
      error: () => this.submissionsLoading.set(false),
    });
  }

  switchTab(tab: 'upload' | 'submissions'): void {
    this.activeTab.set(tab);
    if (tab === 'submissions') this.loadMySubmissions();
  }

  reviewStatusLabel(doc: DocumentSummary): 'awaiting' | 'approved' | 'rejected' | 'auto' {
    if (!doc.requiresReview) return 'auto';
    if (!doc.reviewDecision) return 'awaiting';
    return doc.reviewDecision.approved ? 'approved' : 'rejected';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.setFile(input.files[0]);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(): void {
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file?.type === 'application/pdf') this.setFile(file);
  }

  private setFile(file: File): void {
    if (file.type !== 'application/pdf') return;
    this.selectedFile.set(file);
    this.uploadStatus.set('idle');
    this.resetSteps();
  }

  private resetSteps(): void {
    this.currentStepIndex.set(-1);
    this.steps.update((steps) =>
      steps.map((s) => ({ ...s, state: 'pending' as StepState, detail: undefined })),
    );
  }

  startProcessing(): void {
    const file = this.selectedFile();
    if (!file) return;
    this.uploadStatus.set('processing');
    this.apiError.set(null);
    this.totalElapsedMs.set(null);
    this.resetSteps();

    // Mark first step active immediately for instant feedback
    this.setStepState(0, 'active');

    this.apiService.uploadDocument(file).subscribe({
      next: (result) => this.animateResults(result),
      error: (err) => {
        this.apiError.set(
          err?.error?.error ?? err?.message ?? 'Upload failed. Is the backend running?',
        );
        this.uploadStatus.set('error');
        // Mark all pending/active steps as error
        this.steps.update((steps) =>
          steps.map((s) =>
            s.state === 'active' || s.state === 'pending'
              ? { ...s, state: 'error' as StepState }
              : s,
          ),
        );
      },
    });
  }

  private animateResults(result: PipelineResult): void {
    this.totalElapsedMs.set(result.totalElapsedMs);
    const stepResults: StepResult[] = [
      result.ingestion,
      result.preProcess,
      result.translation,
      result.extraction,
      result.validation,
      result.contentSafety,
      result.plagiarism,
      result.ragIndex,
      result.summarization,
      result.qnA,
      result.humanFeedback,
    ];

    const STEP_DELAY = 350; // ms between each step animation
    stepResults.forEach((stepResult, i) => {
      // Activate step
      setTimeout(() => this.setStepState(i, 'active'), i * STEP_DELAY * 2);
      // Resolve step to done/error
      setTimeout(
        () =>
          this.setStepState(
            i,
            stepResult.success ? 'done' : 'error',
            stepResult.error ?? undefined,
          ),
        i * STEP_DELAY * 2 + STEP_DELAY,
      );
    });

    // Mark overall status after all animations, then switch to My Submissions tab
    setTimeout(
      () => {
        this.uploadStatus.set('done');
        this.loadMySubmissions();
      },
      stepResults.length * STEP_DELAY * 2 + STEP_DELAY,
    );
  }

  private setStepState(index: number, state: StepState, detail?: string): void {
    this.currentStepIndex.set(index);
    this.steps.update((steps) => steps.map((s, i) => (i === index ? { ...s, state, detail } : s)));
  }

  reset(): void {
    this.selectedFile.set(null);
    this.uploadStatus.set('idle');
    this.apiError.set(null);
    this.totalElapsedMs.set(null);
    this.resetSteps();
  }

  formatFileSize(bytes: number): string {
    return bytes < 1024 * 1024
      ? `${(bytes / 1024).toFixed(1)} KB`
      : `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }
}
