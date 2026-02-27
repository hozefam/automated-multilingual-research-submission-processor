import { Injectable, signal } from '@angular/core';

import { Router } from '@angular/router';

export type UserRole = 'admin' | 'user';

interface Credentials {
  username: string;
  password: string;
  role: UserRole;
}

const STATIC_USERS: Credentials[] = [
  { username: 'admin', password: 'admin', role: 'admin' },
  { username: 'user', password: 'user', role: 'user' },
];

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _currentRole = signal<UserRole | null>(null);
  private _currentUser = signal<string | null>(null);

  readonly currentRole = this._currentRole.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();

  constructor(private router: Router) {}

  login(username: string, password: string): boolean {
    const match = STATIC_USERS.find((u) => u.username === username && u.password === password);
    if (match) {
      this._currentRole.set(match.role);
      this._currentUser.set(match.username);
      this.router.navigate([`/${match.role}`]);
      return true;
    }
    return false;
  }

  loginAsRole(username: string, password: string, role: UserRole): boolean {
    const match = STATIC_USERS.find(
      (u) => u.username === username && u.password === password && u.role === role,
    );
    if (match) {
      this._currentRole.set(match.role);
      this._currentUser.set(match.username);
      this.router.navigate([`/${match.role}`]);
      return true;
    }
    return false;
  }

  logout(): void {
    this._currentRole.set(null);
    this._currentUser.set(null);
    this.router.navigate(['/']);
  }

  isLoggedIn(): boolean {
    return this._currentRole() !== null;
  }

  hasRole(role: UserRole): boolean {
    return this._currentRole() === role;
  }
}
