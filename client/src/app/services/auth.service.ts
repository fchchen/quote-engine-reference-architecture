import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { LoginRequest, AuthResponse } from '../models/auth.model';

const STORAGE_KEY = 'auth_response';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = environment.apiUrl;

  // State
  authResponse = signal<AuthResponse | null>(null);
  error = signal<string | null>(null);
  isLoading = signal(false);

  // Computed
  isAuthenticated = computed(() => {
    const auth = this.authResponse();
    if (!auth) return false;
    return new Date(auth.expiration) > new Date();
  });

  currentUser = computed(() => this.authResponse()?.username ?? null);
  currentRole = computed(() => this.authResponse()?.role ?? null);
  token = computed(() => this.authResponse()?.token ?? null);

  constructor() {
    this.hydrateFromStorage();
  }

  loginDemo(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/demo`, {}).pipe(
      tap(response => {
        this.setAuth(response);
        this.isLoading.set(false);
        this.router.navigate(['/quote']);
      }),
      catchError(err => {
        this.error.set(err.message || 'Demo login failed');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  login(request: LoginRequest): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, request).pipe(
      tap(response => {
        this.setAuth(response);
        this.isLoading.set(false);
        const returnUrl = this.router.parseUrl(this.router.url).queryParams['returnUrl'] || '/quote';
        this.router.navigate([returnUrl]);
      }),
      catchError(err => {
        this.error.set(err.message || 'Invalid username or password');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  logout(): void {
    this.authResponse.set(null);
    this.error.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.token();
  }

  private setAuth(response: AuthResponse): void {
    this.authResponse.set(response);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
  }

  private hydrateFromStorage(): void {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return;

    try {
      const auth: AuthResponse = JSON.parse(stored);
      if (new Date(auth.expiration) > new Date()) {
        this.authResponse.set(auth);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
