import { Component, input, output, Pipe, PipeTransform } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { QuoteResponse } from '../../models/quote.model';

// Pipe for formatting product type - defined before use
@Pipe({
  name: 'formatProductType',
  standalone: true
})
export class FormatProductTypePipe implements PipeTransform {
  transform(value: string): string {
    // Convert camelCase to Title Case with spaces
    return value.replace(/([A-Z])/g, ' $1').trim();
  }
}

/**
 * Quote result display component using signal-based inputs/outputs.
 *
 * INTERVIEW TALKING POINTS:
 * - Uses Angular 17+ input() and output() signals
 * - Standalone component with imports
 * - Demonstrates premium breakdown display
 */
@Component({
  selector: 'app-quote-result',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatListModule,
    MatChipsModule,
    FormatProductTypePipe
  ],
  templateUrl: './quote-result.component.html',
  styleUrl: './quote-result.component.scss'
})
export class QuoteResultComponent {
  // Signal-based inputs (Angular 17+)
  quote = input.required<QuoteResponse>();
  showActions = input(true);

  // Signal-based outputs (Angular 17+)
  save = output<QuoteResponse>();
  bind = output<QuoteResponse>();
  print = output<QuoteResponse>();

  onSave(): void {
    this.save.emit(this.quote());
  }

  onBind(): void {
    this.bind.emit(this.quote());
  }

  onPrint(): void {
    this.print.emit(this.quote());
    window.print();
  }
}
