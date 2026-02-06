import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TenantBranding {
  tenantId: string;
  companyName: string;
  tagLine: string;
  logoUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  backgroundColor: string;
  textColor: string;
  fontFamily: string;
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  branding = signal<TenantBranding | null>(null);
  isLoading = signal(false);

  companyName = computed(() => this.branding()?.companyName ?? 'TeamForge');
  tagLine = computed(() => this.branding()?.tagLine ?? '');
  logoUrl = computed(() => this.branding()?.logoUrl ?? null);

  loadBranding(): void {
    this.isLoading.set(true);
    this.http.get<TenantBranding>(`${this.apiUrl}/branding`).pipe(
      tap(branding => {
        this.branding.set(branding);
        this.applyTheme(branding);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  applyTheme(branding: TenantBranding): void {
    const root = document.documentElement;
    root.style.setProperty('--tf-primary', branding.primaryColor);
    root.style.setProperty('--tf-secondary', branding.secondaryColor);
    root.style.setProperty('--tf-accent', branding.accentColor);
    root.style.setProperty('--tf-background', branding.backgroundColor);
    root.style.setProperty('--tf-text', branding.textColor);
    root.style.setProperty('--tf-font-family', `'${branding.fontFamily}', sans-serif`);
  }

  updateBranding(updates: Partial<TenantBranding>): void {
    this.http.put<TenantBranding>(`${this.apiUrl}/branding`, updates).pipe(
      tap(branding => {
        this.branding.set(branding);
        this.applyTheme(branding);
      }),
      catchError(() => of(null))
    ).subscribe();
  }
}
