import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
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

const STEP_DURATIONS_MS = [1200, 900, 1500, 2000, 1800, 1600, 800];

@Component({
  selector: 'app-user',
  imports: [CommonModule, HealthStatusComponent],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserComponent {
  selectedFile = signal<File | null>(null);
  uploadStatus = signal<UploadStatus>('idle');
  isDragOver = signal(false);
  currentStepIndex = signal(-1);

  steps = signal<PipelineStep[]>([
    {
      id: 1,
      icon: '🧠',
      title: 'Metadata Extraction',
      description: 'Extracting title, authors, abstract, keywords and document structure.',
      state: 'pending',
    },
    {
      id: 2,
      icon: '🌐',
      title: 'Language Detection',
      description: 'Identifying primary language and detecting multilingual sections.',
      state: 'pending',
    },
    {
      id: 3,
      icon: '🛡️',
      title: 'Content Safety Check',
      description: 'Scanning content for policy violations and ethical concerns.',
      state: 'pending',
    },
    {
      id: 4,
      icon: '🔍',
      title: 'Plagiarism Detection',
      description: 'Cross-referencing against academic databases for similarity.',
      state: 'pending',
    },
    {
      id: 5,
      icon: '📚',
      title: 'RAG Indexing',
      description: 'Chunking and indexing document into the vector knowledge base.',
      state: 'pending',
    },
    {
      id: 6,
      icon: '✨',
      title: 'AI Summarization',
      description: 'Generating structured summary and key findings using LLM.',
      state: 'pending',
    },
    {
      id: 7,
      icon: '💬',
      title: 'Q&A System',
      description: 'Enabling interactive Q&A on the document via RAG pipeline.',
      state: 'pending',
    },
  ]);

  completedSteps = computed(() => this.steps().filter((s) => s.state === 'done').length);
  progressPercent = computed(() => Math.round((this.completedSteps() / this.steps().length) * 100));

  constructor(public auth: AuthService) {}

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
    if (!this.selectedFile()) return;
    this.uploadStatus.set('processing');
    this.resetSteps();
    this.runStep(0);
  }

  private runStep(index: number): void {
    if (index >= this.steps().length) {
      this.uploadStatus.set('done');
      return;
    }

    this.currentStepIndex.set(index);
    this.steps.update((steps) =>
      steps.map((s, i) => (i === index ? { ...s, state: 'active' as StepState } : s)),
    );

    setTimeout(() => {
      this.steps.update((steps) =>
        steps.map((s, i) => (i === index ? { ...s, state: 'done' as StepState } : s)),
      );
      this.runStep(index + 1);
    }, STEP_DURATIONS_MS[index]);
  }

  reset(): void {
    this.selectedFile.set(null);
    this.uploadStatus.set('idle');
    this.resetSteps();
  }

  formatFileSize(bytes: number): string {
    return bytes < 1024 * 1024
      ? `${(bytes / 1024).toFixed(1)} KB`
      : `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }
}
