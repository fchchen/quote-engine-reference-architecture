import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RouterTestingModule } from '@angular/router/testing';
import { UsersPage, UserDetail, Role } from './users.page';
import { environment } from '../../../environments/environment';

describe('UsersPage', () => {
  let component: UsersPage;
  let fixture: ComponentFixture<UsersPage>;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  const mockUsers: UserDetail[] = [
    { id: 'u1', displayName: 'Alice Admin', email: 'alice@test.com', roles: ['Admin'], lastLoginAt: '2024-01-15T10:00:00Z', isActive: true },
    { id: 'u2', displayName: 'Bob Member', email: 'bob@test.com', roles: ['Member'], lastLoginAt: null, isActive: true }
  ];

  const mockRoles: Role[] = [
    { id: 'r1', name: 'Admin', description: 'Full access' },
    { id: 'r2', name: 'Member', description: 'Standard access' },
    { id: 'r3', name: 'Viewer', description: 'Read-only access' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UsersPage, HttpClientTestingModule, NoopAnimationsModule, RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(UsersPage);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushInit(): void {
    const usersReq = httpMock.expectOne(`${apiUrl}/users`);
    usersReq.flush(mockUsers);
    const rolesReq = httpMock.expectOne(`${apiUrl}/roles`);
    rolesReq.flush(mockRoles);
  }

  it('should create', () => {
    fixture.detectChanges();
    flushInit();
    expect(component).toBeTruthy();
  });

  it('should load users and roles on init', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();
    expect(component.users().length).toBe(2);
    expect(component.roles().length).toBe(3);
    expect(component.isLoading()).toBeFalse();
  }));

  it('should toggle invite form', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    expect(component.showInviteForm()).toBeFalse();
    component.showInviteForm.set(true);
    expect(component.showInviteForm()).toBeTrue();
  }));

  it('should validate invite form', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    expect(component.inviteForm.valid).toBeFalse();
    component.inviteForm.patchValue({
      email: 'invalid',
      displayName: 'Test',
      password: 'short'
    });
    expect(component.inviteForm.valid).toBeFalse();

    component.inviteForm.patchValue({
      email: 'test@test.com',
      password: 'longpassword'
    });
    expect(component.inviteForm.valid).toBeTrue();
  }));

  it('should invite a user', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.showInviteForm.set(true);
    component.inviteForm.patchValue({
      email: 'charlie@test.com',
      displayName: 'Charlie',
      password: 'password123',
      role: 'Member'
    });
    component.inviteUser();

    const req = httpMock.expectOne(`${apiUrl}/users/invite`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('charlie@test.com');
    req.flush({
      id: 'u3', displayName: 'Charlie', email: 'charlie@test.com',
      roles: ['Member'], lastLoginAt: null, isActive: true
    });
    tick();

    expect(component.users().length).toBe(3);
    expect(component.showInviteForm()).toBeFalse();
  }));

  it('should start editing roles', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditingRoles(mockUsers[1]);
    expect(component.editingUserId()).toBe('u2');
    expect(component.selectedRoles()).toEqual(['Member']);
  }));

  it('should cancel editing roles', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditingRoles(mockUsers[1]);
    component.cancelEditingRoles();
    expect(component.editingUserId()).toBeNull();
    expect(component.selectedRoles()).toEqual([]);
  }));

  it('should update roles via PUT', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditingRoles(mockUsers[1]);
    component.selectedRoles.set(['Admin', 'Member']);
    component.updateRoles();

    const req = httpMock.expectOne(`${apiUrl}/users/u2/roles`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.roles).toEqual(['Admin', 'Member']);
    req.flush({
      id: 'u2', displayName: 'Bob Member', email: 'bob@test.com',
      roles: ['Admin', 'Member'], lastLoginAt: null, isActive: true
    });
    tick();

    const updated = component.users().find(u => u.id === 'u2');
    expect(updated?.roles).toEqual(['Admin', 'Member']);
    expect(component.editingUserId()).toBeNull();
  }));
});
