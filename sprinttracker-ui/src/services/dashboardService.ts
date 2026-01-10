import api from '@/lib/api';
import { ApiResponse, DashboardStats } from '@/types';

export interface ActivityLog {
  id: string;
  userId: string;
  action: string;
  entityType: string;
  entityId: string;
  details?: string;
  timestamp: string;
}

export const dashboardService = {
  async getDashboardStats(): Promise<ApiResponse<DashboardStats>> {
    try {
      const response = await api.get<ApiResponse<DashboardStats>>('/dashboard');
      return response.data;
    } catch (err) {
      console.error('Failed to fetch dashboard stats:', err);
      throw err;
    }
  },

  async getRecentActivity(count = 20): Promise<ApiResponse<ActivityLog[]>> {
    try {
      const response = await api.get<ApiResponse<ActivityLog[]>>('/dashboard/activity', {
        params: { count },
      });
      return response.data;
    } catch (err) {
      console.error('Failed to fetch recent activity:', err);
      throw err;
    }
  },
};
