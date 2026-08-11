import type { LearningProfile, LearningProfileOptions, PutLearningProfileRequest } from './learning-profile.types'

export function useLearningProfileApi() {
  const api = useApi()
  return {
    get: (): Promise<LearningProfile> => api.get('/learning-profile'),
    getOptions: (): Promise<LearningProfileOptions> => api.get('/learning-profile/options'),
    put: (request: PutLearningProfileRequest): Promise<LearningProfile> => api.put('/learning-profile', request),
  }
}
