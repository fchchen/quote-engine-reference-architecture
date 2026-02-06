import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ThemedToolbarComponent } from '../../shared/components/themed-toolbar.component';

export interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
  lastLoginAt: string | null;
  isActive: boolean;
}

export interface Role {
  id: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule, MatProgressSpinnerModule,
    ThemedToolbarComponent
  ],
  templateUrl: './users.page.html',
  styleUrl: './users.page.scss'
})
export class UsersPage implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  users = signal<UserDetail[]>([]);
  roles = signal<Role[]>([]);
  isLoading = signal(true);
  showInviteForm = signal(false);
  editingUserId = signal<string | null>(null);
  selectedRoles = signal<string[]>([]);

  inviteForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Member']
  });

  ngOnInit(): void {
    this.http.get<UserDetail[]>(`${this.apiUrl}/users`).pipe(
      tap(users => {
        this.users.set(users);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of([]);
      })
    ).subscribe();

    this.http.get<Role[]>(`${this.apiUrl}/roles`).pipe(
      tap(roles => this.roles.set(roles)),
      catchError(() => of([]))
    ).subscribe();
  }

  inviteUser(): void {
    if (this.inviteForm.invalid) return;
    const v = this.inviteForm.value;
    this.http.post<UserDetail>(`${this.apiUrl}/users/invite`, {
      email: v.email,
      displayName: v.displayName,
      password: v.password,
      role: v.role
    }).pipe(
      tap(user => {
        this.users.update(list => [...list, user]);
        this.inviteForm.reset({ role: 'Member' });
        this.showInviteForm.set(false);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  startEditingRoles(user: UserDetail): void {
    this.editingUserId.set(user.id);
    this.selectedRoles.set([...user.roles]);
  }

  cancelEditingRoles(): void {
    this.editingUserId.set(null);
    this.selectedRoles.set([]);
  }

  updateRoles(): void {
    const id = this.editingUserId();
    if (!id) return;
    const roles = this.selectedRoles();
    this.http.put<UserDetail>(`${this.apiUrl}/users/${id}/roles`, { roles }).pipe(
      tap(updated => {
        this.users.update(list =>
          list.map(u => u.id === id ? { ...u, roles: updated.roles } : u)
        );
        this.editingUserId.set(null);
        this.selectedRoles.set([]);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  onRolesChanged(roleNames: string[]): void {
    this.selectedRoles.set(roleNames);
  }
}
