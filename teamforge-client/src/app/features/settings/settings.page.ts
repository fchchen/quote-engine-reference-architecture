import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ThemeService } from '../../core/services/theme.service';
import { ThemedToolbarComponent } from '../../shared/components/themed-toolbar.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
    ThemedToolbarComponent
  ],
  template: `
    <app-themed-toolbar></app-themed-toolbar>
    <div class="page-container">
      <div class="page-header">
        <h1>Branding Settings</h1>
      </div>
      <mat-card>
        <mat-card-content>
          <form [formGroup]="brandingForm" (ngSubmit)="save()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Company Name</mat-label>
              <input matInput formControlName="companyName">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Tag Line</mat-label>
              <input matInput formControlName="tagLine">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Primary Color</mat-label>
              <input matInput formControlName="primaryColor" type="color">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Secondary Color</mat-label>
              <input matInput formControlName="secondaryColor" type="color">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Font Family</mat-label>
              <input matInput formControlName="fontFamily">
            </mat-form-field>
            <div class="form-actions">
              <button mat-raised-button color="primary" type="submit">Save</button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`.full-width { width: 100%; }`]
})
export class SettingsPage implements OnInit {
  themeService = inject(ThemeService);
  private fb = inject(FormBuilder);

  brandingForm = this.fb.group({
    companyName: [''],
    tagLine: [''],
    primaryColor: ['#3f51b5'],
    secondaryColor: ['#ff4081'],
    fontFamily: ['Roboto']
  });

  ngOnInit(): void {
    const branding = this.themeService.branding();
    if (branding) {
      this.brandingForm.patchValue({
        companyName: branding.companyName,
        tagLine: branding.tagLine,
        primaryColor: branding.primaryColor,
        secondaryColor: branding.secondaryColor,
        fontFamily: branding.fontFamily
      });
    }
  }

  save(): void {
    const v = this.brandingForm.value;
    this.themeService.updateBranding({
      companyName: v.companyName ?? undefined,
      tagLine: v.tagLine ?? undefined,
      primaryColor: v.primaryColor ?? undefined,
      secondaryColor: v.secondaryColor ?? undefined,
      fontFamily: v.fontFamily ?? undefined
    });
  }
}
