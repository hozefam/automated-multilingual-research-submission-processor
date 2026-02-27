import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class LoginComponent {
  username = '';
  password = '';
  errorMessage = signal('');

  constructor(private auth: AuthService) {}

  onSubmit(): void {
    const success = this.auth.login(this.username.trim(), this.password);
    if (!success) {
      this.errorMessage.set('Invalid username or password.');
    }
  }
}
