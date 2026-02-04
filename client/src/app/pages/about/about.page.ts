import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

/**
 * About page with architecture information.
 */
@Component({
  selector: 'app-about-page',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatIconModule, MatChipsModule],
  template: `
    <div class="about-page">
      <h1>About Quote Engine</h1>

      <mat-card>
        <mat-card-header>
          <mat-icon mat-card-avatar>architecture</mat-icon>
          <mat-card-title>Reference Architecture</mat-card-title>
          <mat-card-subtitle>
            Commercial Insurance Quoting Platform
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <p>
            This is a reference architecture demonstrating best practices for
            building scalable, real-time commercial quoting platforms using
            modern web technologies.
          </p>

          <h3>Technologies Used</h3>
          <div class="tech-chips">
            <mat-chip-set>
              <mat-chip>Angular 17</mat-chip>
              <mat-chip>.NET 8</mat-chip>
              <mat-chip>SQL Server</mat-chip>
              <mat-chip>Azure Functions</mat-chip>
              <mat-chip>Entity Framework Core</mat-chip>
            </mat-chip-set>
          </div>

          <h3>Key Features</h3>
          <mat-list>
            <mat-list-item>
              <mat-icon matListItemIcon>check_circle</mat-icon>
              <span matListItemTitle>Signal-First State Management</span>
              <span matListItemLine>Angular Signals for reactive UI</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>check_circle</mat-icon>
              <span matListItemTitle>RESTful API Design</span>
              <span matListItemLine>Versioned endpoints with proper status codes</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>check_circle</mat-icon>
              <span matListItemTitle>Dependency Injection</span>
              <span matListItemLine>Loose coupling and testability</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>check_circle</mat-icon>
              <span matListItemTitle>Real-Time Premium Calculation</span>
              <span matListItemLine>Instant quote generation</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>check_circle</mat-icon>
              <span matListItemTitle>Serverless Azure Functions</span>
              <span matListItemLine>Free tier deployment option</span>
            </mat-list-item>
          </mat-list>

          <h3>Architecture Patterns</h3>
          <mat-list>
            <mat-list-item>
              <mat-icon matListItemIcon>layers</mat-icon>
              <span matListItemTitle>Clean Architecture</span>
              <span matListItemLine>Separation of concerns across layers</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>sync</mat-icon>
              <span matListItemTitle>Repository Pattern</span>
              <span matListItemLine>Abstracted data access</span>
            </mat-list-item>
            <mat-list-item>
              <mat-icon matListItemIcon>extension</mat-icon>
              <span matListItemTitle>Strategy Pattern</span>
              <span matListItemLine>Swappable implementations</span>
            </mat-list-item>
          </mat-list>
        </mat-card-content>
      </mat-card>

      <mat-card class="docs-card">
        <mat-card-header>
          <mat-icon mat-card-avatar>description</mat-icon>
          <mat-card-title>Documentation</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>
            For detailed documentation, see the /docs folder in the repository:
          </p>
          <ul>
            <li>architecture-overview.md - System design and data flow</li>
            <li>dotnet-patterns.md - .NET patterns and best practices</li>
            <li>sql-optimization.md - Database indexing and query tuning</li>
            <li>angular-patterns.md - Angular 17+ patterns</li>
            <li>azure-deployment.md - Deployment guide</li>
          </ul>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .about-page {
      padding: 20px 0;
      max-width: 800px;
      margin: 0 auto;
    }

    h1 {
      margin-bottom: 24px;
    }

    mat-card {
      margin-bottom: 24px;
    }

    h3 {
      margin: 24px 0 12px 0;
      color: rgba(0, 0, 0, 0.54);
    }

    .tech-chips {
      margin: 12px 0;
    }

    .docs-card ul {
      margin: 12px 0;
      padding-left: 24px;
    }

    .docs-card li {
      margin: 8px 0;
    }
  `]
})
export class AboutPage {}
