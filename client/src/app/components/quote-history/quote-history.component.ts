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
  templateUrl: './quote-history.component.html',
  styleUrl: './quote-history.component.scss'
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
    const quotes = [...this.history()];

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

  private getSortValue(quote: QuoteResponse, field: string): string | number {
    switch (field) {
      case 'quoteDate':
        return new Date(quote.quoteDate).getTime();
      case 'premium':
        return quote.premium?.annualPremium ?? 0;
      default:
        return String((quote as unknown as Record<string, unknown>)[field] ?? '');
    }
  }
}
