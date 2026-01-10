'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuthStore } from '@/store/authStore';
import { authService } from '@/services/authService';
import { Button, Input, Card, Select } from '@/components/ui';
import { UserPlus } from 'lucide-react';
import { UserRole } from '@/types';

export default function RegisterPage() {
  const router = useRouter();
  const { setAuth } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    role: UserRole.Developer,
  });

  const roleOptions = [
    { value: UserRole.Developer, label: 'Developer' },
    { value: UserRole.Manager, label: 'Manager' },
    { value: UserRole.Admin, label: 'Admin' },
  ];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    // Enforce stronger password rules (match server-side validation)
    const pwd = formData.password;
    if (pwd.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    const complexity = /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])/;
    if (!complexity.test(pwd)) {
      setError('Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character');
      return;
    }

    setIsLoading(true);

    try {
      const response = await authService.register({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        role: formData.role,
      });

      if (response.success && response.data) {
        setAuth(response.data.token, response.data.user);
        router.push('/dashboard');
      } else {
        // Show server side validation errors if present
        if (response.errors && response.errors.length > 0) {
          setError(response.errors.join('; '));
        } else {
          setError(response.message || 'Registration failed');
        }
      }
    } catch (err: any) {
      // Handle Axios and server validation errors
      if (err?.isAxiosError && err.response?.data) {
        const serverData = err.response.data as any;
        if (serverData.errors && serverData.errors.length > 0) {
          setError(serverData.errors.join('; '));
        } else if (serverData.message) {
          setError(serverData.message);
        } else {
          setError(err.message || 'An error occurred');
        }
      } else {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred';
        setError(errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-blue-600">SprintTracker</h1>
          <p className="mt-2 text-gray-600">Create your account</p>
        </div>

        <Card>
          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                {error}
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <Input
                id="firstName"
                type="text"
                label="First name"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                required
                placeholder="John"
              />

              <Input
                id="lastName"
                type="text"
                label="Last name"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                required
                placeholder="Doe"
              />
            </div>

            <Input
              id="email"
              type="email"
              label="Email address"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              required
              autoComplete="email"
              placeholder="john@example.com"
            />

            <Select
              id="role"
              label="Role"
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: Number(e.target.value) as UserRole })}
              options={roleOptions}
            />

            <Input
              id="password"
              type="password"
              label="Password"
              value={formData.password}
              onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              required
              autoComplete="new-password"
              placeholder="At least 8 chars, include upper, lower, number & special"
            />

            <Input
              id="confirmPassword"
              type="password"
              label="Confirm password"
              value={formData.confirmPassword}
              onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
              required
              autoComplete="new-password"
              placeholder="Confirm your password"
            />

            <Button type="submit" className="w-full" isLoading={isLoading}>
              <UserPlus className="h-4 w-4 mr-2" />
              Create account
            </Button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-sm text-gray-600">
              Already have an account?{' '}
              <Link href="/login" className="text-blue-600 hover:text-blue-700 font-medium">
                Sign in
              </Link>
            </p>
          </div>
        </Card>
      </div>
    </div>
  );
}
