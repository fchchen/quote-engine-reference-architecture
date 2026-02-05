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
  templateUrl: './dynamic-field.component.html',
  styleUrl: './dynamic-field.component.scss'
})
export class DynamicFieldComponent {
  config = input.required<FormFieldConfig>();
  value = input<string | number | boolean>('');
  valueChange = output<string | number | boolean>();
}
