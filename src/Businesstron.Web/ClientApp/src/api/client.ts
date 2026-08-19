export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    let message = `Request failed: ${response.status}`
    try {
      const parsed = JSON.parse(text)
      message = parsed.detail || parsed.title || parsed.error || message
    } catch {
      if (text) message = text
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) return undefined as T
  return (await response.json()) as T
}

export interface MeResponse { email: string; name: string }

export type SearchSource = 'DataGov' | 'AbnList'
export type RecordFilter =
  | 'errors'
  | 'suitable'
  | 'pushed'
  | 'websites'
  | 'emails'
  | 'webpending'
  | 'webfailed'
  | 'nowebsite'
export type SearchRunStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'

export type WebEnrichmentRunState =
  | 'NotRequested'
  | 'Queued'
  | 'Running'
  | 'Completed'
  | 'Cancelled'
  | 'Failed'

// The stage-aware badge value: the ASIC statuses, plus the web-stage phases that show
// once ASIC is done but the pipeline isn't.
export type OverallStatus =
  | SearchRunStatus
  | 'WebQueued'
  | 'EnrichingWeb'
  | 'WebStopped'
  | 'WebFailed'

export interface SearchRunDto {
  id: string
  source: SearchSource
  startDate: string | null
  endDate: string | null
  status: SearchRunStatus
  overallStatus: OverallStatus
  webEnrichmentState: WebEnrichmentRunState
  cancellationRequested: boolean
  totalItems: number
  processedItems: number
  foundRecords: number
  suitableCount: number
  pushedCount: number
  errorCount: number
  appliedKeywords: string[]
  tags: string[]
  enableWebEnrichment: boolean
  createdBy: string | null
  created: string
  startedAt: string | null
  completedAt: string | null
  error: string | null
  webEmailCount: number | null
  webPendingCount: number | null
}

export type WebEnrichmentStatus =
  | 'NotAttempted'
  | 'Pending'
  | 'Skipped'
  | 'NoWebsite'
  | 'NoEmail'
  | 'Enriched'
  | 'Failed'

export interface PaginatedList<T> {
  items: T[]
  pageNumber: number
  totalPages: number
  totalCount: number
}

export interface BusinessNameRecordDto {
  id: string
  businessName: string | null
  status: string | null
  registrationDate: string | null
  renewalDate: string | null
  holderName: string | null
  holderType: string | null
  holderAbn: string | null
  abnStatus: string | null
  gst: string | null
  hasMultipleBusinessNames: boolean
  enrichmentStatus: 'Pending' | 'Enriched' | 'Failed'
  enrichmentError: string | null
  isSuitable: boolean
  suitabilityReason: string | null
  ontraportStatus: string
  ontraportContactId: string | null
  ontraportError: string | null
  pushedAt: string | null
  webEnrichmentStatus: WebEnrichmentStatus
  webEnrichmentError: string | null
  websites: string | null
  contactEmail: string | null
  contactEmails: string | null
  contactPhone: string | null
  contactSocials: string | null
}

export interface WebEnrichmentSummary {
  state: WebEnrichmentRunState
  active: boolean
  stopping: boolean
  error: string | null
  notAttempted: number
  pending: number
  skipped: number
  noWebsite: number
  noEmail: number
  enriched: number
  failed: number
  withWebsite: number
  withEmail: number
  eligible: number
  processed: number
  running: boolean
  hasActivity: boolean
}

export interface SearchRunDetail {
  run: SearchRunDto
  records: BusinessNameRecordDto[]
  recordsPage: number
  recordsPageSize: number
  recordsTotalCount: number
  recordsTotalPages: number
  pendingCount: number
  sampleFailureReason: string | null
  web: WebEnrichmentSummary
}

export interface FilterKeywordDto { id: string; word: string; isActive: boolean }

export interface UserDto {
  id: string
  email: string | null
  emailConfirmed: boolean
  lockedOut: boolean
}

export interface OntraportConfigDto {
  tagId: number | null
  sequenceId: number | null
  autoPushEnabled: boolean
  isConfigured: boolean
}

export interface OntraportCredentials {
  appId: string | null
  apiKey: string | null
}

export interface TwoCaptchaCredentials {
  apiKey: string | null
  defaultTimeoutSeconds: number
  recaptchaTimeoutSeconds: number
  pollingIntervalSeconds: number
}

export interface AsicSettings {
  maxConcurrency: number
  maxConcurrencyLimit: number
  forceTls13: boolean
  proxyUrl: string | null
  proxyUsername: string | null
  proxyPassword: string | null
}

export interface AsicSettingsInput {
  maxConcurrency: number
  forceTls13: boolean
  proxyUrl: string | null
  proxyUsername: string | null
  proxyPassword: string | null
}

export interface AsicConnectionTestResult {
  succeeded: boolean
  statusCode: number | null
  message: string
}

export interface CreateSearchRunInput {
  source: SearchSource
  startDate?: string | null
  endDate?: string | null
  abnListRaw?: string | null
  keywords?: string[] | null
  tags?: string[] | null
  enableWebEnrichment?: boolean
}

export interface WhoisXmlCredentials {
  apiKey: string | null
}

export const api = {
  me: () => apiFetch<MeResponse>('/api/me'),
  login: (email: string, password: string) =>
    apiFetch<unknown>('/api/login?useCookies=true', { method: 'POST', body: JSON.stringify({ email, password }) }),
  logout: () => apiFetch<void>('/api/logout', { method: 'POST' }),

  searchRuns: {
    list: (page = 1, pageSize = 20) =>
      apiFetch<PaginatedList<SearchRunDto>>(`/api/search-runs?page=${page}&pageSize=${pageSize}`),
    get: (id: string, page = 1, pageSize = 100, filter?: RecordFilter | null) =>
      apiFetch<SearchRunDetail>(`/api/search-runs/${id}?page=${page}&pageSize=${pageSize}${filter ? `&filter=${filter}` : ''}`),
    updateTags: (id: string, tags: string[]) =>
      apiFetch<void>(`/api/search-runs/${id}/tags`, { method: 'PUT', body: JSON.stringify({ tags }) }),
    create: (input: CreateSearchRunInput) =>
      apiFetch<{ id: string }>('/api/search-runs', { method: 'POST', body: JSON.stringify(input) }),
    push: (id: string, onlyWithContact = false) =>
      apiFetch<void>(`/api/search-runs/${id}/push${onlyWithContact ? '?onlyWithContact=true' : ''}`, { method: 'POST' }),
    cancel: (id: string) => apiFetch<void>(`/api/search-runs/${id}/cancel`, { method: 'POST' }),
    retryFailed: (id: string) => apiFetch<void>(`/api/search-runs/${id}/retry`, { method: 'POST' }),
    enrichWeb: (id: string) => apiFetch<void>(`/api/search-runs/${id}/enrich-web`, { method: 'POST' }),
    stopWebEnrichment: (id: string) => apiFetch<void>(`/api/search-runs/${id}/stop-web-enrichment`, { method: 'POST' }),
    remove: (id: string) => apiFetch<void>(`/api/search-runs/${id}`, { method: 'DELETE' }),
    exportHref: (id: string, filter?: 'suitable' | 'contacts') =>
      `/api/search-runs/${id}/export${filter ? `?filter=${filter}` : ''}`,
  },

  users: {
    list: () => apiFetch<UserDto[]>('/api/users'),
    create: (email: string, password: string) =>
      apiFetch<{ id: string }>('/api/users', { method: 'POST', body: JSON.stringify({ email, password }) }),
    resetPassword: (id: string, newPassword: string) =>
      apiFetch<void>(`/api/users/${id}/reset-password`, { method: 'POST', body: JSON.stringify({ newPassword }) }),
    remove: (id: string) => apiFetch<void>(`/api/users/${id}`, { method: 'DELETE' }),
  },

  keywords: {
    list: () => apiFetch<FilterKeywordDto[]>('/api/filter-keywords'),
    create: (word: string) =>
      apiFetch<{ id: string }>('/api/filter-keywords', { method: 'POST', body: JSON.stringify({ word }) }),
    update: (id: string, word: string, isActive: boolean) =>
      apiFetch<void>(`/api/filter-keywords/${id}`, { method: 'PUT', body: JSON.stringify({ word, isActive }) }),
    remove: (id: string) => apiFetch<void>(`/api/filter-keywords/${id}`, { method: 'DELETE' }),
  },

  settings: {
    getOntraport: () => apiFetch<OntraportConfigDto>('/api/settings/ontraport'),
    updateOntraport: (input: { tagId: number | null; sequenceId: number | null; autoPushEnabled: boolean }) =>
      apiFetch<void>('/api/settings/ontraport', { method: 'PUT', body: JSON.stringify(input) }),
    getOntraportCredentials: () => apiFetch<OntraportCredentials>('/api/settings/ontraport-credentials'),
    updateOntraportCredentials: (input: OntraportCredentials) =>
      apiFetch<void>('/api/settings/ontraport-credentials', { method: 'PUT', body: JSON.stringify(input) }),
    getCaptcha: () => apiFetch<TwoCaptchaCredentials>('/api/settings/captcha'),
    updateCaptcha: (input: TwoCaptchaCredentials) =>
      apiFetch<void>('/api/settings/captcha', { method: 'PUT', body: JSON.stringify(input) }),
    getWhoisXml: () => apiFetch<WhoisXmlCredentials>('/api/settings/whoisxml'),
    updateWhoisXml: (input: WhoisXmlCredentials) =>
      apiFetch<void>('/api/settings/whoisxml', { method: 'PUT', body: JSON.stringify(input) }),
    getAsic: () => apiFetch<AsicSettings>('/api/settings/asic'),
    updateAsic: (input: AsicSettingsInput) =>
      apiFetch<void>('/api/settings/asic', { method: 'PUT', body: JSON.stringify(input) }),
    testAsic: (input: AsicSettingsInput) =>
      apiFetch<AsicConnectionTestResult>('/api/settings/asic/test', { method: 'POST', body: JSON.stringify(input) }),
  },
}
