import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { QuoteHistoryComponent } from '../../components/quote-history/quote-history.component';
import { QuoteService } from '../../services/quote.service';
import { QuoteResponse } from '../../models/quote.model';

/**
 * Quote history page.
 */
@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [CommonModule, QuoteHistoryComponent],
  template: `
    <div class="history-page">
      <h1>Quote History</h1>
      <app-quote-history
        [history]="quoteService.quoteHistory()"
        (view)="onViewQuote($event)"
        (bind)="onBindQuote($event)">
      </app-quote-history>
    </div>
  `,
  styles: [`
    .history-page {
      padding: 20px 0;
    }

    h1 {
      margin-bottom: 24px;
    }
  `]
})
export class HistoryPage {
  quoteService = inject(QuoteService);
  private router = inject(Router);

  onViewQuote(quote: QuoteResponse): void {
    // Navigate to quote details or show modal
    console.log('View quote:', quote.quoteNumber);
  }

  onBindQuote(quote: QuoteResponse): void {
    // Navigate to binding flow
    console.log('Bind quote:', quote.quoteNumber);
  }
}
