import api from '@/lib/api';
import { ApiResponse, SprintSubmission, SprintReportData } from '@/types';

export interface SprintSubmissionRequest {
  storyPointsCompleted: number;
  storyPointsPlanned: number;
  hoursWorked: number;
  userStories?: UserStoryRequest[];
  featuresDelivered?: FeatureRequest[];
  impediments?: ImpedimentRequest[];
  appreciations?: AppreciationRequest[];
  achievements?: string;
  learnings?: string;
  nextSprintGoals?: string;
  additionalNotes?: string;
}

export interface UserStoryRequest {
  storyId: string;
  title: string;
  description?: string;
  storyPoints: number;
  status: string;
  remarks?: string;
}

export interface FeatureRequest {
  featureName: string;
  description?: string;
  module?: string;
  status: string;
}

export interface ImpedimentRequest {
  description: string;
  category: string;
  impact: string;
  status: string;
  resolution?: string;
  reportedDate?: string;
}

export interface AppreciationRequest {
  appreciatedUserId?: string;
  appreciatedUserName: string;
  reason: string;
  category: string;
}

export const sprintSubmissionService = {
  async getMySubmission(sprintId: string): Promise<ApiResponse<SprintSubmission>> {
    const response = await api.get<ApiResponse<SprintSubmission>>(`/sprintsubmissions/sprint/${sprintId}`);
    return response.data;
  },

  async saveSubmission(sprintId: string, data: SprintSubmissionRequest): Promise<ApiResponse<SprintSubmission>> {
    // Sanitize the payload to avoid sending empty required items which cause server-side validation 400
    const sanitized: SprintSubmissionRequest = {
      storyPointsCompleted: Number.isFinite(Number(data.storyPointsCompleted)) ? Number(data.storyPointsCompleted) : 0,
      storyPointsPlanned: Number.isFinite(Number(data.storyPointsPlanned)) ? Number(data.storyPointsPlanned) : 0,
      hoursWorked: Number.isFinite(Number(data.hoursWorked)) ? Number(data.hoursWorked) : 0,
      achievements: data.achievements?.trim(),
      learnings: data.learnings?.trim(),
      nextSprintGoals: data.nextSprintGoals?.trim(),
      additionalNotes: data.additionalNotes?.trim(),
    } as SprintSubmissionRequest;

    if (data.userStories && data.userStories.length > 0) {
      sanitized.userStories = data.userStories
        .map((u) => ({
          storyId: (u.storyId || '').toString().trim(),
          title: (u.title || '').toString().trim(),
          description: u.description?.toString().trim(),
          storyPoints: Number.isFinite(Number(u.storyPoints)) ? Math.max(0, Math.min(100, Math.trunc(Number(u.storyPoints)))) : 0,
          status: (u.status || 'Completed').toString().trim(),
          remarks: u.remarks?.toString().trim(),
        }))
        .filter((u) => u.storyId && u.title);
    }

    if (data.featuresDelivered && data.featuresDelivered.length > 0) {
      sanitized.featuresDelivered = data.featuresDelivered
        .map((f) => ({
          featureName: (f.featureName || '').toString().trim(),
          description: f.description?.toString().trim(),
          module: f.module?.toString().trim(),
          status: (f.status || 'Delivered').toString().trim(),
        }))
        .filter((f) => f.featureName);
    }

    if (data.impediments && data.impediments.length > 0) {
      sanitized.impediments = data.impediments
        .map((i) => ({
          description: (i.description || '').toString().trim(),
          category: (i.category || 'Technical').toString().trim(),
          impact: (i.impact || 'Medium').toString().trim(),
          status: (i.status || 'Open').toString().trim(),
          resolution: i.resolution?.toString().trim(),
          reportedDate: i.reportedDate,
        }))
        .filter((i) => i.description);
    }

    if (data.appreciations && data.appreciations.length > 0) {
      sanitized.appreciations = data.appreciations
        .map((a) => ({
          appreciatedUserId: a.appreciatedUserId || undefined,
          appreciatedUserName: (a.appreciatedUserName || '').toString().trim(),
          reason: (a.reason || '').toString().trim(),
          category: (a.category || 'Teamwork').toString().trim(),
        }))
        .filter((a) => a.appreciatedUserName && a.reason);
    }

    const response = await api.post<ApiResponse<SprintSubmission>>(`/sprintsubmissions/sprint/${sprintId}`, sanitized);
    return response.data;
  },

  async submitSubmission(submissionId: string): Promise<ApiResponse<SprintSubmission>> {
    const response = await api.post<ApiResponse<SprintSubmission>>(`/sprintsubmissions/${submissionId}/submit`);
    return response.data;
  },

  async reopenSubmission(submissionId: string): Promise<ApiResponse<SprintSubmission>> {
    const response = await api.post<ApiResponse<SprintSubmission>>(`/sprintsubmissions/${submissionId}/reopen`);
    return response.data;
  },

  async getAllSprintSubmissions(sprintId: string): Promise<ApiResponse<SprintSubmission[]>> {
    const response = await api.get<ApiResponse<SprintSubmission[]>>(`/sprintsubmissions/sprint/${sprintId}/all`);
    return response.data;
  },

  async getSprintReport(sprintId: string): Promise<ApiResponse<SprintReportData>> {
    const response = await api.get<ApiResponse<SprintReportData>>(`/sprintsubmissions/sprint/${sprintId}/report`);
    return response.data;
  },

  async getMySubmissions(): Promise<ApiResponse<SprintSubmission[]>> {
    const response = await api.get<ApiResponse<SprintSubmission[]>>('/sprintsubmissions/my-submissions');
    return response.data;
  },

  async deleteSubmission(submissionId: string): Promise<ApiResponse<boolean>> {
    const response = await api.delete<ApiResponse<boolean>>(`/sprintsubmissions/${submissionId}`);
    return response.data;
  },
};
