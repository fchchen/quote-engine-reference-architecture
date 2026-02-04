import { Component, input, output, computed } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { QuoteResponse } from '../../models/quote.model';

/**
 * Quote history table component.
 *
 * INTERVIEW TALKING POINTS:
 * - Uses signal-based input() for data binding
 * - computed() for sorted/filtered data
 * - Material table with sort and pagination
 */
@Component({
  selector: 'app-quote-history',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  template: `
    <div class="quote-history-container">
      <h2>Quote History</h2>

      @if (history().length === 0) {
        <p class="no-quotes">No quotes yet. Start by creating a new quote.</p>
      } @else {
        <table mat-table [dataSource]="displayedQuotes()" matSort (matSortChange)="onSort($event)">
          <!-- Quote Number Column -->
          <ng-container matColumnDef="quoteNumber">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Quote #</th>
            <td mat-cell *matCellDef="let quote">{{ quote.quoteNumber }}</td>
          </ng-container>

          <!-- Date Column -->
          <ng-container matColumnDef="quoteDate">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Date</th>
            <td mat-cell *matCellDef="let quote">{{ quote.quoteDate | date:'short' }}</td>
          </ng-container>

          <!-- Business Name Column -->
          <ng-container matColumnDef="businessName">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Business</th>
            <td mat-cell *matCellDef="let quote">{{ quote.businessName }}</td>
          </ng-container>

          <!-- Product Column -->
          <ng-container matColumnDef="productType">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Product</th>
            <td mat-cell *matCellDef="let quote">
              {{ formatProductType(quote.productType) }}
            </td>
          </ng-container>

          <!-- Premium Column -->
          <ng-container matColumnDef="premium">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Premium</th>
            <td mat-cell *matCellDef="let quote" class="currency">
              @if (quote.isEligible) {
                {{ quote.premium.annualPremium | currency }}
              } @else {
                -
              }
            </td>
          </ng-container>

          <!-- Status Column -->
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
            <td mat-cell *matCellDef="let quote">
              <span class="status-badge" [class]="'status-' + quote.status.toLowerCase()">
                {{ quote.status }}
              </span>
            </td>
          </ng-container>

          <!-- Actions Column -->
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Actions</th>
            <td mat-cell *matCellDef="let quote">
              <button mat-icon-button (click)="onView(quote)" title="View">
                <mat-icon>visibility</mat-icon>
              </button>
              @if (quote.isEligible) {
                <button mat-icon-button (click)="onBind(quote)" title="Bind">
                  <mat-icon>check_circle</mat-icon>
                </button>
              }
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns;"
              (click)="onView(row)"
              class="clickable-row">
          </tr>
        </table>

        <mat-paginator
          [length]="history().length"
          [pageSize]="pageSize"
          [pageSizeOptions]="[5, 10, 25]"
          (page)="onPage($event)">
        </mat-paginator>

        <!-- Summary Stats -->
        <div class="summary-stats">
          <span>Total: {{ history().length }} quotes</span>
          <span>Quoted: {{ quotedCount() }}</span>
          <span>Total Premium: {{ totalPremium() | currency }}</span>
        </div>
      }
    </div>
  `,
  styles: [`
    .quote-history-container {
      margin: 20px 0;
    }

    h2 {
      margin-bottom: 16px;
    }

    .no-quotes {
      text-align: center;
      color: rgba(0, 0, 0, 0.54);
      padding: 40px;
    }

    table {
      width: 100%;
    }

    .clickable-row {
      cursor: pointer;
    }

    .clickable-row:hover {
      background-color: #f5f5f5;
    }

    .currency {
      font-family: 'Roboto Mono', monospace;
      text-align: right;
    }

    .summary-stats {
      display: flex;
      gap: 24px;
      padding: 16px;
      background-color: #f5f5f5;
      border-radius: 4px;
      margin-top: 16px;
      font-size: 0.875rem;
    }

    .summary-stats span {
      color: rgba(0, 0, 0, 0.54);
    }
  `]
})
export class QuoteHistoryComponent {
  // Signal-based input
  history = input<QuoteResponse[]>([]);

  // Signal-based outputs
  view = output<QuoteResponse>();
  bind = output<QuoteResponse>();

  // Table configuration
  displayedColumns = ['quoteNumber', 'quoteDate', 'businessName', 'productType', 'premium', 'status', 'actions'];
  pageSize = 10;
  pageIndex = 0;
  sortField = 'quoteDate';
  sortDirection: 'asc' | 'desc' = 'desc';

  // Computed values
  displayedQuotes = computed(() => {
    let quotes = [...this.history()];

    // Sort
    quotes.sort((a, b) => {
      let comparison = 0;
      const aVal = this.getSortValue(a, this.sortField);
      const bVal = this.getSortValue(b, this.sortField);

      if (aVal < bVal) comparison = -1;
      if (aVal > bVal) comparison = 1;

      return this.sortDirection === 'asc' ? comparison : -comparison;
    });

    // Paginate
    const start = this.pageIndex * this.pageSize;
    return quotes.slice(start, start + this.pageSize);
  });

  quotedCount = computed(() =>
    this.history().filter(q => q.status === 'Quoted').length
  );

  totalPremium = computed(() =>
    this.history()
      .filter(q => q.isEligible)
      .reduce((sum, q) => sum + q.premium.annualPremium, 0)
  );

  onSort(sort: Sort): void {
    this.sortField = sort.active;
    this.sortDirection = sort.direction as 'asc' | 'desc' || 'desc';
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
  }

  onView(quote: QuoteResponse): void {
    this.view.emit(quote);
  }

  onBind(quote: QuoteResponse): void {
    this.bind.emit(quote);
  }

  formatProductType(type: string): string {
    return type.replace(/([A-Z])/g, ' $1').trim();
  }

  private getSortValue(quote: QuoteResponse, field: string): any {
    switch (field) {
      case 'quoteDate':
        return new Date(quote.quoteDate).getTime();
      case 'premium':
        return quote.premium?.annualPremium ?? 0;
      default:
        return (quote as any)[field];
    }
  }
}
