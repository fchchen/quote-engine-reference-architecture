import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ThemedToolbarComponent } from '../../shared/components/themed-toolbar.component';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    ThemedToolbarComponent
  ],
  template: `
    <app-themed-toolbar></app-themed-toolbar>
    <div class="page-container">
      <div class="page-header">
        <h1>Welcome! Let's set up your workspace</h1>
      </div>
      <mat-card>
        <mat-card-content>
          @if (isLoading()) {
            <div class="loading-spinner">
              <mat-spinner diameter="40"></mat-spinner>
              <p>Setting up your workspace...</p>
            </div>
          } @else if (isComplete()) {
            <div class="complete-message">
              <mat-icon>check_circle</mat-icon>
              <h2>All set!</h2>
              <p>Your workspace has been configured.</p>
              <button mat-raised-button color="primary" (click)="goToDashboard()">Go to Dashboard</button>
            </div>
          } @else {
            <div class="start-message">
              <p>We'll configure your workspace with default projects, teams, and settings.</p>
              <button mat-raised-button color="primary" (click)="startOnboarding()">Get Started</button>
              <button mat-button (click)="goToDashboard()">Skip</button>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .loading-spinner, .complete-message, .start-message { text-align: center; padding: 32px; }
    .complete-message mat-icon { font-size: 48px; width: 48px; height: 48px; color: #4caf50; }
  `]
})
export class OnboardingPage {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = environment.apiUrl;

  isLoading = signal(false);
  isComplete = signal(false);

  startOnboarding(): void {
    this.isLoading.set(true);
    this.http.post(`${this.apiUrl}/onboarding/start`, {}).pipe(
      tap(() => {
        this.isLoading.set(false);
        this.isComplete.set(true);
      }),
      catchError(() => {
        this.isLoading.set(false);
        this.isComplete.set(true);
        return of(null);
      })
    ).subscribe();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
