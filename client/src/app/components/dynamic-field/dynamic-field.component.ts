import { Component, input, output } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FormFieldConfig } from '../../models/form-field.model';

/**
 * Dynamic form field component - renders different input types based on config.
 *
 * INTERVIEW TALKING POINTS:
 * - Uses signal-based input() and output()
 * - @switch for type-based rendering
 * - Reusable for any form configuration
 */
@Component({
  selector: 'app-dynamic-field',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    CurrencyPipe
  ],
  template: `
    @switch (config().type) {
      @case ('text') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <input
            matInput
            type="text"
            [value]="value()"
            [placeholder]="config().placeholder ?? ''"
            [required]="config().required"
            (input)="onInput($event)">
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('number') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <input
            matInput
            type="number"
            [value]="value()"
            [min]="config().min"
            [max]="config().max"
            [step]="config().step ?? 1"
            [required]="config().required"
            (input)="onNumberInput($event)">
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('currency') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <span matTextPrefix>$ </span>
          <input
            matInput
            type="number"
            [value]="value()"
            [min]="config().min"
            [max]="config().max"
            [required]="config().required"
            (input)="onNumberInput($event)">
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('select') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <mat-select
            [value]="value()"
            [required]="config().required"
            (selectionChange)="onChange($event.value)">
            @for (opt of config().options; track opt.value) {
              <mat-option [value]="opt.value" [disabled]="opt.disabled">
                {{ opt.label }}
              </mat-option>
            }
          </mat-select>
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('checkbox') {
        <mat-checkbox
          [checked]="value()"
          [required]="config().required"
          (change)="onChange($event.checked)">
          {{ config().label }}
        </mat-checkbox>
        @if (config().hint) {
          <p class="checkbox-hint">{{ config().hint }}</p>
        }
      }

      @case ('date') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <input
            matInput
            [matDatepicker]="picker"
            [value]="value()"
            [required]="config().required"
            (dateChange)="onChange($event.value)">
          <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('email') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <input
            matInput
            type="email"
            [value]="value()"
            [required]="config().required"
            (input)="onInput($event)">
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('textarea') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <textarea
            matInput
            [value]="value()"
            [required]="config().required"
            rows="4"
            (input)="onInput($event)">
          </textarea>
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @default {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <input
            matInput
            [value]="value()"
            [required]="config().required"
            (input)="onInput($event)">
        </mat-form-field>
      }
    }
  `,
  styles: [`
    .full-width {
      width: 100%;
    }

    .checkbox-hint {
      font-size: 0.75rem;
      color: rgba(0, 0, 0, 0.54);
      margin-top: 4px;
    }

    mat-checkbox {
      display: block;
      margin: 16px 0;
    }
  `]
})
export class DynamicFieldComponent {
  // Signal-based inputs
  config = input.required<FormFieldConfig>();
  value = input<any>('');

  // Signal-based output
  valueChange = output<any>();

  /**
   * Handle text input changes.
   */
  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.valueChange.emit(value);
  }

  /**
   * Handle number input changes.
   */
  onNumberInput(event: Event): void {
    const value = (event.target as HTMLInputElement).valueAsNumber;
    this.valueChange.emit(isNaN(value) ? null : value);
  }

  /**
   * Handle select/checkbox/date changes.
   */
  onChange(value: any): void {
    this.valueChange.emit(value);
  }
}
