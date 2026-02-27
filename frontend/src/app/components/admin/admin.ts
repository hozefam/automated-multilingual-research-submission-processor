import { AuthService } from '../../services/auth.service';
import { Component } from '@angular/core';
import { HealthStatusComponent } from '../health-status/health-status';

@Component({
  selector: 'app-admin',
  imports: [HealthStatusComponent],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class AdminComponent {
  constructor(public auth: AuthService) {}
}
