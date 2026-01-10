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
    const response = await api.post<ApiResponse<SprintSubmission>>(`/sprintsubmissions/sprint/${sprintId}`, data);
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
