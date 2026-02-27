import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'admin',
    loadComponent: () => import('./components/admin/admin').then((m) => m.AdminComponent),
    canActivate: [authGuard('admin')],
  },
  {
    path: 'user',
    loadComponent: () => import('./components/user/user').then((m) => m.UserComponent),
    canActivate: [authGuard('user')],
  },
  { path: '**', redirectTo: 'login' },
];
