import type { LearningProfile, LearningProfileOptions, PutLearningProfileRequest } from './learning-profile.types'
import { invalidateDiscover } from '../discover/discover'

export function useLearningProfileApi() {
  const api = useApi()
  return {
    get: (): Promise<LearningProfile> => api.get('/learning-profile'),
    getOptions: (): Promise<LearningProfileOptions> => api.get('/learning-profile/options'),
    put: async (request: PutLearningProfileRequest): Promise<LearningProfile> => {
      const profile = await api.put<LearningProfile>('/learning-profile', request)
      invalidateDiscover()
      return profile
    },
  }
}
