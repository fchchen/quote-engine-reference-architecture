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
 * Uses one-way data flow: parent passes value via [value], child emits changes via (valueChange).
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [placeholder]="config().placeholder ?? ''"
            [required]="config().required">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [min]="config().min ?? null"
            [max]="config().max ?? null"
            [step]="config().step ?? 1"
            [required]="config().required">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [min]="config().min ?? 0"
            [max]="config().max ?? null"
            [required]="config().required">
          @if (config().hint) {
            <mat-hint>{{ config().hint }}</mat-hint>
          }
        </mat-form-field>
      }

      @case ('select') {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ config().label }}</mat-label>
          <mat-select
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [required]="config().required">
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
          [ngModel]="value()"
          (ngModelChange)="valueChange.emit($event)"
          [required]="config().required">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [required]="config().required">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [required]="config().required">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [required]="config().required"
            rows="4">
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
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [required]="config().required">
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
  config = input.required<FormFieldConfig>();
  value = input<any>('');
  valueChange = output<any>();
}
