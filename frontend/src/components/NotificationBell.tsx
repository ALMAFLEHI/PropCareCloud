import { Bell, CheckCheck, RefreshCw } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  getNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  markNotificationRead,
} from '../api/propCareApi'
import type { UserNotificationResponse } from '../types/api'

const pollIntervalMilliseconds = 45_000

function formatNotificationTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function NotificationBell() {
  const navigate = useNavigate()
  const containerRef = useRef<HTMLDivElement>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [notifications, setNotifications] = useState<UserNotificationResponse[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  const refresh = useCallback(async (showLoading = false) => {
    if (showLoading) {
      setIsLoading(true)
    }

    try {
      const [recent, count] = await Promise.all([
        getNotifications(15),
        getUnreadNotificationCount(),
      ])
      setNotifications(recent)
      setUnreadCount(count)
      setError('')
    } catch {
      setError('Notifications are temporarily unavailable.')
    } finally {
      if (showLoading) {
        setIsLoading(false)
      }
    }
  }, [])

  useEffect(() => {
    void refresh()
    const interval = window.setInterval(() => void refresh(), pollIntervalMilliseconds)
    return () => window.clearInterval(interval)
  }, [refresh])

  useEffect(() => {
    if (!isOpen) {
      return
    }

    void refresh(true)
  }, [isOpen, refresh])

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [])

  async function handleNotificationClick(notification: UserNotificationResponse) {
    if (!notification.isRead) {
      try {
        const updated = await markNotificationRead(notification.id)
        setNotifications((current) =>
          current.map((item) => (item.id === updated.id ? updated : item)),
        )
        setUnreadCount((current) => Math.max(0, current - 1))
      } catch {
        setError('The notification could not be marked as read.')
      }
    }

    if (notification.maintenanceRequestId) {
      setIsOpen(false)
      navigate(`/requests/${notification.maintenanceRequestId}`)
    }
  }

  async function handleMarkAllRead() {
    try {
      await markAllNotificationsRead()
      const readAtUtc = new Date().toISOString()
      setNotifications((current) =>
        current.map((item) => ({
          ...item,
          isRead: true,
          readAtUtc: item.readAtUtc ?? readAtUtc,
        })),
      )
      setUnreadCount(0)
      setError('')
    } catch {
      setError('Notifications could not be marked as read.')
    }
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="relative inline-flex size-10 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-700 shadow-sm transition hover:border-cyan-200 hover:bg-cyan-50 hover:text-cyan-800"
        aria-label="Open notifications"
        aria-expanded={isOpen}
        title="Notifications"
      >
        <Bell className="size-5" aria-hidden="true" />
        {unreadCount > 0 && (
          <span className="absolute -right-1.5 -top-1.5 flex min-w-5 items-center justify-center rounded-full bg-cyan-700 px-1.5 py-0.5 text-[10px] font-bold leading-none text-white ring-2 ring-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <section
          className="absolute right-0 z-50 mt-2 w-[min(23rem,calc(100vw-2rem))] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-2xl"
          aria-label="Recent notifications"
        >
          <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
            <div>
              <h2 className="text-sm font-semibold text-slate-950">Notifications</h2>
              <p className="text-xs text-slate-500">{unreadCount} unread</p>
            </div>
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={() => void refresh(true)}
                disabled={isLoading}
                className="inline-flex size-8 items-center justify-center rounded-md text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:opacity-50"
                aria-label="Refresh notifications"
                title="Refresh notifications"
              >
                <RefreshCw
                  className={`size-4 ${isLoading ? 'animate-spin' : ''}`}
                  aria-hidden="true"
                />
              </button>
              <button
                type="button"
                onClick={() => void handleMarkAllRead()}
                disabled={unreadCount === 0}
                className="inline-flex items-center gap-1.5 rounded-md px-2 py-1.5 text-xs font-semibold text-cyan-700 hover:bg-cyan-50 disabled:cursor-not-allowed disabled:opacity-40"
              >
                <CheckCheck className="size-4" aria-hidden="true" />
                Mark all read
              </button>
            </div>
          </div>

          {error && (
            <p className="border-b border-rose-100 bg-rose-50 px-4 py-2 text-xs text-rose-700">
              {error}
            </p>
          )}

          <div className="max-h-96 overflow-y-auto">
            {isLoading && notifications.length === 0 ? (
              <p className="px-4 py-8 text-center text-sm text-slate-500">
                Loading notifications...
              </p>
            ) : notifications.length === 0 ? (
              <p className="px-4 py-8 text-center text-sm text-slate-500">
                No notifications yet.
              </p>
            ) : (
              notifications.map((notification) => (
                <button
                  key={notification.id}
                  type="button"
                  onClick={() => void handleNotificationClick(notification)}
                  className={`block w-full border-b border-slate-100 px-4 py-3 text-left transition last:border-b-0 hover:bg-slate-50 ${
                    notification.isRead ? 'bg-white' : 'bg-cyan-50/60'
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <span
                      className={`mt-1.5 size-2 shrink-0 rounded-full ${
                        notification.isRead ? 'bg-slate-300' : 'bg-cyan-600'
                      }`}
                      aria-hidden="true"
                    />
                    <span className="min-w-0">
                      <span className="block text-sm font-semibold text-slate-900">
                        {notification.title}
                      </span>
                      <span className="mt-1 block text-sm leading-5 text-slate-600">
                        {notification.message}
                      </span>
                      <span className="mt-1.5 block text-xs text-slate-400">
                        {formatNotificationTime(notification.createdAtUtc)}
                      </span>
                    </span>
                  </div>
                </button>
              ))
            )}
          </div>
        </section>
      )}
    </div>
  )
}

export default NotificationBell
