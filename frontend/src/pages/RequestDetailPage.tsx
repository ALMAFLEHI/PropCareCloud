import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import {
  ArrowLeft,
  Download,
  FileText,
  MessageSquare,
  Paperclip,
  RefreshCcw,
  Send,
  Upload,
  UserCheck,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import {
  addMaintenanceRequestComment,
  assignMaintenanceRequest,
  confirmMaintenanceRequestAttachment,
  createAttachmentDownloadAuthorization,
  createAttachmentUploadAuthorization,
  getMaintenanceRequestById,
  getMaintenanceRequestAttachments,
  getMaintenanceRequestComments,
  getMaintenanceStaff,
  uploadAttachmentDirectlyToS3,
  updateMaintenanceRequestStatus,
} from '../api/propCareApi'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatusBadge from '../components/StatusBadge'
import StatusTimeline from '../components/StatusTimeline'
import { useAuth } from '../context/AuthContext'
import type {
  ApiEnumValue,
  MaintenanceAttachmentResponse,
  MaintenanceRequestCommentResponse,
  MaintenanceRequestResponse,
  UserProfileSummaryResponse,
} from '../types/api'
import {
  formatDateTime,
  getCategoryLabel,
  getPriorityLabel,
} from '../utils/formatters'
import {
  canAssignRequests,
  canUpdateRequestStatus,
  getAllowedStatusValues,
  isAdminOrManager,
} from '../utils/roles'

const statusOptions = [
  { value: '0', label: 'Submitted' },
  { value: '1', label: 'Under review' },
  { value: '2', label: 'Assigned' },
  { value: '3', label: 'In progress' },
  { value: '4', label: 'Completed' },
  { value: '5', label: 'Cancelled' },
]

const statusValueByName: Record<string, string> = {
  submitted: '0',
  underreview: '1',
  assigned: '2',
  inprogress: '3',
  completed: '4',
  cancelled: '5',
}

const allowedAttachmentTypes = new Set([
  'image/jpeg',
  'image/png',
  'image/webp',
  'application/pdf',
])
const maxAttachmentSizeBytes = 10 * 1024 * 1024

function validateAttachment(file: File) {
  if (file.size < 1) {
    return 'The selected attachment is empty.'
  }
  if (file.size > maxAttachmentSizeBytes) {
    return 'The attachment must be 10 MB or smaller.'
  }
  if (!allowedAttachmentTypes.has(file.type)) {
    return 'Select a JPEG, PNG, WebP, or PDF file.'
  }
  return ''
}

function formatFileSize(sizeBytes: number) {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }
  if (sizeBytes < 1024 * 1024) {
    return `${(sizeBytes / 1024).toFixed(1)} KB`
  }
  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`
}

function getStatusValue(status: ApiEnumValue) {
  if (typeof status === 'number') {
    return String(status)
  }

  const parsed = Number(status)
  if (!Number.isNaN(parsed) && status.trim() !== '') {
    return String(parsed)
  }

  return statusValueByName[status.replace(/\s+/g, '').toLowerCase()] ?? '0'
}

function RequestDetailPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const [request, setRequest] = useState<MaintenanceRequestResponse | null>(null)
  const [comments, setComments] = useState<MaintenanceRequestCommentResponse[]>([])
  const [attachments, setAttachments] = useState<MaintenanceAttachmentResponse[]>([])
  const [maintenanceStaff, setMaintenanceStaff] = useState<UserProfileSummaryResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [noteText, setNoteText] = useState('')
  const [isInternalNote, setIsInternalNote] = useState(false)
  const [isSavingNote, setIsSavingNote] = useState(false)
  const [isSavingStatus, setIsSavingStatus] = useState(false)
  const [isAssigning, setIsAssigning] = useState(false)
  const [selectedAttachment, setSelectedAttachment] = useState<File | null>(null)
  const [isUploadingAttachment, setIsUploadingAttachment] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [downloadingAttachmentId, setDownloadingAttachmentId] = useState('')
  const attachmentInputRef = useRef<HTMLInputElement>(null)
  const canAssign = canAssignRequests(user)
  const canUpdateStatus = canUpdateRequestStatus(user)
  const canMarkInternal = isAdminOrManager(user)
  const allowedStatusValues = getAllowedStatusValues(user)

  async function loadRequestDetails() {
    if (!id) {
      setError('Request details could not be loaded.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError('')

    try {
      const [requestData, commentData, attachmentData, staffData] = await Promise.all([
        getMaintenanceRequestById(id),
        getMaintenanceRequestComments(id),
        getMaintenanceRequestAttachments(id),
        canAssign ? getMaintenanceStaff() : Promise.resolve([]),
      ])

      setRequest(requestData)
      setComments(commentData)
      setAttachments(attachmentData)
      setMaintenanceStaff(staffData)
    } catch {
      setError('Request details could not be loaded for this account.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadRequestDetails()
  }, [id, canAssign])

  async function handleStatusChange(value: string) {
    if (!id) {
      return
    }

    setIsSavingStatus(true)
    setError('')
    setSuccessMessage('')

    try {
      const updated = await updateMaintenanceRequestStatus(id, {
        status: Number(value),
      })
      setRequest(updated)
      setSuccessMessage('Status updated.')
    } catch {
      setError('Status update failed for this request.')
    } finally {
      setIsSavingStatus(false)
    }
  }

  async function handleAssignRequest(staffProfileId: string) {
    if (!id || !staffProfileId) {
      return
    }

    setIsAssigning(true)
    setError('')
    setSuccessMessage('')

    try {
      const updated = await assignMaintenanceRequest(id, {
        assignedStaffProfileId: staffProfileId,
      })
      setRequest(updated)
      setSuccessMessage('Assigned staff updated.')
    } catch {
      setError('Staff assignment failed for this request.')
    } finally {
      setIsAssigning(false)
    }
  }

  async function handleAddNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!id || !user || !noteText.trim()) {
      return
    }

    setIsSavingNote(true)
    setError('')
    setSuccessMessage('')

    try {
      await addMaintenanceRequestComment(id, {
        userProfileId: user.userProfileId,
        commentText: noteText.trim(),
        isInternal: canMarkInternal && isInternalNote,
      })
      const updatedComments = await getMaintenanceRequestComments(id)
      setComments(updatedComments)
      setNoteText('')
      setIsInternalNote(false)
      setSuccessMessage('Activity note added.')
    } catch {
      setError('Activity note could not be saved for this request.')
    } finally {
      setIsSavingNote(false)
    }
  }

  function handleAttachmentSelection(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    setError('')
    setSuccessMessage('')

    if (!file) {
      setSelectedAttachment(null)
      return
    }

    const validationError = validateAttachment(file)
    if (validationError) {
      setSelectedAttachment(null)
      setError(validationError)
      event.target.value = ''
      return
    }

    setSelectedAttachment(file)
  }

  async function handleAttachmentUpload() {
    if (!id || !selectedAttachment) {
      return
    }

    const validationError = validateAttachment(selectedAttachment)
    if (validationError) {
      setError(validationError)
      return
    }

    setIsUploadingAttachment(true)
    setUploadProgress(0)
    setError('')
    setSuccessMessage('')

    try {
      const uploadRequest = {
        fileName: selectedAttachment.name,
        contentType: selectedAttachment.type,
        sizeBytes: selectedAttachment.size,
      }
      const authorization = await createAttachmentUploadAuthorization(
        id,
        uploadRequest,
      )
      await uploadAttachmentDirectlyToS3(
        authorization,
        selectedAttachment,
        setUploadProgress,
      )
      await confirmMaintenanceRequestAttachment(id, {
        ...uploadRequest,
        objectKey: authorization.objectKey,
      })

      setAttachments(await getMaintenanceRequestAttachments(id))
      setSelectedAttachment(null)
      setUploadProgress(100)
      if (attachmentInputRef.current) {
        attachmentInputRef.current.value = ''
      }
      setSuccessMessage('Attachment uploaded securely.')
    } catch {
      setError('Attachment upload failed. Please check the file and try again.')
    } finally {
      setIsUploadingAttachment(false)
    }
  }

  async function handleAttachmentDownload(attachmentId: string) {
    if (!id) {
      return
    }

    setDownloadingAttachmentId(attachmentId)
    setError('')
    setSuccessMessage('')
    try {
      const authorization = await createAttachmentDownloadAuthorization(
        id,
        attachmentId,
      )
      const link = document.createElement('a')
      link.href = authorization.downloadUrl
      link.target = '_blank'
      link.rel = 'noopener noreferrer'
      document.body.appendChild(link)
      link.click()
      link.remove()
    } catch {
      setError('The secure download link could not be created. Please try again.')
    } finally {
      setDownloadingAttachmentId('')
    }
  }

  if (isLoading) {
    return <LoadingState title="Loading request details" />
  }

  if (error && !request) {
    return (
      <ErrorState
        message={error}
        onRetry={() => {
          void loadRequestDetails()
        }}
      />
    )
  }

  if (!request) {
    return (
      <EmptyState
        title="Request not found"
        message="The selected request is not available for this account."
      />
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <Link
          to="/requests"
          className="inline-flex w-fit items-center gap-2 text-sm font-semibold text-cyan-700 hover:text-cyan-800"
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back to requests
        </Link>
        <button
          type="button"
          onClick={() => {
            void loadRequestDetails()
          }}
          className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
        >
          <RefreshCcw className="size-4" aria-hidden="true" />
          Refresh
        </button>
      </div>

      <section className="premium-hero-card p-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-4xl">
            <p className="text-sm font-semibold text-cyan-700">
              Maintenance Request
            </p>
            <div className="mt-3 flex flex-wrap items-center gap-3">
              <h2 className="text-3xl font-semibold text-slate-950">
                {request.title}
              </h2>
              <StatusBadge value={request.status} kind="maintenance" />
              <StatusBadge value={request.priority} kind="priority" />
            </div>
            <p className="mt-4 text-base leading-7 text-slate-600">
              {request.description}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white/80 p-4 text-sm text-slate-600 shadow-sm">
            <p className="font-semibold text-slate-950">
              {getPriorityLabel(request.priority)} priority
            </p>
            <p className="mt-2">Created {formatDateTime(request.createdAtUtc)}</p>
            {request.updatedAtUtc && (
              <p className="mt-1">Updated {formatDateTime(request.updatedAtUtc)}</p>
            )}
          </div>
        </div>
      </section>

      {successMessage && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-medium text-emerald-900">
          {successMessage}
        </div>
      )}

      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            void loadRequestDetails()
          }}
        />
      )}

      <section className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="space-y-5">
          <article className="premium-card p-5">
            <h3 className="text-lg font-semibold text-slate-950">
              Service Progress
            </h3>
            <div className="mt-5">
              <StatusTimeline status={request.status} />
            </div>
          </article>

          <article className="premium-card p-5">
            <h3 className="text-lg font-semibold text-slate-950">
              Request Details
            </h3>
            <dl className="mt-5 grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-3">
              <InfoItem label="Category" value={getCategoryLabel(request.category)} />
              <InfoItem label="Tenant" value={request.tenantName} />
              <InfoItem label="Property" value={request.propertyName || 'Not set'} />
              <InfoItem label="Unit" value={request.unitNumber} />
              <InfoItem
                label="Assigned staff"
                value={request.assignedStaffName || 'Unassigned'}
              />
              <InfoItem
                label="Completed"
                value={formatDateTime(request.completedAtUtc)}
              />
            </dl>
          </article>

          <article className="premium-card p-5">
            <div className="flex items-center gap-2">
              <Paperclip className="size-5 text-cyan-700" aria-hidden="true" />
              <h3 className="text-lg font-semibold text-slate-950">
                Attachments
              </h3>
            </div>

            <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50/70 p-4">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Add an attachment
                </span>
                <input
                  ref={attachmentInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  disabled={isUploadingAttachment}
                  onChange={handleAttachmentSelection}
                  className="mt-2 block w-full text-sm text-slate-600 file:mr-4 file:rounded-md file:border-0 file:bg-white file:px-3 file:py-2 file:text-sm file:font-semibold file:text-cyan-700 file:ring-1 file:ring-inset file:ring-slate-200 hover:file:bg-cyan-50 disabled:cursor-not-allowed"
                />
              </label>
              <p className="mt-2 text-xs leading-5 text-slate-500">
                JPEG, PNG, WebP, or PDF. Maximum file size 10 MB.
              </p>

              {selectedAttachment && (
                <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <p className="min-w-0 truncate text-sm font-medium text-slate-700">
                    {selectedAttachment.name} · {formatFileSize(selectedAttachment.size)}
                  </p>
                  <button
                    type="button"
                    disabled={isUploadingAttachment}
                    onClick={() => {
                      void handleAttachmentUpload()
                    }}
                    className="inline-flex shrink-0 items-center justify-center gap-2 rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
                  >
                    <Upload className="size-4" aria-hidden="true" />
                    {isUploadingAttachment ? 'Uploading...' : 'Upload securely'}
                  </button>
                </div>
              )}

              {isUploadingAttachment && (
                <div className="mt-4" aria-live="polite">
                  <div className="flex items-center justify-between text-xs font-medium text-slate-600">
                    <span>Direct upload to secure storage</span>
                    <span>{uploadProgress}%</span>
                  </div>
                  <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200">
                    <div
                      className="h-full rounded-full bg-cyan-600 transition-[width]"
                      style={{ width: `${uploadProgress}%` }}
                    />
                  </div>
                </div>
              )}
            </div>

            <div className="mt-5 divide-y divide-slate-200">
              {attachments.length === 0 && (
                <p className="text-sm text-slate-500">
                  No secure attachments have been added yet.
                </p>
              )}
              {attachments.map((attachment) => (
                <div
                  key={attachment.id}
                  className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="flex min-w-0 items-start gap-3">
                    <span className="flex size-10 shrink-0 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
                      <FileText className="size-5" aria-hidden="true" />
                    </span>
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-slate-950">
                        {attachment.fileName}
                      </p>
                      <p className="mt-1 text-xs leading-5 text-slate-500">
                        {formatFileSize(attachment.sizeBytes)} · Uploaded by{' '}
                        {attachment.uploadedByName || 'Portal user'} ·{' '}
                        {formatDateTime(attachment.uploadedAtUtc)}
                      </p>
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={downloadingAttachmentId === attachment.id}
                    onClick={() => {
                      void handleAttachmentDownload(attachment.id)
                    }}
                    className="inline-flex shrink-0 items-center justify-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
                  >
                    <Download className="size-4" aria-hidden="true" />
                    {downloadingAttachmentId === attachment.id
                      ? 'Preparing...'
                      : 'Open'}
                  </button>
                </div>
              ))}
            </div>
          </article>

          <article className="premium-card p-5">
            <div className="flex items-center gap-2">
              <MessageSquare className="size-5 text-cyan-700" aria-hidden="true" />
              <h3 className="text-lg font-semibold text-slate-950">
                Activity Notes
              </h3>
            </div>

            <form className="mt-5 space-y-4" onSubmit={handleAddNote}>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Add note
                </span>
                <textarea
                  required
                  rows={4}
                  maxLength={2000}
                  value={noteText}
                  onChange={(event) => setNoteText(event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              {canMarkInternal && (
                <label className="flex items-center gap-2 text-sm text-slate-600">
                  <input
                    type="checkbox"
                    checked={isInternalNote}
                    onChange={(event) => setIsInternalNote(event.target.checked)}
                    className="size-4 rounded border-slate-300 text-cyan-700 focus:ring-cyan-600"
                  />
                  Mark as internal team note
                </label>
              )}
              <button
                type="submit"
                disabled={isSavingNote}
                className="inline-flex items-center gap-2 rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                <Send className="size-4" aria-hidden="true" />
                {isSavingNote ? 'Saving...' : 'Add note'}
              </button>
            </form>

            <div className="mt-6 divide-y divide-slate-200">
              {comments.length === 0 && (
                <p className="text-sm text-slate-500">
                  No activity notes have been added yet.
                </p>
              )}
              {comments.map((comment) => (
                <article key={comment.id} className="py-4">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="text-sm font-semibold text-slate-950">
                      {comment.userFullName}
                    </p>
                    {comment.isInternal && (
                      <span className="rounded-md bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-800 ring-1 ring-inset ring-amber-200">
                        Internal
                      </span>
                    )}
                    <span className="text-xs text-slate-500">
                      {formatDateTime(comment.createdAtUtc)}
                    </span>
                  </div>
                  <p className="mt-2 text-sm leading-6 text-slate-600">
                    {comment.commentText}
                  </p>
                </article>
              ))}
            </div>
          </article>
        </div>

        <aside className="space-y-5">
          <article className="premium-card p-5">
            <div className="flex items-center gap-2">
              <UserCheck className="size-5 text-cyan-700" aria-hidden="true" />
              <h3 className="text-lg font-semibold text-slate-950">
                Workflow Actions
              </h3>
            </div>

            <div className="mt-5 space-y-4">
              {canAssign && (
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">
                    Assigned staff
                  </span>
                  <select
                    value={request.assignedStaffProfileId ?? ''}
                    disabled={isAssigning}
                    onChange={(event) => {
                      void handleAssignRequest(event.target.value)
                    }}
                    className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                  >
                    <option value="">Unassigned</option>
                    {maintenanceStaff.map((staff) => (
                      <option key={staff.id} value={staff.id}>
                        {staff.fullName}
                      </option>
                    ))}
                  </select>
                </label>
              )}

              {canUpdateStatus && allowedStatusValues.length > 0 ? (
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">
                    Update status
                  </span>
                  <select
                    value={getStatusValue(request.status)}
                    disabled={isSavingStatus}
                    onChange={(event) => {
                      void handleStatusChange(event.target.value)
                    }}
                    className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                  >
                    {statusOptions
                      .filter((option) => allowedStatusValues.includes(option.value))
                      .map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                  </select>
                </label>
              ) : (
                <p className="text-sm leading-6 text-slate-600">
                  This account can view progress and add activity notes.
                </p>
              )}
            </div>
          </article>
        </aside>
      </section>
    </div>
  )
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 text-slate-900">{value}</dd>
    </div>
  )
}

export default RequestDetailPage
