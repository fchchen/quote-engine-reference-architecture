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

export interface Project {
  id: string;
  name: string;
  description: string | null;
  status: string;
  category: string | null;
  createdAt: string;
}

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule, MatProgressSpinnerModule,
    ThemedToolbarComponent
  ],
  templateUrl: './projects.page.html',
  styleUrl: './projects.page.scss'
})
export class ProjectsPage implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private apiUrl = environment.apiUrl;

  projects = signal<Project[]>([]);
  isLoading = signal(true);
  showCreateForm = signal(false);
  editingProjectId = signal<string | null>(null);

  createForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    category: ['']
  });

  editForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    status: [''],
    category: ['']
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.http.get<Project[]>(`${this.apiUrl}/projects`).pipe(
      tap(projects => {
        this.projects.set(projects);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.isLoading.set(false);
        return of([]);
      })
    ).subscribe();
  }

  createProject(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.http.post<Project>(`${this.apiUrl}/projects`, {
      name: v.name,
      description: v.description || null,
      category: v.category || null
    }).pipe(
      tap(project => {
        this.projects.update(list => [project, ...list]);
        this.createForm.reset();
        this.showCreateForm.set(false);
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  startEditing(project: Project): void {
    this.editingProjectId.set(project.id);
    this.editForm.patchValue({
      name: project.name,
      description: project.description ?? '',
      status: project.status,
      category: project.category ?? ''
    });
  }

  cancelEditing(): void {
    this.editingProjectId.set(null);
    this.editForm.reset();
  }

  updateProject(): void {
    const id = this.editingProjectId();
    if (!id || this.editForm.invalid) return;
    const v = this.editForm.value;
    this.http.put<Project>(`${this.apiUrl}/projects/${id}`, {
      name: v.name,
      description: v.description || null,
      status: v.status || 'Active',
      category: v.category || null
    }).pipe(
      tap(updated => {
        this.projects.update(list =>
          list.map(p => p.id === id ? updated : p)
        );
        this.editingProjectId.set(null);
        this.editForm.reset();
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  deleteProject(id: string): void {
    this.http.delete(`${this.apiUrl}/projects/${id}`).pipe(
      tap(() => {
        this.projects.update(list => list.filter(p => p.id !== id));
      }),
      catchError(() => of(null))
    ).subscribe();
  }
}
