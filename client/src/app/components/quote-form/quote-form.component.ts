import { Component, inject, signal, computed, ViewChild, DestroyRef } from '@angular/core';
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
import { merge } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { QuoteService } from '../../services/quote.service';
import { RateTableService } from '../../services/rate-table.service';
import { BusinessSearchComponent } from '../business-search/business-search.component';
import { QuoteResultComponent } from '../quote-result/quote-result.component';
import { DynamicFieldComponent } from '../dynamic-field/dynamic-field.component';
import { RiskFactorsComponent } from '../risk-factors/risk-factors.component';
import { Business, StateInfo, ProductInfo, BusinessTypeInfo } from '../../models/business.model';
import { QuoteRequest, QuoteResponse, ProductType, RiskFactor } from '../../models/quote.model';
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
  templateUrl: './quote-form.component.html',
  styleUrl: './quote-form.component.scss'
})
export class QuoteFormComponent {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private rateTableService = inject(RateTableService);
  private snackBar = inject(MatSnackBar);
  private destroyRef = inject(DestroyRef);
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
  private allDynamicFieldValues = signal<Map<string, Record<string, unknown>>>(new Map());
  private allRiskFactors = signal<Map<string, RiskFactor[]>>(new Map());
  private allCoverageFormValues = signal<Map<string, Record<string, unknown>>>(new Map());
  private allBusinessFormValues = signal<Map<string, Record<string, unknown>>>(new Map());

  private defaultCoverageValues(): Record<string, unknown> {
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

  private defaultDynamicValues(): Record<string, unknown> {
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

    // Update premium estimate whenever relevant form data changes.
    merge(this.businessForm.valueChanges, this.coverageForm.valueChanges)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.businessForm.valid && this.coverageForm.valid) {
          this.updatePremiumEstimate();
        }
      });
  }

  onBusinessSelected(business: Business): void {
    // Save current business's form values before switching
    const previousBusinessId = this.currentBusinessId();
    if (previousBusinessId) {
      this.allCoverageFormValues.update(allValues => {
        const newMap = new Map(allValues);
        newMap.set(previousBusinessId, { ...this.coverageForm.value });
        return newMap;
      });
      this.allBusinessFormValues.update(allValues => {
        const newMap = new Map(allValues);
        newMap.set(previousBusinessId, { ...this.businessForm.value });
        return newMap;
      });
    }

    this.selectedBusiness.set(business);

    // Set the current business ID to isolate data
    const businessId = business.taxId || business.businessName;
    this.currentBusinessId.set(businessId);

    // Restore saved business form values, or populate from API data
    const savedBusinessForm = this.allBusinessFormValues().get(businessId);
    if (savedBusinessForm) {
      this.businessForm.patchValue(savedBusinessForm);
    } else {
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
    }

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

  getDynamicFieldValue(key: string): string | number | boolean {
    const values = this.dynamicFieldValues();
    return (values[key] as string | number | boolean) ?? '';
  }

  setDynamicFieldValue(key: string, value: string | number | boolean): void {
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

    const request = {
      ...this.businessForm.value,
      ...this.coverageForm.value,
      riskFactors: this.riskFactors()
    } as QuoteRequest;

    this.quoteService.calculateQuote(request);
  }

  onSaveQuote(quote: QuoteResponse): void {
    this.quoteService.saveQuote(quote);
    this.snackBar.open(`Quote ${quote.quoteNumber} saved successfully`, 'Close', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }

  onBindQuote(quote: QuoteResponse): void {
    console.log('Bind quote:', quote);
    // Would integrate with policy binding functionality
  }

  startNewQuote(): void {
    // Clear current quote
    this.quoteService.clearCurrentQuote();

    // Save current business's form values before resetting
    const previousBusinessId = this.currentBusinessId();
    if (previousBusinessId) {
      this.allBusinessFormValues.update(allValues => {
        const newMap = new Map(allValues);
        newMap.set(previousBusinessId, { ...this.businessForm.value });
        return newMap;
      });
      this.allCoverageFormValues.update(allValues => {
        const newMap = new Map(allValues);
        newMap.set(previousBusinessId, { ...this.coverageForm.value });
        return newMap;
      });
    }

    // Reset forms to defaults
    this.businessForm.reset({
      yearsInBusiness: 5,
      employeeCount: 10,
      annualRevenue: 500000,
      annualPayroll: 300000
    });
    this.coverageForm.patchValue(this.defaultCoverageValues());

    // Reset selected business and current business ID
    this.selectedBusiness.set(null);
    this.currentBusinessId.set(null);

    // Trigger search field and risk factors reset
    this.searchResetTrigger.update(n => n + 1);
    this.riskFactorsResetTrigger.update(n => n + 1);

    // Go back to first step
    this.stepper.selectedIndex = 0;
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
      classificationCode: this.coverageForm.get('classificationCode')?.value ?? 'DEFAULT',
      annualPayroll: this.businessForm.get('annualPayroll')?.value ?? 0,
      annualRevenue: this.businessForm.get('annualRevenue')?.value ?? 0,
      employeeCount: this.businessForm.get('employeeCount')?.value ?? 0,
      coverageLimit: this.coverageForm.get('coverageLimit')?.value ?? 1000000,
      deductible: this.coverageForm.get('deductible')?.value ?? 1000
    });
  }
}
