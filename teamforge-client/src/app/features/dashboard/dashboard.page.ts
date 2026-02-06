import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { ThemedToolbarComponent } from '../../shared/components/themed-toolbar.component';

interface DashboardStats {
  totalProjects: number;
  totalTeams: number;
  totalUsers: number;
  activeProjects: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule,
    ThemedToolbarComponent
  ],
  template: `
    <app-themed-toolbar></app-themed-toolbar>
    <div class="page-container">
      <div class="page-header">
        <h1>Dashboard</h1>
      </div>
      @if (isLoading()) {
        <div class="loading-spinner">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      } @else {
        <div class="stats-grid">
          <mat-card>
            <mat-card-content class="stat-card">
              <mat-icon>folder</mat-icon>
              <div class="stat-value">{{ stats()?.totalProjects ?? 0 }}</div>
              <div class="stat-label">Projects</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content class="stat-card">
              <mat-icon>group</mat-icon>
              <div class="stat-value">{{ stats()?.totalTeams ?? 0 }}</div>
              <div class="stat-label">Teams</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content class="stat-card">
              <mat-icon>person</mat-icon>
              <div class="stat-value">{{ stats()?.totalUsers ?? 0 }}</div>
              <div class="stat-label">Users</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content class="stat-card">
              <mat-icon>check_circle</mat-icon>
              <div class="stat-value">{{ stats()?.activeProjects ?? 0 }}</div>
              <div class="stat-label">Active</div>
            </mat-card-content>
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [`
    .stats-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; }
    .stat-card { text-align: center; padding: 24px !important; }
    .stat-card mat-icon { font-size: 36px; width: 36px; height: 36px; color: var(--tf-primary, #3f51b5); }
    .stat-value { font-size: 2rem; font-weight: 500; margin: 8px 0 4px; }
    .stat-label { color: rgba(0,0,0,0.6); }
  `]
})
export class DashboardPage implements OnInit {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  authService = inject(AuthService);
  themeService = inject(ThemeService);

  stats = signal<DashboardStats | null>(null);
  isLoading = signal(true);

  ngOnInit(): void {
    this.themeService.loadBranding();
    this.http.get<DashboardStats>(`${this.apiUrl}/dashboard`).pipe(
      tap(stats => {
        this.stats.set(stats);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }
}
