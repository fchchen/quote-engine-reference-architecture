import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError, finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Error Interceptor (Functional style for Angular 17+).
 *
 * INTERVIEW TALKING POINTS:
 * - Functional interceptor (new in Angular 15+)
 * - Global error handling
 * - Transforms error responses for consistent handling
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unknown error occurred';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Client error: ${error.error.message}`;
      } else {
        // Server-side error
        switch (error.status) {
          case 400:
            errorMessage = error.error?.detail || error.error?.title || 'Invalid request';
            break;
          case 401:
            errorMessage = 'Unauthorized - please log in';
            // Clear stale token and redirect to login
            if (!req.url.includes('/auth/')) {
              const authService = inject(AuthService);
              authService.logout();
            }
            break;
          case 403:
            errorMessage = 'Access denied';
            break;
          case 404:
            errorMessage = error.error?.detail || 'Resource not found';
            break;
          case 422:
            errorMessage = error.error?.detail || 'Validation failed';
            break;
          case 500:
            errorMessage = 'Server error - please try again later';
            break;
          case 503:
            errorMessage = 'Service unavailable - please try again later';
            break;
          default:
            errorMessage = `Error ${error.status}: ${error.statusText}`;
        }
      }

      // Log error for debugging
      console.error('HTTP Error:', {
        url: req.url,
        status: error.status,
        message: errorMessage,
        error: error.error
      });

      // Return a custom error object
      return throwError(() => ({
        status: error.status,
        message: errorMessage,
        originalError: error.error
      }));
    })
  );
};

/**
 * Loading interceptor - adds loading state for HTTP requests.
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Determine loading key based on URL
  let loadingKey = 'default';
  if (req.url.includes('/quote')) {
    loadingKey = 'quote';
  } else if (req.url.includes('/business/search')) {
    loadingKey = 'search';
  } else if (req.url.includes('/ratetable')) {
    loadingKey = 'referenceData';
  }

  loadingService.startLoading(loadingKey);

  return next(req).pipe(
    finalize(() => loadingService.stopLoading(loadingKey))
  );
};
