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

export interface TeamMember {
  userId: string;
  displayName: string;
  email: string;
  role: string;
}

export interface Team {
  id: string;
  name: string;
  description: string | null;
  memberCount: number;
  members: TeamMember[];
  createdAt: string;
}

export interface User {
  id: string;
  displayName: string;
  email: string;
}

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule, MatProgressSpinnerModule,
    ThemedToolbarComponent
  ],
  templateUrl: './teams.page.html',
  styleUrl: './teams.page.scss'
})
export class TeamsPage implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  teams = signal<Team[]>([]);
  users = signal<User[]>([]);
  isLoading = signal(true);
  showCreateForm = signal(false);
  editingTeamId = signal<string | null>(null);
  managingTeamId = signal<string | null>(null);
  selectedUserId = signal<string | null>(null);

  createForm = this.fb.group({
    name: ['', Validators.required],
    description: ['']
  });

  editForm = this.fb.group({
    name: ['', Validators.required],
    description: ['']
  });

  ngOnInit(): void {
    this.http.get<Team[]>(`${this.apiUrl}/teams`).pipe(
      tap(teams => {
        this.teams.set(teams);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of([]);
      })
    ).subscribe();

    this.http.get<User[]>(`${this.apiUrl}/users`).pipe(
      tap(users => this.users.set(users)),
      catchError(() => of([]))
    ).subscribe();
  }

  createTeam(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.http.post<Team>(`${this.apiUrl}/teams`, {
      name: v.name,
      description: v.description || null
    }).pipe(
      tap(team => {
        this.teams.update(list => [...list, team]);
        this.createForm.reset();
        this.showCreateForm.set(false);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  startEditing(team: Team): void {
    this.editingTeamId.set(team.id);
    this.editForm.patchValue({
      name: team.name,
      description: team.description ?? ''
    });
  }

  cancelEditing(): void {
    this.editingTeamId.set(null);
    this.editForm.reset();
  }

  updateTeam(): void {
    const id = this.editingTeamId();
    if (!id || this.editForm.invalid) return;
    const v = this.editForm.value;
    this.http.put<Team>(`${this.apiUrl}/teams/${id}`, {
      name: v.name,
      description: v.description || null
    }).pipe(
      tap(updated => {
        this.teams.update(list =>
          list.map(t => t.id === id
            ? { ...t, name: updated.name, description: updated.description }
            : t
          )
        );
        this.editingTeamId.set(null);
        this.editForm.reset();
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  toggleManageMembers(teamId: string): void {
    this.managingTeamId.set(this.managingTeamId() === teamId ? null : teamId);
    this.selectedUserId.set(null);
  }

  getAvailableUsers(team: Team): User[] {
    const memberIds = new Set(team.members.map(m => m.userId));
    return this.users().filter(u => !memberIds.has(u.id));
  }

  addMember(teamId: string): void {
    const userId = this.selectedUserId();
    if (!userId) return;
    this.http.post<TeamMember>(`${this.apiUrl}/teams/${teamId}/members`, { userId }).pipe(
      tap(member => {
        this.teams.update(list =>
          list.map(t => t.id === teamId
            ? { ...t, members: [...t.members, member], memberCount: t.memberCount + 1 }
            : t
          )
        );
        this.selectedUserId.set(null);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  removeMember(teamId: string, userId: string): void {
    this.http.delete(`${this.apiUrl}/teams/${teamId}/members/${userId}`).pipe(
      tap(() => {
        this.teams.update(list =>
          list.map(t => t.id === teamId
            ? { ...t, members: t.members.filter(m => m.userId !== userId), memberCount: t.memberCount - 1 }
            : t
          )
        );
      }),
      catchError(() => of(null))
    ).subscribe();
  }
}
