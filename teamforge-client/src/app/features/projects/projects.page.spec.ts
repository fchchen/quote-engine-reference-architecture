import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RouterTestingModule } from '@angular/router/testing';
import { ProjectsPage, Project } from './projects.page';
import { environment } from '../../../environments/environment';

describe('ProjectsPage', () => {
  let component: ProjectsPage;
  let fixture: ComponentFixture<ProjectsPage>;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  const mockProjects: Project[] = [
    { id: '1', name: 'Alpha', description: 'First project', status: 'Active', category: 'Dev', createdAt: '2024-01-01' },
    { id: '2', name: 'Beta', description: 'Second project', status: 'OnHold', category: 'QA', createdAt: '2024-01-02' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectsPage, HttpClientTestingModule, NoopAnimationsModule, RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectsPage);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushInit(): void {
    const req = httpMock.expectOne(`${apiUrl}/projects`);
    req.flush(mockProjects);
  }

  it('should create', () => {
    fixture.detectChanges();
    flushInit();
    expect(component).toBeTruthy();
  });

  it('should load projects on init', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();
    expect(component.projects().length).toBe(2);
    expect(component.isLoading()).toBeFalse();
  }));

  it('should create a project', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.showCreateForm.set(true);
    component.createForm.patchValue({ name: 'Gamma', description: 'New', category: 'Ops' });
    component.createProject();

    const req = httpMock.expectOne(`${apiUrl}/projects`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: '3', name: 'Gamma', description: 'New', status: 'Active', category: 'Ops', createdAt: '2024-01-03' });
    tick();

    expect(component.projects().length).toBe(3);
    expect(component.showCreateForm()).toBeFalse();
  }));

  it('should delete a project', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.deleteProject('1');
    const req = httpMock.expectOne(`${apiUrl}/projects/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    tick();

    expect(component.projects().length).toBe(1);
    expect(component.projects()[0].id).toBe('2');
  }));

  // Gap 1 tests
  it('should start editing a project', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockProjects[0]);
    expect(component.editingProjectId()).toBe('1');
    expect(component.editForm.value.name).toBe('Alpha');
    expect(component.editForm.value.status).toBe('Active');
  }));

  it('should cancel editing', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockProjects[0]);
    expect(component.editingProjectId()).toBe('1');

    component.cancelEditing();
    expect(component.editingProjectId()).toBeNull();
  }));

  it('should update a project via PUT', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockProjects[0]);
    component.editForm.patchValue({ name: 'Alpha Updated', description: 'Updated desc', status: 'Completed', category: 'DevOps' });
    component.updateProject();

    const req = httpMock.expectOne(`${apiUrl}/projects/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.name).toBe('Alpha Updated');
    req.flush({ id: '1', name: 'Alpha Updated', description: 'Updated desc', status: 'Completed', category: 'DevOps', createdAt: '2024-01-01' });
    tick();

    expect(component.projects()[0].name).toBe('Alpha Updated');
    expect(component.editingProjectId()).toBeNull();
  }));

  it('should not update if edit form is invalid', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockProjects[0]);
    component.editForm.patchValue({ name: '' });
    component.updateProject();

    httpMock.expectNone(`${apiUrl}/projects/1`);
  }));
});
