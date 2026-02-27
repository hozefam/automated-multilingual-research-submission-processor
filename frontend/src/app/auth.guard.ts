import { AuthService, UserRole } from './services/auth.service';
import { CanActivateFn, Router } from '@angular/router';

import { inject } from '@angular/core';

export const authGuard = (role: UserRole): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.hasRole(role)) {
      return true;
    }

    router.navigate(['/']);
    return false;
  };
};
