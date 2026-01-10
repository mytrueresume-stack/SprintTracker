'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { sprintService } from '@/services/sprintService';
import { sprintSubmissionService, UserStoryRequest, FeatureRequest, ImpedimentRequest, AppreciationRequest } from '@/services/sprintSubmissionService';
import { Sprint, SprintSubmission, SubmissionStatus } from '@/types';
import { Card, Button, Loader, Input, Textarea, Badge } from '@/components/ui';
import { ArrowLeft, Save, Send, Plus, Trash2, CheckCircle } from 'lucide-react';

export default function SprintSubmissionPage() {
  const params = useParams();
  const router = useRouter();
  const sprintId = params.id as string;

  const [sprint, setSprint] = useState<Sprint | null>(null);
  const [submission, setSubmission] = useState<SprintSubmission | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [formData, setFormData] = useState({
    storyPointsCompleted: 0,
    storyPointsPlanned: 0,
    hoursWorked: 0,
    achievements: '',
    learnings: '',
    nextSprintGoals: '',
    additionalNotes: '',
  });

  const [userStories, setUserStories] = useState<UserStoryRequest[]>([]);
  const [features, setFeatures] = useState<FeatureRequest[]>([]);
  const [impediments, setImpediments] = useState<ImpedimentRequest[]>([]);
  const [appreciations, setAppreciations] = useState<AppreciationRequest[]>([]);

  useEffect(() => {
    if (sprintId) {
      loadData();
    }
  }, [sprintId]);

  const loadData = async () => {
    try {
      const [sprintRes, submissionRes] = await Promise.all([
        sprintService.getSprint(sprintId),
        sprintSubmissionService.getMySubmission(sprintId).catch(() => ({ success: false, data: null })),
      ]);

      if (sprintRes.success && sprintRes.data) {
        setSprint(sprintRes.data);
      }

      if (submissionRes.success && submissionRes.data) {
        const sub = submissionRes.data;
        setSubmission(sub);
        setFormData({
          storyPointsCompleted: sub.storyPointsCompleted,
          storyPointsPlanned: sub.storyPointsPlanned,
          hoursWorked: sub.hoursWorked,
          achievements: sub.achievements || '',
          learnings: sub.learnings || '',
          nextSprintGoals: sub.nextSprintGoals || '',
          additionalNotes: sub.additionalNotes || '',
        });
        setUserStories(sub.userStories?.map(s => ({
          storyId: s.storyId,
          title: s.title,
          description: s.description,
          storyPoints: s.storyPoints,
          status: s.status,
          remarks: s.remarks,
        })) || []);
        setFeatures(sub.featuresDelivered?.map(f => ({
          featureName: f.featureName,
          description: f.description,
          module: f.module,
          status: f.status,
        })) || []);
        setImpediments(sub.impediments?.map(i => ({
          description: i.description,
          category: i.category,
          impact: i.impact,
          status: i.status,
          resolution: i.resolution,
        })) || []);
        setAppreciations(sub.appreciations?.map(a => ({
          appreciatedUserId: a.appreciatedUserId,
          appreciatedUserName: a.appreciatedUserName,
          reason: a.reason,
          category: a.category,
        })) || []);
      }
    } catch (err) {
      setError('Failed to load data');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    setError('');
    setSuccessMessage('');

    try {
      const response = await sprintSubmissionService.saveSubmission(sprintId, {
        ...formData,
        userStories,
        featuresDelivered: features,
        impediments,
        appreciations,
      });

      if (response.success && response.data) {
        setSubmission(response.data);
        setSuccessMessage('Draft saved successfully');
      } else {
        setError(response.message || 'Failed to save');
      }
    } catch (err) {
      setError('Failed to save submission');
      console.error(err);
    } finally {
      setIsSaving(false);
    }
  };

  const handleSubmit = async () => {
    if (!submission) {
      await handleSave();
    }

    setIsSubmitting(true);
    setError('');

    try {
      const subId = submission?.id;
      if (!subId) {
        const saveRes = await sprintSubmissionService.saveSubmission(sprintId, {
          ...formData,
          userStories,
          featuresDelivered: features,
          impediments,
          appreciations,
        });
        if (saveRes.success && saveRes.data) {
          const submitRes = await sprintSubmissionService.submitSubmission(saveRes.data.id);
          if (submitRes.success) {
            setSuccessMessage('Submission completed successfully');
            router.push(`/sprints/${sprintId}/report`);
          }
        }
      } else {
        const response = await sprintSubmissionService.submitSubmission(subId);
        if (response.success) {
          setSuccessMessage('Submission completed successfully');
          router.push(`/sprints/${sprintId}/report`);
        } else {
          setError(response.message || 'Failed to submit');
        }
      }
    } catch (err) {
      setError('Failed to submit');
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const addUserStory = () => {
    setUserStories([...userStories, {
      storyId: `US-${Date.now()}`,
      title: '',
      storyPoints: 0,
      status: 'Completed',
    }]);
  };

  const addFeature = () => {
    setFeatures([...features, {
      featureName: '',
      status: 'Completed',
    }]);
  };

  const addImpediment = () => {
    setImpediments([...impediments, {
      description: '',
      category: 'Technical',
      impact: 'Medium',
      status: 'Open',
    }]);
  };

  const addAppreciation = () => {
    setAppreciations([...appreciations, {
      appreciatedUserName: '',
      reason: '',
      category: 'Teamwork',
    }]);
  };

  const isReadOnly = submission?.status === SubmissionStatus.Submitted;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader size="lg" />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-6">
        <Link href={sprint ? `/projects/${sprint.projectId}` : '/projects'} className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700 mb-4">
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Project
        </Link>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Sprint Submission</h1>
            <p className="text-gray-600 mt-1">{sprint?.name}</p>
          </div>
          {submission && (
            <Badge variant={submission.status === SubmissionStatus.Submitted ? 'success' : 'warning'}>
              {submission.status === SubmissionStatus.Submitted ? 'Submitted' : 'Draft'}
            </Badge>
          )}
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 flex items-center">
          <CheckCircle className="h-5 w-5 mr-2" />
          {successMessage}
        </div>
      )}

      <div className="space-y-6">
        <Card>
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Story Points & Hours</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input
              id="storyPointsPlanned"
              type="number"
              label="Story Points Planned"
              value={formData.storyPointsPlanned}
              onChange={(e) => setFormData({ ...formData, storyPointsPlanned: Number(e.target.value) })}
              disabled={isReadOnly}
              min={0}
            />
            <Input
              id="storyPointsCompleted"
              type="number"
              label="Story Points Completed"
              value={formData.storyPointsCompleted}
              onChange={(e) => setFormData({ ...formData, storyPointsCompleted: Number(e.target.value) })}
              disabled={isReadOnly}
              min={0}
            />
            <Input
              id="hoursWorked"
              type="number"
              label="Hours Worked"
              value={formData.hoursWorked}
              onChange={(e) => setFormData({ ...formData, hoursWorked: Number(e.target.value) })}
              disabled={isReadOnly}
              min={0}
            />
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">User Stories</h2>
            {!isReadOnly && (
              <Button size="sm" variant="outline" onClick={addUserStory}>
                <Plus className="h-4 w-4 mr-1" />
                Add Story
              </Button>
            )}
          </div>
          {userStories.length > 0 ? (
            <div className="space-y-4">
              {userStories.map((story, index) => (
                <div key={index} className="p-4 bg-gray-50 rounded-lg">
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <Input
                      label="Story ID"
                      value={story.storyId}
                      onChange={(e) => {
                        const updated = [...userStories];
                        updated[index].storyId = e.target.value;
                        setUserStories(updated);
                      }}
                      disabled={isReadOnly}
                    />
                    <div className="md:col-span-2">
                      <Input
                        label="Title"
                        value={story.title}
                        onChange={(e) => {
                          const updated = [...userStories];
                          updated[index].title = e.target.value;
                          setUserStories(updated);
                        }}
                        disabled={isReadOnly}
                      />
                    </div>
                    <Input
                      type="number"
                      label="Story Points"
                      value={story.storyPoints}
                      onChange={(e) => {
                        const updated = [...userStories];
                        updated[index].storyPoints = Number(e.target.value);
                        setUserStories(updated);
                      }}
                      disabled={isReadOnly}
                      min={0}
                    />
                  </div>
                  {!isReadOnly && (
                    <Button
                      size="sm"
                      variant="ghost"
                      className="mt-2 text-red-600"
                      onClick={() => setUserStories(userStories.filter((_, i) => i !== index))}
                    >
                      <Trash2 className="h-4 w-4 mr-1" />
                      Remove
                    </Button>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-sm">No user stories added yet</p>
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Features Delivered</h2>
            {!isReadOnly && (
              <Button size="sm" variant="outline" onClick={addFeature}>
                <Plus className="h-4 w-4 mr-1" />
                Add Feature
              </Button>
            )}
          </div>
          {features.length > 0 ? (
            <div className="space-y-4">
              {features.map((feature, index) => (
                <div key={index} className="p-4 bg-gray-50 rounded-lg">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <Input
                      label="Feature Name"
                      value={feature.featureName}
                      onChange={(e) => {
                        const updated = [...features];
                        updated[index].featureName = e.target.value;
                        setFeatures(updated);
                      }}
                      disabled={isReadOnly}
                    />
                    <Input
                      label="Module"
                      value={feature.module || ''}
                      onChange={(e) => {
                        const updated = [...features];
                        updated[index].module = e.target.value;
                        setFeatures(updated);
                      }}
                      disabled={isReadOnly}
                    />
                  </div>
                  {!isReadOnly && (
                    <Button
                      size="sm"
                      variant="ghost"
                      className="mt-2 text-red-600"
                      onClick={() => setFeatures(features.filter((_, i) => i !== index))}
                    >
                      <Trash2 className="h-4 w-4 mr-1" />
                      Remove
                    </Button>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-sm">No features added yet</p>
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Impediments</h2>
            {!isReadOnly && (
              <Button size="sm" variant="outline" onClick={addImpediment}>
                <Plus className="h-4 w-4 mr-1" />
                Add Impediment
              </Button>
            )}
          </div>
          {impediments.length > 0 ? (
            <div className="space-y-4">
              {impediments.map((impediment, index) => (
                <div key={index} className="p-4 bg-gray-50 rounded-lg">
                  <Textarea
                    label="Description"
                    value={impediment.description}
                    onChange={(e) => {
                      const updated = [...impediments];
                      updated[index].description = e.target.value;
                      setImpediments(updated);
                    }}
                    disabled={isReadOnly}
                    rows={2}
                  />
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-3">
                    <Input
                      label="Category"
                      value={impediment.category}
                      onChange={(e) => {
                        const updated = [...impediments];
                        updated[index].category = e.target.value;
                        setImpediments(updated);
                      }}
                      disabled={isReadOnly}
                    />
                    <Input
                      label="Impact"
                      value={impediment.impact}
                      onChange={(e) => {
                        const updated = [...impediments];
                        updated[index].impact = e.target.value;
                        setImpediments(updated);
                      }}
                      disabled={isReadOnly}
                    />
                    <Input
                      label="Status"
                      value={impediment.status}
                      onChange={(e) => {
                        const updated = [...impediments];
                        updated[index].status = e.target.value;
                        setImpediments(updated);
                      }}
                      disabled={isReadOnly}
                    />
                  </div>
                  {!isReadOnly && (
                    <Button
                      size="sm"
                      variant="ghost"
                      className="mt-2 text-red-600"
                      onClick={() => setImpediments(impediments.filter((_, i) => i !== index))}
                    >
                      <Trash2 className="h-4 w-4 mr-1" />
                      Remove
                    </Button>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-sm">No impediments reported</p>
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Team Appreciations</h2>
            {!isReadOnly && (
              <Button size="sm" variant="outline" onClick={addAppreciation}>
                <Plus className="h-4 w-4 mr-1" />
                Add Appreciation
              </Button>
            )}
          </div>
          {appreciations.length > 0 ? (
            <div className="space-y-4">
              {appreciations.map((appreciation, index) => (
                <div key={index} className="p-4 bg-gray-50 rounded-lg">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <Input
                      label="Team Member Name"
                      value={appreciation.appreciatedUserName}
                      onChange={(e) => {
                        const updated = [...appreciations];
                        updated[index].appreciatedUserName = e.target.value;
                        setAppreciations(updated);
                      }}
                      disabled={isReadOnly}
                    />
                    <Input
                      label="Category"
                      value={appreciation.category}
                      onChange={(e) => {
                        const updated = [...appreciations];
                        updated[index].category = e.target.value;
                        setAppreciations(updated);
                      }}
                      disabled={isReadOnly}
                    />
                  </div>
                  <Textarea
                    label="Reason"
                    value={appreciation.reason}
                    onChange={(e) => {
                      const updated = [...appreciations];
                      updated[index].reason = e.target.value;
                      setAppreciations(updated);
                    }}
                    disabled={isReadOnly}
                    rows={2}
                    className="mt-3"
                  />
                  {!isReadOnly && (
                    <Button
                      size="sm"
                      variant="ghost"
                      className="mt-2 text-red-600"
                      onClick={() => setAppreciations(appreciations.filter((_, i) => i !== index))}
                    >
                      <Trash2 className="h-4 w-4 mr-1" />
                      Remove
                    </Button>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-sm">No appreciations added yet</p>
          )}
        </Card>

        <Card>
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Additional Information</h2>
          <div className="space-y-4">
            <Textarea
              id="achievements"
              label="Achievements"
              value={formData.achievements}
              onChange={(e) => setFormData({ ...formData, achievements: e.target.value })}
              disabled={isReadOnly}
              rows={3}
              placeholder="What did you achieve this sprint?"
            />
            <Textarea
              id="learnings"
              label="Learnings"
              value={formData.learnings}
              onChange={(e) => setFormData({ ...formData, learnings: e.target.value })}
              disabled={isReadOnly}
              rows={3}
              placeholder="What did you learn?"
            />
            <Textarea
              id="nextSprintGoals"
              label="Next Sprint Goals"
              value={formData.nextSprintGoals}
              onChange={(e) => setFormData({ ...formData, nextSprintGoals: e.target.value })}
              disabled={isReadOnly}
              rows={3}
              placeholder="What are your goals for the next sprint?"
            />
            <Textarea
              id="additionalNotes"
              label="Additional Notes"
              value={formData.additionalNotes}
              onChange={(e) => setFormData({ ...formData, additionalNotes: e.target.value })}
              disabled={isReadOnly}
              rows={3}
              placeholder="Any other notes or comments"
            />
          </div>
        </Card>

        {!isReadOnly && (
          <div className="flex justify-end space-x-3">
            <Button variant="outline" onClick={handleSave} isLoading={isSaving}>
              <Save className="h-4 w-4 mr-2" />
              Save Draft
            </Button>
            <Button onClick={handleSubmit} isLoading={isSubmitting}>
              <Send className="h-4 w-4 mr-2" />
              Submit
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
