import { AuthService } from '../../services/auth.service';
import { Component } from '@angular/core';
import { HealthStatusComponent } from '../health-status/health-status';

@Component({
  selector: 'app-user',
  imports: [HealthStatusComponent],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserComponent {
  constructor(public auth: AuthService) {}
}
