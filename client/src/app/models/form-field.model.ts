/**
 * Dynamic form field configuration.
 * Used for generating form fields based on business/product type.
 */
export interface FormFieldConfig {
  key: string;
  label: string;
  type: FormFieldType;
  required: boolean;
  defaultValue?: string | number | boolean;
  validators?: FormFieldValidator[];
  options?: FormFieldOption[];
  hint?: string;
  placeholder?: string;
  min?: number;
  max?: number;
  step?: number;
  prefix?: string;
  suffix?: string;
  dependsOn?: string;
  visibleWhen?: VisibilityCondition;
}

export type FormFieldType =
  | 'text'
  | 'number'
  | 'currency'
  | 'select'
  | 'radio'
  | 'checkbox'
  | 'date'
  | 'email'
  | 'phone'
  | 'textarea';

export interface FormFieldValidator {
  type: ValidatorType;
  value?: string | number | boolean;
  message: string;
}

export type ValidatorType =
  | 'required'
  | 'min'
  | 'max'
  | 'minLength'
  | 'maxLength'
  | 'pattern'
  | 'email'
  | 'custom';

export interface FormFieldOption {
  value: string | number | boolean;
  label: string;
  disabled?: boolean;
}

export interface VisibilityCondition {
  field: string;
  operator: 'equals' | 'notEquals' | 'contains' | 'greaterThan' | 'lessThan';
  value: string | number | boolean;
}

/**
 * Form step configuration for multi-step wizard.
 */
export interface FormStepConfig {
  id: string;
  label: string;
  description?: string;
  fields: FormFieldConfig[];
  optional?: boolean;
}

/**
 * Pre-defined form configurations for different business types.
 */
export const WORKERS_COMP_FIELDS: FormFieldConfig[] = [
  {
    key: 'annualPayroll',
    label: 'Annual Payroll',
    type: 'currency',
    required: true,
    min: 10000,
    max: 100000000,
    hint: 'Total annual payroll for all employees',
    validators: [
      { type: 'required', message: 'Annual payroll is required' },
      { type: 'min', value: 10000, message: 'Minimum payroll is $10,000' }
    ]
  },
  {
    key: 'employeeCount',
    label: 'Number of Employees',
    type: 'number',
    required: true,
    min: 1,
    max: 10000,
    validators: [
      { type: 'required', message: 'Employee count is required' },
      { type: 'min', value: 1, message: 'At least 1 employee required' }
    ]
  },
  {
    key: 'classificationCode',
    label: 'Classification Code',
    type: 'select',
    required: true,
    options: [],
    hint: 'Primary classification for workers compensation'
  }
];

export const GENERAL_LIABILITY_FIELDS: FormFieldConfig[] = [
  {
    key: 'annualRevenue',
    label: 'Annual Revenue',
    type: 'currency',
    required: true,
    min: 10000,
    max: 50000000,
    hint: 'Gross annual revenue'
  },
  {
    key: 'coverageLimit',
    label: 'Coverage Limit',
    type: 'select',
    required: true,
    defaultValue: 1000000,
    options: [
      { value: 300000, label: '$300,000' },
      { value: 500000, label: '$500,000' },
      { value: 1000000, label: '$1,000,000' },
      { value: 2000000, label: '$2,000,000' }
    ]
  },
  {
    key: 'deductible',
    label: 'Deductible',
    type: 'select',
    required: true,
    defaultValue: 1000,
    options: [
      { value: 500, label: '$500' },
      { value: 1000, label: '$1,000' },
      { value: 2500, label: '$2,500' },
      { value: 5000, label: '$5,000' },
      { value: 10000, label: '$10,000' }
    ]
  }
];

export const BOP_FIELDS: FormFieldConfig[] = [
  ...GENERAL_LIABILITY_FIELDS,
  {
    key: 'buildingValue',
    label: 'Building Value',
    type: 'currency',
    required: false,
    hint: 'If you own the building'
  },
  {
    key: 'contentsValue',
    label: 'Contents Value',
    type: 'currency',
    required: true,
    hint: 'Value of business personal property'
  }
];
