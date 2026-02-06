import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RouterTestingModule } from '@angular/router/testing';
import { TeamsPage, Team, User } from './teams.page';
import { environment } from '../../../environments/environment';

describe('TeamsPage', () => {
  let component: TeamsPage;
  let fixture: ComponentFixture<TeamsPage>;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  const mockTeams: Team[] = [
    {
      id: 't1', name: 'Engineering', description: 'Dev team', memberCount: 2,
      members: [
        { userId: 'u1', displayName: 'Alice', email: 'alice@test.com', role: 'Admin' },
        { userId: 'u2', displayName: 'Bob', email: 'bob@test.com', role: 'Member' }
      ],
      createdAt: '2024-01-01'
    },
    {
      id: 't2', name: 'Design', description: null, memberCount: 0, members: [],
      createdAt: '2024-01-02'
    }
  ];

  const mockUsers: User[] = [
    { id: 'u1', displayName: 'Alice', email: 'alice@test.com' },
    { id: 'u2', displayName: 'Bob', email: 'bob@test.com' },
    { id: 'u3', displayName: 'Charlie', email: 'charlie@test.com' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeamsPage, HttpClientTestingModule, NoopAnimationsModule, RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(TeamsPage);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushInit(): void {
    const teamsReq = httpMock.expectOne(`${apiUrl}/teams`);
    teamsReq.flush(mockTeams);
    const usersReq = httpMock.expectOne(`${apiUrl}/users`);
    usersReq.flush(mockUsers);
  }

  it('should create', () => {
    fixture.detectChanges();
    flushInit();
    expect(component).toBeTruthy();
  });

  it('should load teams and users on init', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();
    expect(component.teams().length).toBe(2);
    expect(component.users().length).toBe(3);
    expect(component.isLoading()).toBeFalse();
  }));

  it('should create a team', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.showCreateForm.set(true);
    component.createForm.patchValue({ name: 'QA Team', description: 'Quality assurance' });
    component.createTeam();

    const req = httpMock.expectOne(`${apiUrl}/teams`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 't3', name: 'QA Team', description: 'Quality assurance', memberCount: 0, members: [], createdAt: '2024-01-03' });
    tick();

    expect(component.teams().length).toBe(3);
    expect(component.showCreateForm()).toBeFalse();
  }));

  // Gap 2 tests - Team inline edit
  it('should start editing a team', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockTeams[0]);
    expect(component.editingTeamId()).toBe('t1');
    expect(component.editForm.value.name).toBe('Engineering');
  }));

  it('should cancel editing', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockTeams[0]);
    component.cancelEditing();
    expect(component.editingTeamId()).toBeNull();
  }));

  it('should update a team via PUT', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockTeams[0]);
    component.editForm.patchValue({ name: 'Engineering v2', description: 'Updated' });
    component.updateTeam();

    const req = httpMock.expectOne(`${apiUrl}/teams/t1`);
    expect(req.request.method).toBe('PUT');
    req.flush({ id: 't1', name: 'Engineering v2', description: 'Updated', memberCount: 2, members: [], createdAt: '2024-01-01' });
    tick();

    const updated = component.teams().find(t => t.id === 't1');
    expect(updated?.name).toBe('Engineering v2');
    expect(updated?.members.length).toBe(2); // preserves local members
    expect(component.editingTeamId()).toBeNull();
  }));

  it('should not update if edit form is invalid', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.startEditing(mockTeams[0]);
    component.editForm.patchValue({ name: '' });
    component.updateTeam();

    httpMock.expectNone(`${apiUrl}/teams/t1`);
  }));

  // Gap 3 tests - Team member management
  it('should toggle manage members mode', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.toggleManageMembers('t1');
    expect(component.managingTeamId()).toBe('t1');

    component.toggleManageMembers('t1');
    expect(component.managingTeamId()).toBeNull();
  }));

  it('should filter available users (exclude existing members)', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    const available = component.getAvailableUsers(component.teams()[0]);
    expect(available.length).toBe(1);
    expect(available[0].id).toBe('u3');
  }));

  it('should add a member', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.selectedUserId.set('u3');
    component.addMember('t1');

    const req = httpMock.expectOne(`${apiUrl}/teams/t1/members`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.userId).toBe('u3');
    req.flush({ userId: 'u3', displayName: 'Charlie', email: 'charlie@test.com', role: 'Member' });
    tick();

    const team = component.teams().find(t => t.id === 't1');
    expect(team?.members.length).toBe(3);
    expect(team?.memberCount).toBe(3);
  }));

  it('should remove a member', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.removeMember('t1', 'u2');

    const req = httpMock.expectOne(`${apiUrl}/teams/t1/members/u2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    tick();

    const team = component.teams().find(t => t.id === 't1');
    expect(team?.members.length).toBe(1);
    expect(team?.memberCount).toBe(1);
  }));

  it('should not add member if no user selected', fakeAsync(() => {
    fixture.detectChanges();
    flushInit();
    tick();

    component.selectedUserId.set(null);
    component.addMember('t1');

    httpMock.expectNone(`${apiUrl}/teams/t1/members`);
  }));
});
