import { Component, input, output, Pipe, PipeTransform } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { QuoteResponse, RiskTier, QuoteStatus } from '../../models/quote.model';

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
  template: `
    <mat-card class="quote-result-card">
      <mat-card-header>
        <mat-icon mat-card-avatar>description</mat-icon>
        <mat-card-title>Quote #{{ quote().quoteNumber }}</mat-card-title>
        <mat-card-subtitle>
          Generated {{ quote().quoteDate | date:'medium' }}
        </mat-card-subtitle>
        <span class="status-badge" [class]="'status-' + quote().status.toLowerCase()">
          {{ quote().status }}
        </span>
      </mat-card-header>

      <mat-card-content>
        <!-- Business Info -->
        <div class="section">
          <h3>Business Information</h3>
          <p><strong>{{ quote().businessName }}</strong></p>
          <p>{{ quote().productType | formatProductType }} - {{ quote().stateCode }}</p>
        </div>

        <mat-divider></mat-divider>

        <!-- Coverage Details -->
        <div class="section">
          <h3>Coverage</h3>
          <div class="detail-row">
            <span>Coverage Limit:</span>
            <span class="currency">{{ quote().coverageLimit | currency }}</span>
          </div>
          <div class="detail-row">
            <span>Deductible:</span>
            <span class="currency">{{ quote().deductible | currency }}</span>
          </div>
          <div class="detail-row">
            <span>Effective Date:</span>
            <span>{{ quote().effectiveDate | date }}</span>
          </div>
          <div class="detail-row">
            <span>Expiration Date:</span>
            <span>{{ quote().policyExpirationDate | date }}</span>
          </div>
        </div>

        <mat-divider></mat-divider>

        @if (quote().isEligible) {
          <!-- Premium Breakdown -->
          <div class="section">
            <h3>Premium Breakdown</h3>
            <div class="detail-row">
              <span>Base Premium:</span>
              <span class="currency">{{ quote().premium.basePremium | currency }}</span>
            </div>

            @for (adj of quote().premium.adjustments; track adj.code) {
              <div class="detail-row adjustment" [class.discount]="adj.amount < 0">
                <span>{{ adj.description }}:</span>
                <span class="currency">
                  {{ adj.amount >= 0 ? '+' : '' }}{{ adj.amount | currency }}
                </span>
              </div>
            }

            <div class="detail-row">
              <span>Subtotal:</span>
              <span class="currency">{{ quote().premium.subtotal | currency }}</span>
            </div>
            <div class="detail-row">
              <span>State Tax:</span>
              <span class="currency">{{ quote().premium.stateTax | currency }}</span>
            </div>
            <div class="detail-row">
              <span>Policy Fee:</span>
              <span class="currency">{{ quote().premium.policyFee | currency }}</span>
            </div>

            <mat-divider></mat-divider>

            <div class="premium-total">
              <div class="premium-display">
                {{ quote().premium.annualPremium | currency }}
                <span class="premium-label">/year</span>
              </div>
              <div class="monthly-premium">
                or {{ quote().premium.monthlyPremium | currency }}/month
              </div>
            </div>
          </div>

          <mat-divider></mat-divider>

          <!-- Risk Assessment -->
          <div class="section">
            <h3>Risk Assessment</h3>
            <div class="risk-info">
              <span class="risk-tier" [class]="'risk-' + quote().riskAssessment.riskTier.toLowerCase()">
                {{ quote().riskAssessment.riskTier }}
              </span>
              <span class="risk-score">
                Score: {{ quote().riskAssessment.riskScore }}/100
              </span>
            </div>

            @if (quote().riskAssessment.notes.length > 0) {
              <mat-list dense>
                @for (note of quote().riskAssessment.notes; track note) {
                  <mat-list-item>
                    <mat-icon matListItemIcon>info</mat-icon>
                    <span matListItemTitle>{{ note }}</span>
                  </mat-list-item>
                }
              </mat-list>
            }
          </div>
        } @else {
          <!-- Declined Message -->
          <div class="section declined">
            <h3>Quote Declined</h3>
            @for (msg of quote().eligibilityMessages; track msg) {
              <p class="error-message">{{ msg }}</p>
            }
          </div>
        }
      </mat-card-content>

      @if (showActions()) {
        <mat-card-actions align="end">
          <button mat-button (click)="onPrint()">
            <mat-icon>print</mat-icon>
            Print
          </button>
          <button mat-button (click)="onSave()">
            <mat-icon>save</mat-icon>
            Save
          </button>
          @if (quote().isEligible) {
            <button mat-raised-button color="primary" (click)="onBind()">
              <mat-icon>check_circle</mat-icon>
              Bind Policy
            </button>
          }
        </mat-card-actions>
      }

      <mat-card-footer>
        <small class="processing-time">
          Processed in {{ quote().processingTimeMs }}
        </small>
      </mat-card-footer>
    </mat-card>
  `,
  styles: [`
    .quote-result-card {
      max-width: 600px;
      margin: 20px auto;
    }

    mat-card-header {
      position: relative;
    }

    .status-badge {
      position: absolute;
      top: 16px;
      right: 16px;
    }

    .section {
      padding: 16px 0;
    }

    .section h3 {
      margin: 0 0 12px 0;
      font-size: 1rem;
      color: rgba(0, 0, 0, 0.54);
    }

    .detail-row {
      display: flex;
      justify-content: space-between;
      padding: 4px 0;
    }

    .adjustment {
      color: rgba(0, 0, 0, 0.54);
      font-size: 0.875rem;
    }

    .adjustment.discount {
      color: #2e7d32;
    }

    .premium-total {
      text-align: center;
      padding: 24px 0;
    }

    .premium-display {
      font-size: 2.5rem;
      font-weight: 500;
      color: #1976d2;
    }

    .premium-label {
      font-size: 1rem;
      color: rgba(0, 0, 0, 0.54);
    }

    .monthly-premium {
      color: rgba(0, 0, 0, 0.54);
      font-size: 0.875rem;
    }

    .risk-info {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .risk-score {
      color: rgba(0, 0, 0, 0.54);
    }

    .declined {
      text-align: center;
      color: #c62828;
    }

    .processing-time {
      display: block;
      text-align: right;
      padding: 8px 16px;
      color: rgba(0, 0, 0, 0.38);
    }

    mat-card-actions {
      padding: 16px;
    }
  `]
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
