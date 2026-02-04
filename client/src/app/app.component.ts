import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { LoadingService } from './services/loading.service';
import { MatProgressBarModule } from '@angular/material/progress-bar';

/**
 * Root application component.
 *
 * INTERVIEW TALKING POINTS:
 * - Standalone component (no NgModule)
 * - Material design layout
 * - Global loading indicator using signals
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    MatProgressBarModule
  ],
  template: `
    <mat-toolbar color="primary" class="app-toolbar">
      <button mat-icon-button (click)="sidenav.toggle()">
        <mat-icon>menu</mat-icon>
      </button>
      <span class="app-title">Quote Engine</span>
      <span class="spacer"></span>
      <button mat-button routerLink="/quote">
        <mat-icon>add</mat-icon>
        New Quote
      </button>
    </mat-toolbar>

    @if (loadingService.isLoading()) {
      <mat-progress-bar mode="indeterminate" class="global-progress"></mat-progress-bar>
    }

    <mat-sidenav-container class="sidenav-container">
      <mat-sidenav #sidenav mode="over">
        <mat-nav-list>
          <a mat-list-item routerLink="/quote" routerLinkActive="active"
             (click)="sidenav.close()">
            <mat-icon matListItemIcon>add_circle</mat-icon>
            <span matListItemTitle>New Quote</span>
          </a>
          <a mat-list-item routerLink="/history" routerLinkActive="active"
             (click)="sidenav.close()">
            <mat-icon matListItemIcon>history</mat-icon>
            <span matListItemTitle>Quote History</span>
          </a>
          <mat-divider></mat-divider>
          <a mat-list-item routerLink="/about" routerLinkActive="active"
             (click)="sidenav.close()">
            <mat-icon matListItemIcon>info</mat-icon>
            <span matListItemTitle>About</span>
          </a>
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content class="main-content">
        <div class="app-container">
          <router-outlet></router-outlet>
        </div>
      </mat-sidenav-content>
    </mat-sidenav-container>

    <footer class="app-footer">
      <small>Quote Engine Reference Architecture | Built with Angular 17 & .NET 8</small>
    </footer>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }

    .app-toolbar {
      position: sticky;
      top: 0;
      z-index: 100;
    }

    .app-title {
      margin-left: 8px;
      font-weight: 500;
    }

    .spacer {
      flex: 1;
    }

    .global-progress {
      position: fixed;
      top: 64px;
      left: 0;
      right: 0;
      z-index: 99;
    }

    .sidenav-container {
      flex: 1;
    }

    mat-sidenav {
      width: 250px;
    }

    .main-content {
      min-height: calc(100vh - 64px - 48px);
    }

    .app-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .app-footer {
      padding: 12px;
      text-align: center;
      background-color: #f5f5f5;
      color: rgba(0, 0, 0, 0.54);
    }

    .active {
      background-color: rgba(0, 0, 0, 0.04);
    }
  `]
})
export class AppComponent {
  loadingService = inject(LoadingService);
}
