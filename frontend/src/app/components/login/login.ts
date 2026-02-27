import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService, UserRole } from '../../services/auth.service';
import { Component, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class LoginComponent implements OnInit {
  username = '';
  password = '';
  selectedRole = signal<UserRole>('user');
  errorMessage = signal('');

  constructor(
    private auth: AuthService,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const role = this.route.snapshot.queryParamMap.get('role');
    if (role === 'admin' || role === 'user') {
      this.selectedRole.set(role);
    }
  }

  onRoleChange(role: UserRole): void {
    this.selectedRole.set(role);
    this.username = '';
    this.password = '';
    this.errorMessage.set('');
  }

  onSubmit(): void {
    const success = this.auth.loginAsRole(this.username.trim(), this.password, this.selectedRole());
    if (!success) {
      const label = this.selectedRole() === 'admin' ? 'admin' : 'user';
      this.errorMessage.set(`Invalid ${label} credentials.`);
    }
  }
}
