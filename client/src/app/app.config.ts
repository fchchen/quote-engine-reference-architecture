import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { httpErrorInterceptor, loadingInterceptor } from './interceptors/http-error.interceptor';

/**
 * Application configuration (replaces NgModule).
 *
 * INTERVIEW TALKING POINTS:
 * - Standalone configuration (Angular 15+)
 * - Functional HTTP interceptors
 * - Zone change detection optimization
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([httpErrorInterceptor, loadingInterceptor])
    ),
    provideAnimationsAsync()
  ]
};
