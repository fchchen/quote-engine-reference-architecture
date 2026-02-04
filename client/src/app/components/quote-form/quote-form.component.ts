import { Component, inject, signal, computed, effect, ViewChild } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatStepperModule, MatStepper } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { QuoteService } from '../../services/quote.service';
import { RateTableService } from '../../services/rate-table.service';
import { BusinessSearchComponent } from '../business-search/business-search.component';
import { QuoteResultComponent } from '../quote-result/quote-result.component';
import { DynamicFieldComponent } from '../dynamic-field/dynamic-field.component';
import { RiskFactorsComponent } from '../risk-factors/risk-factors.component';
import { Business, StateInfo, ProductInfo, BusinessTypeInfo } from '../../models/business.model';
import { QuoteRequest, ProductType, BusinessType, RiskFactor } from '../../models/quote.model';
import { FormFieldConfig } from '../../models/form-field.model';
import { ReferenceData } from '../../resolvers/rate-table.resolver';

/**
 * Multi-step quote form using Angular Material Stepper.
 *
 * INTERVIEW TALKING POINTS:
 * - Signal-first state management
 * - computed() for derived values (premium estimate)
 * - effect() for side effects (load dynamic fields)
 * - Reactive forms with validation
 * - RxJS only for HTTP (via services)
 */
@Component({
  selector: 'app-quote-form',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    ReactiveFormsModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatDividerModule,
    MatSnackBarModule,
    BusinessSearchComponent,
    QuoteResultComponent,
    DynamicFieldComponent,
    RiskFactorsComponent
  ],
  template: `
    <div class="quote-form-container">
      <h1>Get a Quote</h1>

      <mat-stepper [linear]="true" #stepper>
        <!-- Step 1: Business Information -->
        <mat-step [stepControl]="businessForm" label="Business Info">
          <div class="step-content">
            <h2>Business Information</h2>
            <p class="step-description">
              Search for an existing business or enter new information.
            </p>

            <app-business-search
              [resetTrigger]="searchResetTrigger()"
              (businessSelected)="onBusinessSelected($event)">
            </app-business-search>

            <mat-divider></mat-divider>

            <form [formGroup]="businessForm">
              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Business Name</mat-label>
                  <input matInput formControlName="businessName" required>
                  @if (businessForm.get('businessName')?.hasError('required')) {
                    <mat-error>Business name is required</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Tax ID</mat-label>
                  <input matInput formControlName="taxId" placeholder="XX-XXXXXXX" required>
                  @if (businessForm.get('taxId')?.hasError('required')) {
                    <mat-error>Tax ID is required</mat-error>
                  } @else if (businessForm.get('taxId')?.hasError('pattern')) {
                    <mat-error>Format: XX-XXXXXXX</mat-error>
                  }
                </mat-form-field>
              </div>

              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Business Type</mat-label>
                  <mat-select formControlName="businessType" required>
                    @for (type of businessTypes(); track type.type) {
                      <mat-option [value]="type.type">{{ type.name }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>State</mat-label>
                  <mat-select formControlName="stateCode" required>
                    @for (state of states(); track state.code) {
                      <mat-option [value]="state.code">{{ state.name }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Years in Business</mat-label>
                  <input matInput type="number" formControlName="yearsInBusiness" min="1">
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Number of Employees</mat-label>
                  <input matInput type="number" formControlName="employeeCount" min="1">
                </mat-form-field>
              </div>

              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Annual Revenue</mat-label>
                  <span matTextPrefix>$ </span>
                  <input matInput type="number" formControlName="annualRevenue">
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Annual Payroll</mat-label>
                  <span matTextPrefix>$ </span>
                  <input matInput type="number" formControlName="annualPayroll">
                </mat-form-field>
              </div>
            </form>

            <div class="button-row">
              <button mat-raised-button color="primary" matStepperNext
                      [disabled]="!businessForm.valid">
                Next
              </button>
            </div>
          </div>
        </mat-step>

        <!-- Step 2: Coverage Selection -->
        <mat-step [stepControl]="coverageForm" label="Coverage">
          <div class="step-content">
            <h2>Coverage Options</h2>

            <form [formGroup]="coverageForm">
              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Product Type</mat-label>
                  <mat-select formControlName="productType" required>
                    @for (product of products(); track product.type) {
                      <mat-option [value]="product.type">{{ product.name }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Classification Code</mat-label>
                  <mat-select formControlName="classificationCode">
                    @for (code of classificationCodes(); track code.code) {
                      <mat-option [value]="code.code">
                        {{ code.code }} - {{ code.description }}
                      </mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <div class="form-row">
                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Coverage Limit</mat-label>
                  <mat-select formControlName="coverageLimit">
                    <mat-option [value]="300000">$300,000</mat-option>
                    <mat-option [value]="500000">$500,000</mat-option>
                    <mat-option [value]="1000000">$1,000,000</mat-option>
                    <mat-option [value]="2000000">$2,000,000</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="form-field">
                  <mat-label>Deductible</mat-label>
                  <mat-select formControlName="deductible">
                    <mat-option [value]="500">$500</mat-option>
                    <mat-option [value]="1000">$1,000</mat-option>
                    <mat-option [value]="2500">$2,500</mat-option>
                    <mat-option [value]="5000">$5,000</mat-option>
                    <mat-option [value]="10000">$10,000</mat-option>
                  </mat-select>
                </mat-form-field>
              </div>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Effective Date</mat-label>
                <input matInput type="date" formControlName="effectiveDate">
              </mat-form-field>

              <!-- Dynamic Fields based on Product Type -->
              @for (field of dynamicFields(); track field.key + '-' + currentBusinessId()) {
                <app-dynamic-field
                  [config]="field"
                  [value]="getDynamicFieldValue(field.key)"
                  (valueChange)="setDynamicFieldValue(field.key, $event)">
                </app-dynamic-field>
              }
            </form>

            <!-- Premium Estimate -->
            @if (premiumEstimate()) {
              <mat-card class="estimate-card">
                <mat-card-content>
                  <div class="estimate-display">
                    <span class="estimate-label">Estimated Premium</span>
                    <span class="estimate-amount">
                      {{ premiumEstimate()!.estimatedAnnualPremium | currency }}
                    </span>
                    <span class="estimate-note">per year</span>
                  </div>
                </mat-card-content>
              </mat-card>
            }

            <div class="button-row">
              <button mat-button matStepperPrevious>Back</button>
              <button mat-raised-button color="primary" matStepperNext
                      [disabled]="!coverageForm.valid">
                Next
              </button>
            </div>
          </div>
        </mat-step>

        <!-- Step 3: Risk Factors (Optional) -->
        <mat-step label="Risk Factors" [optional]="true">
          <div class="step-content">
            <h2>Additional Risk Information</h2>
            <p class="step-description">
              Providing this information may help improve your quote.
            </p>

            <app-risk-factors
              [resetTrigger]="riskFactorsResetTrigger()"
              [initialFactors]="riskFactors()"
              (factorsChange)="onRiskFactorsChange($event)">
            </app-risk-factors>

            <div class="button-row">
              <button mat-button matStepperPrevious>Back</button>
              <button mat-raised-button color="primary" matStepperNext>
                Next
              </button>
            </div>
          </div>
        </mat-step>

        <!-- Step 4: Review & Submit -->
        <mat-step label="Review">
          <div class="step-content">
            <h2>Review & Submit</h2>

            @if (quoteService.isLoading()) {
              <div class="loading-container">
                <mat-spinner diameter="60"></mat-spinner>
                <p>Calculating your quote...</p>
              </div>
            } @else if (quoteService.currentQuote()) {
              <app-quote-result
                [quote]="quoteService.currentQuote()!"
                (save)="onSaveQuote($event)"
                (bind)="onBindQuote($event)">
              </app-quote-result>
              <div class="button-row">
                <button mat-raised-button color="accent" (click)="startNewQuote()">
                  <mat-icon>add</mat-icon>
                  Start New Quote
                </button>
              </div>
            } @else {
              <!-- Review Summary -->
              <mat-card class="review-card">
                <mat-card-header>
                  <mat-card-title>Quote Summary</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <div class="review-section">
                    <h4>Business</h4>
                    <p>{{ businessForm.get('businessName')?.value }}</p>
                    <p>{{ businessForm.get('stateCode')?.value }} | {{ getBusinessTypeName() }}</p>
                  </div>

                  <div class="review-section">
                    <h4>Coverage</h4>
                    <p>{{ getProductTypeName() }}</p>
                    <p>Limit: {{ coverageForm.get('coverageLimit')?.value | currency }}</p>
                    <p>Deductible: {{ coverageForm.get('deductible')?.value | currency }}</p>
                  </div>

                  @if (premiumEstimate()) {
                    <div class="review-section estimate">
                      <h4>Estimated Premium</h4>
                      <p class="estimate-amount">
                        {{ premiumEstimate()!.estimatedAnnualPremium | currency }}/year
                      </p>
                    </div>
                  }
                </mat-card-content>
              </mat-card>

              @if (quoteService.error()) {
                <div class="error-message">
                  <mat-icon>error</mat-icon>
                  {{ quoteService.error() }}
                </div>
              }

              <div class="button-row">
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="primary"
                        (click)="submitQuote()"
                        [disabled]="!businessForm.valid || !coverageForm.valid">
                  <mat-icon>calculate</mat-icon>
                  Get Final Quote
                </button>
              </div>
            }
          </div>
        </mat-step>
      </mat-stepper>
    </div>
  `,
  styles: [`
    .quote-form-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 20px;
    }

    h1 {
      margin-bottom: 24px;
    }

    .step-content {
      padding: 24px 0;
    }

    .step-description {
      color: rgba(0, 0, 0, 0.54);
      margin-bottom: 24px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 8px;
    }

    .form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    mat-divider {
      margin: 24px 0;
    }

    .button-row {
      display: flex;
      gap: 12px;
      margin-top: 24px;
      justify-content: flex-end;
    }

    .estimate-card {
      margin: 24px 0;
      background-color: #e3f2fd;
    }

    .estimate-display {
      text-align: center;
    }

    .estimate-label {
      display: block;
      font-size: 0.875rem;
      color: rgba(0, 0, 0, 0.54);
    }

    .estimate-amount {
      display: block;
      font-size: 2rem;
      font-weight: 500;
      color: #1976d2;
    }

    .estimate-note {
      display: block;
      font-size: 0.75rem;
      color: rgba(0, 0, 0, 0.54);
    }

    .loading-container {
      text-align: center;
      padding: 48px;
    }

    .loading-container p {
      margin-top: 16px;
      color: rgba(0, 0, 0, 0.54);
    }

    .review-card {
      margin: 16px 0;
    }

    .review-section {
      margin-bottom: 16px;
    }

    .review-section h4 {
      margin: 0 0 8px 0;
      color: rgba(0, 0, 0, 0.54);
      font-size: 0.875rem;
    }

    .review-section.estimate {
      text-align: center;
      padding: 16px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #c62828;
      padding: 16px;
      background-color: #ffebee;
      border-radius: 4px;
      margin: 16px 0;
    }

    @media (max-width: 768px) {
      .form-row {
        flex-direction: column;
        gap: 0;
      }
    }
  `]
})
export class QuoteFormComponent {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private rateTableService = inject(RateTableService);
  private snackBar = inject(MatSnackBar);
  quoteService = inject(QuoteService);

  @ViewChild('stepper') stepper!: MatStepper;

  // Reference data from resolver
  states = signal<StateInfo[]>([]);
  products = signal<ProductInfo[]>([]);
  businessTypes = signal<BusinessTypeInfo[]>([]);
  classificationCodes = signal<{ code: string; description: string }[]>([]);
  dynamicFields = signal<FormFieldConfig[]>([]);

  // UI state
  currentStep = signal(0);
  selectedBusiness = signal<Business | null>(null);
  selectedProductType = signal<string>('GeneralLiability');
  searchResetTrigger = signal(0);
  riskFactorsResetTrigger = signal(0);

  // Data isolation per business
  currentBusinessId = signal<string | null>(null);
  private allDynamicFieldValues = signal<Map<string, Record<string, any>>>(new Map());
  private allRiskFactors = signal<Map<string, RiskFactor[]>>(new Map());
  private allCoverageFormValues = signal<Map<string, Record<string, any>>>(new Map());

  private defaultCoverageValues(): Record<string, any> {
    return {
      productType: 'GeneralLiability',
      classificationCode: '41650',
      coverageLimit: 1000000,
      deductible: 1000,
      effectiveDate: this.getDefaultEffectiveDate()
    };
  }

  // Computed signal for current business's risk factors
  riskFactors = computed(() => {
    const businessId = this.currentBusinessId();
    if (!businessId) return [];
    return this.allRiskFactors().get(businessId) ?? [];
  });

  // Computed signal for current business's dynamic field values
  dynamicFieldValues = computed(() => {
    const businessId = this.currentBusinessId();
    if (!businessId) {
      return this.defaultDynamicValues();
    }
    const allValues = this.allDynamicFieldValues();
    return allValues.get(businessId) ?? this.defaultDynamicValues();
  });

  private defaultDynamicValues(): Record<string, any> {
    return {
      annualRevenue: 500000,
      annualPayroll: 300000,
      employeeCount: 10,
      buildingValue: 0,
      contentsValue: 0,
      squareFootage: 0,
      vehicleCount: 0,
      recordCount: 0,
      pciCompliant: false,
      radius: 50
    };
  }

  // Forms
  businessForm = this.fb.group({
    businessName: ['', [Validators.required, Validators.minLength(2)]],
    taxId: ['', [Validators.required, Validators.pattern(/^\d{2}-\d{7}$/)]],
    businessType: ['', Validators.required],
    stateCode: ['', Validators.required],
    yearsInBusiness: [5, [Validators.required, Validators.min(1)]],
    employeeCount: [10, [Validators.required, Validators.min(1)]],
    annualRevenue: [500000, [Validators.required, Validators.min(10000)]],
    annualPayroll: [300000, [Validators.required, Validators.min(10000)]]
  });

  coverageForm = this.fb.group({
    productType: ['GeneralLiability', Validators.required],
    classificationCode: ['41650', Validators.required],
    coverageLimit: [1000000, Validators.required],
    deductible: [1000, Validators.required],
    effectiveDate: [this.getDefaultEffectiveDate()]
  });

  // Computed values
  businessFormValid = computed(() => this.businessForm.valid);

  premiumEstimate = computed(() => this.quoteService.premiumEstimate());

  isFormComplete = computed(() =>
    this.businessForm.valid && this.coverageForm.valid
  );

  constructor() {
    // Load reference data from resolver
    const data = this.route.snapshot.data['referenceData'] as ReferenceData | undefined;
    if (data) {
      this.states.set(data.states);
      this.products.set(data.products);
      this.businessTypes.set(data.businessTypes);
    }

    // Load initial classification codes
    this.loadClassificationCodes('GeneralLiability');

    // Subscribe to product type changes and reload classification codes
    this.coverageForm.get('productType')?.valueChanges.subscribe(value => {
      if (value) {
        this.selectedProductType.set(value);
        this.loadClassificationCodes(value);
      }
    });

    // Effect: Update premium estimate when form values change
    effect(() => {
      if (this.businessForm.valid && this.coverageForm.valid) {
        this.updatePremiumEstimate();
      }
    });
  }

  onBusinessSelected(business: Business): void {
    // Save current business's coverage form values before switching
    const previousBusinessId = this.currentBusinessId();
    if (previousBusinessId) {
      this.allCoverageFormValues.update(allValues => {
        const newMap = new Map(allValues);
        newMap.set(previousBusinessId, { ...this.coverageForm.value });
        return newMap;
      });
    }

    this.selectedBusiness.set(business);

    // Set the current business ID to isolate data
    const businessId = business.taxId || business.businessName;
    this.currentBusinessId.set(businessId);

    // Calculate years in business from dateEstablished
    let yearsInBusiness = 5; // default
    if (business.dateEstablished) {
      const established = new Date(business.dateEstablished);
      const now = new Date();
      yearsInBusiness = Math.max(1, Math.floor((now.getTime() - established.getTime()) / (365.25 * 24 * 60 * 60 * 1000)));
    }

    this.businessForm.patchValue({
      businessName: business.businessName,
      taxId: business.taxId,
      businessType: business.businessType,
      stateCode: business.stateCode,
      yearsInBusiness: yearsInBusiness,
      employeeCount: business.employeeCount ?? 10,
      annualRevenue: business.annualRevenue ?? 500000,
      annualPayroll: business.annualPayroll ?? 300000
    });

    // Initialize dynamic field values for this business (only if not already set)
    const allValues = this.allDynamicFieldValues();
    if (!allValues.has(businessId)) {
      const newValues = new Map(allValues);
      newValues.set(businessId, {
        annualRevenue: business.annualRevenue ?? 500000,
        annualPayroll: business.annualPayroll ?? 300000,
        employeeCount: business.employeeCount ?? 10,
        buildingValue: 0,
        contentsValue: 0,
        squareFootage: 0,
        vehicleCount: 0,
        recordCount: 0,
        pciCompliant: false,
        radius: 50
      });
      this.allDynamicFieldValues.set(newValues);
    }

    // Restore coverage form values for this business (or use defaults)
    const savedCoverage = this.allCoverageFormValues().get(businessId);
    const coverageValues = savedCoverage ?? this.defaultCoverageValues();
    this.coverageForm.patchValue(coverageValues);

    // Trigger risk factors reset for new business
    this.riskFactorsResetTrigger.update(n => n + 1);
  }

  onRiskFactorsChange(factors: RiskFactor[]): void {
    const businessId = this.currentBusinessId();
    if (!businessId) return;

    // Store risk factors for the current business
    this.allRiskFactors.update(allFactors => {
      const newMap = new Map(allFactors);
      newMap.set(businessId, factors);
      return newMap;
    });
  }

  getDynamicFieldValue(key: string): any {
    const values = this.dynamicFieldValues();
    return values[key] ?? '';
  }

  setDynamicFieldValue(key: string, value: any): void {
    const businessId = this.currentBusinessId();
    if (!businessId) return;

    // Update the values for the current business
    this.allDynamicFieldValues.update(allValues => {
      const newMap = new Map(allValues);
      const currentValues = newMap.get(businessId) ?? this.defaultDynamicValues();
      newMap.set(businessId, {
        ...currentValues,
        [key]: value
      });
      return newMap;
    });
  }

  submitQuote(): void {
    if (!this.isFormComplete()) return;

    const request: QuoteRequest = {
      ...this.businessForm.value as any,
      ...this.coverageForm.value as any,
      riskFactors: this.riskFactors()
    };

    this.quoteService.calculateQuote(request);
  }

  onSaveQuote(quote: any): void {
    this.quoteService.saveQuote(quote);
    this.snackBar.open(`Quote ${quote.quoteNumber} saved successfully`, 'Close', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }

  onBindQuote(quote: any): void {
    console.log('Bind quote:', quote);
    // Would integrate with policy binding functionality
  }

  startNewQuote(): void {
    // Clear current quote
    this.quoteService.clearCurrentQuote();

    // Save coverage form values before reset (product type, limits, etc.)
    const savedCoverage = { ...this.coverageForm.value };

    // Only reset business form, keep coverage options
    this.businessForm.reset({
      yearsInBusiness: 5,
      employeeCount: 10,
      annualRevenue: 500000,
      annualPayroll: 300000
    });

    // Reset selected business and current business ID
    // This will make dynamicFieldValues and riskFactors return default/empty values
    this.selectedBusiness.set(null);
    this.currentBusinessId.set(null);

    // Trigger search field and risk factors reset
    this.searchResetTrigger.update(n => n + 1);
    this.riskFactorsResetTrigger.update(n => n + 1);

    // Go back to first step without resetting all forms
    this.stepper.selectedIndex = 0;

    // Restore coverage form values (product type, limits, deductible, etc.)
    this.coverageForm.patchValue(savedCoverage);
  }

  hasUnsavedChanges(): boolean {
    return this.businessForm.dirty || this.coverageForm.dirty;
  }

  getBusinessTypeName(): string {
    const type = this.businessForm.get('businessType')?.value;
    return this.businessTypes().find(t => t.type === type)?.name ?? type ?? '';
  }

  getProductTypeName(): string {
    const type = this.coverageForm.get('productType')?.value;
    return this.products().find(p => p.type === type)?.name ?? type ?? '';
  }

  private loadClassificationCodes(productType: string): void {
    this.rateTableService.getClassificationCodes(productType as ProductType)
      .subscribe(codes => {
        this.classificationCodes.set(codes);
      });

    this.rateTableService.getFieldsForType(productType as ProductType)
      .subscribe(fields => this.dynamicFields.set(fields));
  }

  private getDefaultEffectiveDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 1);
    return date.toISOString().split('T')[0];
  }

  private updatePremiumEstimate(): void {
    const productType = this.coverageForm.get('productType')?.value;
    if (!productType) return;

    this.quoteService.getPremiumEstimate({
      productType: productType as ProductType,
      stateCode: this.businessForm.get('stateCode')?.value ?? '',
      annualPayroll: this.businessForm.get('annualPayroll')?.value ?? 0,
      annualRevenue: this.businessForm.get('annualRevenue')?.value ?? 0,
      employeeCount: this.businessForm.get('employeeCount')?.value ?? 0,
      coverageLimit: this.coverageForm.get('coverageLimit')?.value ?? 1000000,
      deductible: this.coverageForm.get('deductible')?.value ?? 1000
    });
  }
}
