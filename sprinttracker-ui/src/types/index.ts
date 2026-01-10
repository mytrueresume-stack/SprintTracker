export enum UserRole {
  Admin = 0,
  Manager = 1,
  Developer = 2,
}

export enum ProjectStatus {
  Active = 0,
  OnHold = 1,
  Completed = 2,
  Archived = 3,
}

export enum SprintStatus {
  Planning = 0,
  Active = 1,
  Completed = 2,
  Cancelled = 3,
}

export enum SubmissionStatus {
  Draft = 0,
  Submitted = 1,
  Reviewed = 2,
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  avatar?: string;
  isActive: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface Project {
  id: string;
  name: string;
  key: string;
  description?: string;
  ownerId: string;
  teamMembers: User[];
  status: ProjectStatus;
  startDate?: string;
  targetEndDate?: string;
  settings: ProjectSettings;
  createdAt: string;
}

export interface ProjectSettings {
  defaultSprintDurationDays: number;
  workingDays: number[];
  estimationUnit: number;
}

export interface Sprint {
  id: string;
  projectId: string;
  name: string;
  goal?: string;
  sprintNumber: number;
  status: SprintStatus;
  startDate: string;
  endDate: string;
  capacity: SprintCapacity;
  actualVelocity?: number;
  startedAt?: string;
  completedAt?: string;
  retrospective?: SprintRetrospective;
  stats: SprintStats;
}

export interface SprintCapacity {
  plannedStoryPoints: number;
  committedStoryPoints: number;
  totalAvailableHours: number;
  memberCapacities: MemberCapacity[];
}

export interface MemberCapacity {
  userId: string;
  availableHours: number;
  daysOff: string[];
}

export interface SprintRetrospective {
  whatWentWell: string[];
  whatCouldImprove: string[];
  actionItems: string[];
  teamMorale: number;
  notes?: string;
}

export interface SprintStats {
  totalTasks: number;
  completedTasks: number;
  totalStoryPoints: number;
  completedStoryPoints: number;
  completionPercentage: number;
}

export interface SprintSubmission {
  id: string;
  sprintId: string;
  userId: string;
  projectId: string;
  storyPointsCompleted: number;
  storyPointsPlanned: number;
  hoursWorked: number;
  userStories: UserStoryEntry[];
  featuresDelivered: FeatureEntry[];
  impediments: ImpedimentEntry[];
  appreciations: AppreciationEntry[];
  achievements?: string;
  learnings?: string;
  nextSprintGoals?: string;
  additionalNotes?: string;
  status: SubmissionStatus;
  submittedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UserStoryEntry {
  id: string;
  storyId: string;
  title: string;
  description?: string;
  storyPoints: number;
  status: string;
  remarks?: string;
}

export interface FeatureEntry {
  id: string;
  featureName: string;
  description?: string;
  module?: string;
  status: string;
}

export interface ImpedimentEntry {
  id: string;
  description: string;
  category: string;
  impact: string;
  status: string;
  resolution?: string;
  reportedDate: string;
  resolvedDate?: string;
}

export interface AppreciationEntry {
  id: string;
  appreciatedUserId?: string;
  appreciatedUserName: string;
  reason: string;
  category: string;
}

export interface DashboardStats {
  totalProjects: number;
  activeSprints: number;
  totalTasks: number;
  tasksInProgress: number;
  tasksCompleted: number;
  tasksBlocked: number;
  recentSprints: SprintSummary[];
  myTasks: unknown[];
}

export interface SprintSummary {
  id: string;
  name: string;
  projectName: string;
  status: SprintStatus;
  endDate: string;
  completionPercentage: number;
  daysRemaining: number;
  completionSource?: string;
}

export interface SprintReportData {
  sprintId: string;
  sprintName: string;
  sprintNumber: number;
  startDate: string;
  endDate: string;
  totalTeamMembers: number;
  totalStoryPointsPlanned: number;
  totalStoryPointsCompleted: number;
  completionPercentage: number;
  totalHoursWorked: number;
  totalUserStories: number;
  totalFeatures: number;
  totalImpediments: number;
  openImpediments: number;
  totalAppreciations: number;
  userBreakdown: UserSprintSummary[];
  userStories: UserStoryReport[];
  features: FeatureReport[];
  impediments: ImpedimentReport[];
  appreciations: AppreciationReport[];
}

export interface UserSprintSummary {
  userId: string;
  userName: string;
  storyPointsPlanned: number;
  storyPointsCompleted: number;
  hoursWorked: number;
  userStoriesCount: number;
  featuresCount: number;
  impedimentsCount: number;
  appreciationsGiven: number;
  submissionStatus: string;
}

export interface UserStoryReport {
  storyId: string;
  title: string;
  storyPoints: number;
  status: string;
  reportedBy: string;
}

export interface FeatureReport {
  featureName: string;
  description?: string;
  module?: string;
  status: string;
  deliveredBy: string;
}

export interface ImpedimentReport {
  description: string;
  category: string;
  impact: string;
  status: string;
  resolution?: string;
  reportedBy: string;
  reportedDate: string;
}

export interface AppreciationReport {
  appreciatedUserName: string;
  reason: string;
  category: string;
  givenBy: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
