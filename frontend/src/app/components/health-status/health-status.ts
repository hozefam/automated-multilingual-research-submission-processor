import { ApiService, HealthResponse } from '../../services/api.service';
import { Component, OnInit, signal } from '@angular/core';

type StatusState = 'checking' | 'healthy' | 'unreachable';

@Component({
  selector: 'app-health-status',
  templateUrl: './health-status.html',
  styleUrl: './health-status.css',
})
export class HealthStatusComponent implements OnInit {
  state = signal<StatusState>('checking');
  version = signal<string>('');

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.checkHealth();
  }

  checkHealth(): void {
    this.state.set('checking');
    this.api.getHealth().subscribe({
      next: (res: HealthResponse) => {
        this.version.set(res.version);
        this.state.set('healthy');
      },
      error: () => {
        this.state.set('unreachable');
      },
    });
  }
}
