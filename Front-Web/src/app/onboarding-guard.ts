import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  Router,
  UrlTree,
} from '@angular/router';
import { AuthService } from '../services/AuthService';

/**
 * Enforces:
 * register/login → add-device (if no devices)
 *              → add-sensors (if devices, no sensors)
 *              → dashboard (if both)
 */
@Injectable({ providedIn: 'root' })
export class OnboardingGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    if (!this.authService.isLoggedIn()) {
      return this.router.createUrlTree(['/login']);
    }

    const path = route.routeConfig?.path ?? '';
    const { hasDevices, hasSensors } = this.authService.getOnboardingState();
    const target = this.authService.resolvePostLoginRoute();

    if (path === 'dashboard' || path?.startsWith('dashboard')) {
      if (hasDevices && hasSensors) {
        return true;
      }

      return this.router.createUrlTree([target]);
    }

    if (path === 'add-device') {
      // Mid-onboarding: already have a device but still need sensors.
      if (hasDevices && !hasSensors) {
        return this.router.createUrlTree(['/add-sensors']);
      }

      // New users (no device) or fully onboarded users adding another device.
      return true;
    }

    if (path === 'add-sensors') {
      if (!hasDevices) {
        return this.router.createUrlTree(['/add-device']);
      }

      return true;
    }

    return true;
  }
}
